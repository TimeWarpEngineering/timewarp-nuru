#region Purpose
// Incremental source generator that wires locators, extractors, and emitters to produce the RunAsync interceptor.
#endregion
#region Design
// Pipeline:
//   1. Locate Build() call sites → each Build() is one unique app
//   2. For each Build(), extract AppModel with all entry points (RunAsync, RunReplAsync)
//   3. Locate [NuruRoute] and [Operation] classes → RouteDefinition, plus opt-in
//      referenced-assembly endpoints ([assembly: NuruEndpointAssembly], Map<T>())
//   4. Collect route locations for error reporting
//   5. Validate the combined model (fluent + endpoints)
//   6. Report diagnostics and emit generated interceptor code
// This generator also performs all model validation, so it runs on the exact model being emitted.
#endregion

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// V2 source generator that produces compile-time route interceptors.
/// This generator supports three DSLs: Fluent, Mini-Language, and Endpoints.
/// Also performs model validation to detect duplicate routes, overlapping patterns, etc.
/// </summary>
[Generator]
public sealed partial class NuruGenerator : IIncrementalGenerator
{
  private const string NuruRouteAttributeFullName = "TimeWarp.Nuru.NuruRouteAttribute";
  private const string OperationAttributeFullName = "TimeWarp.Contracts.OperationAttribute";

