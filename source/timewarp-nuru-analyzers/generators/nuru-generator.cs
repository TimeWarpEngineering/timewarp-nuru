// Main incremental source generator that wires together locators, extractors, and emitters
// to produce the RunAsync interceptor.
//
// Pipeline:
//   1. Locate Build() call sites → each Build() is one unique app
//   2. For each Build(), extract AppModel with all entry points (RunAsync, RunReplAsync)
//   3. Locate [NuruRoute] classes → RouteDefinition
//   4. Collect route locations for error reporting
//   5. Validate the combined model (fluent + endpoints)
//   6. Report diagnostics and emit generated interceptor code
//
// This generator also performs all model validation (previously in NuruAnalyzer),
// ensuring that validation happens on the exact model being emitted.

namespace TimeWarp.Nuru.Generators;

using TimeWarp.Nuru.Validation;
using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// V2 source generator that produces compile-time route interceptors.
/// This generator supports three DSLs: Fluent, Mini-Language, and Endpoints.
/// Also performs model validation to detect duplicate routes, overlapping patterns, etc.
/// </summary>
[Generator]
public sealed class NuruGenerator : IIncrementalGenerator
{
  private const string NuruRouteAttributeFullName = "TimeWarp.Nuru.NuruRouteAttribute";

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

    // 2. Locate endpoints ([NuruRoute] decorated classes) with locations
    IncrementalValuesProvider<RouteWithLocation?> endpointsWithLocations = context.SyntaxProvider
      .ForAttributeWithMetadataName(
        NuruRouteAttributeFullName,
        predicate: static (node, _) => node is ClassDeclarationSyntax,
        transform: static (ctx, ct) => ExtractEndpointWithLocation(ctx, ct))
      .Where(static route => route is not null);

    // 3. Collect endpoints into an array (extract just the RouteDefinition)
    IncrementalValueProvider<ImmutableArray<RouteDefinition?>> collectedEndpoints =
      endpointsWithLocations
        .Select(static (r, _) => r?.Route)
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
        .Combine(endpointsWithLocations.Collect())
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
          foreach (RouteWithLocation? route in data.Right)
          {
            if (route is not null && !builder.ContainsKey(route.Pattern))
              builder[route.Pattern] = route.Location;
          }