  /// <summary>
  /// Initializes the incremental generator pipeline.
  /// </summary>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // 1. Locate Build() call sites - each Build() is one unique app
    // This replaces the RunAsync/RunReplAsync-based approach to avoid duplicate extractions
    // NOTE: Don't filter out null models here - CreateGeneratorModelWithValidation handles that
    // and we need to collect diagnostics from all results
    IncrementalValuesProvider<ExtractionResult> buildExtractionResults = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => BuildLocator.IsPotentialMatch(node),
        transform: static (ctx, ct) => AppExtractor.ExtractFromBuildCall(ctx, ct));

    // 2. Locate endpoints ([NuruRoute] decorated classes) with locations and diagnostics
    IncrementalValuesProvider<EndpointWithLocationAndDiagnostics?> routeEndpoints = context.SyntaxProvider
      .ForAttributeWithMetadataName(
        NuruRouteAttributeFullName,
        predicate: static (node, _) => IsRouteTypeDeclaration(node),
        transform: static (ctx, ct) => ExtractEndpointWithLocation(ctx, ct))
      .Where(static result => result is not null);

    IncrementalValuesProvider<EndpointWithLocationAndDiagnostics?> operationEndpoints = context.SyntaxProvider
      .ForAttributeWithMetadataName(
        OperationAttributeFullName,
        predicate: static (node, _) => IsRouteTypeDeclaration(node),
        transform: static (ctx, ct) => HasNuruRouteAttribute(ctx) ? null : ExtractEndpointWithLocation(ctx, ct))
      .Where(static result => result is not null);

    // 2b. Endpoints in referenced assemblies, opt-in only (ReferencedEndpointLocator).
    // [assembly: NuruEndpointAssembly(typeof(X))] adds every endpoint type in X's assembly.
    IncrementalValuesProvider<EndpointWithLocationAndDiagnostics?> assemblyEndpoints = context.SyntaxProvider
      .ForAttributeWithMetadataName(
        ReferencedEndpointLocator.AssemblyAttributeMetadataName,
        predicate: static (node, _) => node is CompilationUnitSyntax,
        transform: static (ctx, ct) => ExtractAssemblyEndpoints(ctx, ct))
      .SelectMany(static (endpoints, _) => endpoints)
      .Where(static result => result is not null);

    // Map<T>() on a referenced-assembly endpoint type adds that endpoint to the mapping app only.
    IncrementalValuesProvider<EndpointWithLocationAndDiagnostics?> mappedReferencedEndpoints = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => ReferencedEndpointLocator.IsGenericMapCall(node),
        transform: static (ctx, ct) => ExtractMappedReferencedEndpoint(ctx, ct))
      .Where(static result => result is not null);

    // Order matters: DistinctBy in CreateGeneratorModelWithValidation keeps the first route per
    // type, so a type both discovered and mapped stays discoverable.
    IncrementalValuesProvider<EndpointWithLocationAndDiagnostics?> endpointsWithDiagnostics = routeEndpoints
      .Collect()
      .Combine(operationEndpoints.Collect())
      .Select(static (pair, _) => pair.Left.AddRange(pair.Right))
      .Combine(assemblyEndpoints.Collect())
      .Select(static (pair, _) => pair.Left.AddRange(pair.Right))
      .Combine(mappedReferencedEndpoints.Collect())
      .Select(static (pair, _) => pair.Left.AddRange(pair.Right))
      .SelectMany(static (endpoints, _) => endpoints);

    // 3. Collect endpoints into an array (extract just the RouteDefinition)
    IncrementalValueProvider<ImmutableArray<RouteDefinition?>> collectedEndpoints =
      endpointsWithDiagnostics
        .Select(static (r, _) => r?.Route)
        .Collect();

    // 3b. Collect endpoint diagnostics
    IncrementalValueProvider<ImmutableArray<Diagnostic>> endpointDiagnostics =
      endpointsWithDiagnostics
        .SelectMany(static (r, _) => r?.Diagnostics ?? [])
        .Collect();

    // 4. Collect fluent route locations from Map() calls
    IncrementalValuesProvider<RouteWithLocation?> fluentRouteLocations = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsMapInvocation(node),
        transform: static (ctx, ct) => GetFluentRouteLocation(ctx, ct))
      .Where(static info => info is not null);

    // 5. Combine all route locations (fluent + endpoints) into a dictionary
    IncrementalValueProvider<ImmutableDictionary<string, Location>> routeLocations =
      fluentRouteLocations
        .Collect()
        .Combine(endpointsWithDiagnostics.Collect())
        .Select(static (data, _) =>
        {
          ImmutableDictionary<string, Location>.Builder builder =
            ImmutableDictionary.CreateBuilder<string, Location>();

          // Add fluent route locations
          foreach (RouteWithLocation? route in data.Left)
          {
            if (route is not null && !builder.ContainsKey(route.Pattern))
              builder[route.Pattern] = route.Location;
          }

          // Add endpoint locations
          foreach (EndpointWithLocationAndDiagnostics? endpoint in data.Right)
          {
            if (endpoint?.Pattern is not null && !builder.ContainsKey(endpoint.Pattern))
              builder[endpoint.Pattern] = endpoint.Location;
          }

          return builder.ToImmutable();
        });

    // 6. Extract assembly metadata (version, commit hash, commit date) from compilation
    IncrementalValueProvider<AssemblyMetadata> assemblyMetadata = context.CompilationProvider
      .Select(static (compilation, _) => AssemblyMetadataExtractor.Extract(compilation));

    // 6b. Detect whether the TimeWarp.Mediator generator emits AddGeneratedMediator() here
    IncrementalValueProvider<string?> generatedMediatorNamespace = context.CompilationProvider
      .Combine(context.AnalyzerConfigOptionsProvider)
      .Select(static (data, _) => GeneratedMediatorDetector.Detect(data.Left, data.Right));

    // 6c. Full configuration sources are emitted only when something in this compilation uses them.
    // The locator result replaces the previous hardcoded HasConfiguration=true.
    IncrementalValueProvider<bool> hasConfiguration = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => AddConfigurationLocator.IsPotentialMatch(node),
        transform: static (ctx, ct) => AddConfigurationLocator.UsesConfiguration(ctx, ct))
      .Where(static usesConfiguration => usesConfiguration)
      .Collect()
      .Select(static (matches, _) => matches.Length > 0);

    // 7. Combine extraction results with endpoints, locations, endpoint diagnostics, and assembly metadata into GeneratorModel
    // Using buildExtractionResults ensures each Build() produces exactly one app - no duplicates
    IncrementalValueProvider<GeneratorModelWithDiagnostics?> generatorModelWithDiagnostics = buildExtractionResults
      .Collect()
      .Combine(collectedEndpoints)
      .Combine(routeLocations)
      .Combine(endpointDiagnostics)
      .Combine(assemblyMetadata)
      .Combine(generatedMediatorNamespace)
      .Combine(hasConfiguration)
      .Combine(context.CompilationProvider)
      .Select(static (data, ct) => CreateGeneratorModelWithValidation(
        data.Left.Left.Left.Left.Left.Left.Left,
        data.Left.Left.Left.Left.Left.Left.Right,
        data.Left.Left.Left.Left.Left.Right,
        data.Left.Left.Left.Left.Right,
        data.Left.Left.Left.Right,
        data.Left.Left.Right,
        data.Left.Right,
        data.Right,
        ct));

    // 8. Split the output into two stages so the heavy emit can cache (M4):
    //   - The Compilation changes on every keystroke. Feeding it (or the Location-bearing
    //     diagnostics) into the emit output forces InterceptorEmitter.Emit to re-run every edit.
    //   - Stage A (uncached): report diagnostics + logger warnings from the wrapper.
    //   - Stage B (cacheable): emit from the equatable GeneratorModel + a precomputed, equatable
    //     EnumInfo set. Enum resolution still needs the Compilation, but only to PRODUCE the
    //     equatable EnumInfo — when the enum shapes are unchanged, emit compares equal and caches.

    // The pure emit model (no diagnostics, no Location) — the cache key for emit.
    IncrementalValueProvider<GeneratorModel?> modelProvider =
      generatorModelWithDiagnostics
        .Select(static (modelWithDiags, _) => modelWithDiags?.Model)
        .WithTrackingName("NuruGeneratorModel");

    // Enum member names, resolved from the Compilation but projected into an equatable set.
    // Re-runs on every edit (Compilation changes), but its OUTPUT caches when enums are unchanged.
    IncrementalValueProvider<EquatableArray<EnumInfo>> enumInfoProvider =
      modelProvider
        .Combine(context.CompilationProvider)
        .Select(static (data, ct) => EnumInfoExtractor.Resolve(data.Left, data.Right, ct))
        .WithTrackingName("NuruEnumInfo");

    // Stage A: diagnostics + logger warnings (uncached — diagnostics carry Roslyn Locations).
    context.RegisterSourceOutput(
      generatorModelWithDiagnostics,
      static (ctx, modelWithDiags) =>
      {
        if (modelWithDiags is null)
          return;

        // Report all diagnostics (extraction + validation)
        foreach (Diagnostic diagnostic in modelWithDiags.Diagnostics)
        {
          ctx.ReportDiagnostic(diagnostic);
        }

        if (modelWithDiags.Model is null)
          return;

        // Report diagnostic if ILogger is injected but no logging is configured
        ReportLoggerWithoutConfigurationWarnings(ctx, modelWithDiags.Model);
      });

    // Stage A2: NURU_I001 for RunAsync() calls no app intercepts (task 547). Uncached, like Stage A.
    IncrementalValueProvider<ImmutableArray<RunAsyncCall>> runAsyncCalls = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => RunAsyncLocator.IsPotentialMatch(node),
        transform: static (ctx, ct) => RunAsyncLocator.LocateCall(ctx, ct))
      .Where(static call => call is not null)
      .Select(static (call, _) => call!)
      .Collect();

    context.RegisterSourceOutput(
      runAsyncCalls.Combine(generatorModelWithDiagnostics),
      static (ctx, data) => ReportRunAsyncNotIntercepted(ctx, data.Left, data.Right));

    // Stage B: emit generated code (cacheable — no Compilation, no diagnostics).
    context.RegisterSourceOutput(
      modelProvider.Combine(enumInfoProvider),
      static (ctx, data) =>
      {
        (GeneratorModel? model, EquatableArray<EnumInfo> enumInfo) = data;

        if (model is null)
          return;

        // Emit the interceptor (includes InterceptsLocationAttribute definition)
        string source = InterceptorEmitter.Emit(model, enumInfo);
        ctx.AddSource("NuruGenerated.g.cs", source);
      });
  }

  /// <summary>
  /// Checks if a syntax node is a Map() invocation.
  /// </summary>
  private static bool IsMapInvocation(RoslynSyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return memberAccess.Name.Identifier.Text == "Map";
  }

  /// <summary>
  /// Extracts route pattern and location from a Map() call.
  /// </summary>
  private static RouteWithLocation? GetFluentRouteLocation(GeneratorSyntaxContext context, CancellationToken cancellationToken)
  {
    _ = cancellationToken;

    if (context.Node is not InvocationExpressionSyntax invocation)
      return null;

    ArgumentListSyntax? argumentList = invocation.ArgumentList;
    if (argumentList is null || argumentList.Arguments.Count == 0)
      return null;

    ArgumentSyntax firstArgument = argumentList.Arguments[0];
    ExpressionSyntax? expression = firstArgument.Expression;

    if (expression is not LiteralExpressionSyntax literal ||
        !literal.IsKind(SyntaxKind.StringLiteralExpression))
    {
      return null;
    }

    string? pattern = literal.Token.ValueText;
    // Empty string is a valid default route. Keep its location so NURU_R004 can point at Map("").
    if (pattern is null)
      return null;

    Location location = literal.GetLocation();

    return new RouteWithLocation(pattern, location, Route: null);
  }
}