          return builder.ToImmutable();
        });

    // 6. Extract assembly metadata (version, commit hash, commit date) from compilation
    IncrementalValueProvider<AssemblyMetadata> assemblyMetadata = context.CompilationProvider
      .Select(static (compilation, _) => AssemblyMetadataExtractor.Extract(compilation));

    // 7. Combine extraction results with endpoints, locations, and assembly metadata into GeneratorModel
    // Using buildExtractionResults ensures each Build() produces exactly one app - no duplicates
    IncrementalValueProvider<GeneratorModelWithDiagnostics?> generatorModelWithDiagnostics = buildExtractionResults
      .Collect()
      .Combine(collectedEndpoints)
      .Combine(routeLocations)
      .Combine(assemblyMetadata)
      .Select(static (data, ct) => CreateGeneratorModelWithValidation(
        data.Left.Left.Left,
        data.Left.Left.Right,
        data.Left.Right,
        data.Right,
        ct));

    // 8. Emit generated code and report diagnostics
    // Combine with compilation for enum type resolution in REPL completions
    context.RegisterSourceOutput(
      generatorModelWithDiagnostics.Combine(context.CompilationProvider),
      static (ctx, data) =>
      {
        (GeneratorModelWithDiagnostics? modelWithDiags, Compilation compilation) = data;

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

        // Emit the interceptor (includes InterceptsLocationAttribute definition)
        string source = InterceptorEmitter.Emit(modelWithDiags.Model, compilation);
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
      return null;

    string? pattern = literal.Token.ValueText;
    if (string.IsNullOrEmpty(pattern))
      return null;

    Location location = literal.GetLocation();

    return new RouteWithLocation(pattern, location, Route: null);
  }

  /// <summary>
  /// Reports NURU_H007 warnings when ILogger&lt;T&gt; is injected but no AddLogging() is configured.
  /// </summary>
  private static void ReportLoggerWithoutConfigurationWarnings(SourceProductionContext ctx, GeneratorModel model)
  {
    // Check if any app has logging configured
    bool hasLoggingConfigured = model.Apps.Any(a => a.HasLogging);
    if (hasLoggingConfigured)
      return; // No warning needed - logging is configured

    // Find all behaviors with ILogger<T> dependencies
    foreach (BehaviorDefinition behavior in model.AllBehaviors)
    {
      foreach (ParameterBinding dep in behavior.ConstructorDependencies)
      {
        if (dep.ParameterTypeName?.Contains("ILogger", StringComparison.Ordinal) == true)
        {
          // Extract the type argument from ILogger<T> for the message
          string typeArg = ExtractLoggerTypeArg(dep.ParameterTypeName);

          ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.LoggerInjectedWithoutConfiguration,
            Location.None, // No specific location available from the model
            typeArg));
        }
      }
    }
  }

  /// <summary>
  /// Extracts the type argument from ILogger&lt;T&gt;.
  /// </summary>
  private static string ExtractLoggerTypeArg(string loggerTypeName)
  {
    int start = loggerTypeName.IndexOf('<', StringComparison.Ordinal);
    int end = loggerTypeName.LastIndexOf('>');

    if (start >= 0 && end > start)
      return loggerTypeName[(start + 1)..end];

    return "T";
  }

  /// <summary>
  /// Extracts a RouteDefinition and location from a class with [NuruRoute] attribute.
  /// </summary>
  private static RouteWithLocation? ExtractEndpointWithLocation
  (
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.TargetNode is not ClassDeclarationSyntax classDeclaration)
      return null;

    RouteDefinition? route = EndpointExtractor.Extract(
      classDeclaration,
      context.SemanticModel,
      cancellationToken);

    if (route is null)
      return null;

    // Get location from the [NuruRoute] attribute
    Location location = Location.None;
    foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
    {
      foreach (AttributeSyntax attribute in attributeList.Attributes)
      {
        string? name = attribute.Name switch
        {
          IdentifierNameSyntax id => id.Identifier.Text,
          QualifiedNameSyntax q => q.Right.Identifier.Text,
          _ => null
        };

        if (name is "NuruRoute" or "NuruRouteAttribute")
        {
          location = attribute.GetLocation();
          break;
        }
      }

      if (location != Location.None)
        break;
    }

    // Use EffectivePattern for endpoints to avoid collisions
    // (OriginalPattern is just the attribute string, EffectivePattern includes all segments)
    return new RouteWithLocation(route.EffectivePattern, location, route);
  }

  /// <summary>
  /// Creates a GeneratorModel from all AppModels, validates the combined model,
  /// and returns both the model and any diagnostics.
  /// </summary>
  private static GeneratorModelWithDiagnostics? CreateGeneratorModelWithValidation
  (
    ImmutableArray<ExtractionResult> extractionResults,
    ImmutableArray<RouteDefinition?> endpoints,
    ImmutableDictionary<string, Location> routeLocations,
    AssemblyMetadata assemblyMetadata,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;

    List<Diagnostic> allDiagnostics = [];

    // Collect extraction diagnostics from all results
    foreach (ExtractionResult result in extractionResults)
    {
      allDiagnostics.AddRange(result.Diagnostics);
    }

    // Deduplicate by BuildLocation - each Build() call is one unique app
    // BuildLocation is the source location of the Build() call, which uniquely identifies the app
    Dictionary<string, AppModel> uniqueApps = [];

    foreach (ExtractionResult result in extractionResults)
    {
      if (result.Model is null || result.Model.InterceptSitesByMethod.Count == 0)
        continue;

      // Use BuildLocation as the deduplication key
      // Same Build() call = same app (should be rare since we extract from Build() directly)
      string key = result.Model.BuildLocation ?? "unknown";

      if (!uniqueApps.TryGetValue(key, out AppModel? existingApp))
      {
        // Ensure each app has help and configuration enabled by default
        AppModel enrichedModel = result.Model with
        {
          HasHelp = true,
          HasConfiguration = true
        };
        uniqueApps[key] = enrichedModel;
      }
      else
      {
        // Merge intercept sites (shouldn't happen often with Build()-based extraction)
        ImmutableDictionary<string, ImmutableArray<InterceptSiteModel>> mergedSites =
          MergeInterceptSites(existingApp.InterceptSitesByMethod, result.Model.InterceptSitesByMethod);
        uniqueApps[key] = existingApp with { InterceptSitesByMethod = mergedSites };
      }
    }

    // If no apps found, we can't generate an interceptor
    if (uniqueApps.Count == 0)
      return new GeneratorModelWithDiagnostics(null, [.. allDiagnostics]);

    // Collect user usings from all apps
    ImmutableArray<string> userUsings = [.. uniqueApps.Values
      .SelectMany(a => a.UserUsings)
      .Distinct()];

    // Collect endpoints (non-null only)
    ImmutableArray<RouteDefinition> allEndpoints = [.. endpoints.Where(r => r is not null)!];

    // Validate each app's combined routes (fluent + filtered endpoints)
    // This catches duplicate routes between fluent and endpoint definitions
    foreach (AppModel app in uniqueApps.Values)
    {
      // Filter endpoints based on app's discovery mode
      ImmutableArray<RouteDefinition> endpointsForApp = FilterEndpointsForApp(app, allEndpoints);

      // Combine this app's fluent routes with filtered endpoints
      ImmutableArray<RouteDefinition> combinedRoutes = [.. app.Routes.Concat(endpointsForApp)];

      ImmutableArray<Diagnostic> validationDiagnostics = ModelValidator.Validate(
        app with { Routes = combinedRoutes },
        routeLocations);

      allDiagnostics.AddRange(validationDiagnostics);
    }

    GeneratorModel model = new(
      Apps: [.. uniqueApps.Values],
      UserUsings: userUsings,
      Endpoints: allEndpoints,
      Version: assemblyMetadata.Version,
      CommitHash: assemblyMetadata.CommitHash,
      CommitDate: assemblyMetadata.CommitDate);

    return new GeneratorModelWithDiagnostics(model, [.. allDiagnostics]);
  }

  /// <summary>
  /// Filters endpoints based on the app's discovery mode.
  /// </summary>
  /// <param name="app">The app model with discovery settings.</param>
  /// <param name="allEndpoints">All discovered endpoint classes.</param>
  /// <returns>Endpoints that should be included in this app.</returns>
  private static ImmutableArray<RouteDefinition> FilterEndpointsForApp
  (
    AppModel app,
    ImmutableArray<RouteDefinition> allEndpoints
  )
  {
    // If DiscoverEndpoints() was called, include all endpoints
    if (app.DiscoverEndpoints)
      return allEndpoints;

    // If explicit Map<T>() calls, include only those endpoints
    if (!app.ExplicitEndpointTypes.IsDefaultOrEmpty && app.ExplicitEndpointTypes.Length > 0)
    {
      return
      [
        .. allEndpoints.Where
        (
          e => app.ExplicitEndpointTypes.Any
          (
            t => e.Handler.FullTypeName?.EndsWith(t, StringComparison.Ordinal) == true ||
                 e.Handler.FullTypeName == t
          )
        )
      ];
    }

    // Default: no endpoints (test isolation)
    return [];
  }

  /// <summary>
  /// Merges two intercept site dictionaries.
  /// </summary>
  private static ImmutableDictionary<string, ImmutableArray<InterceptSiteModel>> MergeInterceptSites(
    ImmutableDictionary<string, ImmutableArray<InterceptSiteModel>> a,
    ImmutableDictionary<string, ImmutableArray<InterceptSiteModel>> b)
  {
    ImmutableDictionary<string, ImmutableArray<InterceptSiteModel>>.Builder builder = a.ToBuilder();
    foreach (KeyValuePair<string, ImmutableArray<InterceptSiteModel>> kvp in b)
    {
      if (builder.TryGetValue(kvp.Key, out ImmutableArray<InterceptSiteModel> existing))
        builder[kvp.Key] = existing.AddRange(kvp.Value);
      else
        builder[kvp.Key] = kvp.Value;
    }

    return builder.ToImmutable();
  }

  /// <summary>
  /// Route pattern with its source location for error reporting.
  /// Optionally includes the RouteDefinition for endpoints.
  /// </summary>
  private sealed record RouteWithLocation(string Pattern, Location Location, RouteDefinition? Route);

  /// <summary>
  /// Generator model with collected diagnostics from extraction and validation.
  /// </summary>
  private sealed record GeneratorModelWithDiagnostics(GeneratorModel? Model, ImmutableArray<Diagnostic> Diagnostics);
}
