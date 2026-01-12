// Main incremental source generator that wires together locators, extractors, and emitters
// to produce the RunAsync interceptor.
//
// Pipeline:
//   1. Locate RunAsync call sites → InterceptSiteModel
//   2. Locate [NuruRoute] classes → RouteDefinition
//   3. Collect route locations for error reporting
//   4. Combine and extract full AppModel (with diagnostics)
//   5. Validate the combined model (fluent + attributed routes)
//   6. Report diagnostics and emit generated interceptor code
//
// This generator also performs all model validation (previously in NuruAnalyzer),
// ensuring that validation happens on the exact model being emitted.

namespace TimeWarp.Nuru.Generators;

using TimeWarp.Nuru.Validation;
using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// V2 source generator that produces compile-time route interceptors.
/// This generator supports three DSLs: Fluent, Mini-Language, and Attributed Routes.
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
    // 1. Locate RunAsync call sites (entry points for interception)
    IncrementalValuesProvider<InterceptSiteModel?> runAsyncCalls = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => RunAsyncLocator.IsPotentialMatch(node),
        transform: static (ctx, ct) => RunAsyncLocator.Extract(ctx, ct))
      .Where(static site => site is not null);

    // 2. Locate attributed routes ([NuruRoute] decorated classes) with locations
    IncrementalValuesProvider<RouteWithLocation?> attributedRoutesWithLocations = context.SyntaxProvider
      .ForAttributeWithMetadataName(
        NuruRouteAttributeFullName,
        predicate: static (node, _) => node is ClassDeclarationSyntax,
        transform: static (ctx, ct) => ExtractAttributedRouteWithLocation(ctx, ct))
      .Where(static route => route is not null);

    // 3. Collect attributed routes into an array (extract just the RouteDefinition)
    IncrementalValueProvider<ImmutableArray<RouteDefinition?>> collectedAttributedRoutes =
      attributedRoutesWithLocations
        .Select(static (r, _) => r?.Route)
        .Collect();

    // 4. Collect fluent route locations from Map() calls
    IncrementalValuesProvider<RouteWithLocation?> fluentRouteLocations = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsMapInvocation(node),
        transform: static (ctx, ct) => GetFluentRouteLocation(ctx, ct))
      .Where(static info => info is not null);

    // 5. Combine all route locations (fluent + attributed) into a dictionary
    IncrementalValueProvider<ImmutableDictionary<string, Location>> routeLocations =
      fluentRouteLocations
        .Collect()
        .Combine(attributedRoutesWithLocations.Collect())
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

          // Add attributed route locations
          foreach (RouteWithLocation? route in data.Right)
          {
            if (route is not null && !builder.ContainsKey(route.Pattern))
              builder[route.Pattern] = route.Location;
          }

          return builder.ToImmutable();
        });

    // 6. For each RunAsync call site, extract the AppModel with fluent routes (with diagnostics)
    IncrementalValuesProvider<ExtractionResult> extractionResults = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => RunAsyncLocator.IsPotentialMatch(node),
        transform: static (ctx, ct) => AppExtractor.ExtractWithDiagnostics(ctx, ct))
      .Where(static result => result.Model is not null);

    // 7. Combine extraction results with attributed routes and locations into GeneratorModel
    IncrementalValueProvider<GeneratorModelWithDiagnostics?> generatorModelWithDiagnostics = extractionResults
      .Collect()
      .Combine(collectedAttributedRoutes)
      .Combine(routeLocations)
      .Select(static (data, ct) => CreateGeneratorModelWithValidation(
        data.Left.Left,
        data.Left.Right,
        data.Right,
        ct));

    // 8. Emit generated code and report diagnostics
    context.RegisterSourceOutput(generatorModelWithDiagnostics, static (ctx, modelWithDiags) =>
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

      // Emit the interceptor (includes InterceptsLocationAttribute definition)
      string source = InterceptorEmitter.Emit(modelWithDiags.Model);
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
  private static RouteWithLocation? ExtractAttributedRouteWithLocation
  (
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.TargetNode is not ClassDeclarationSyntax classDeclaration)
      return null;

    RouteDefinition? route = AttributedRouteExtractor.Extract(
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

    return new RouteWithLocation(route.OriginalPattern, location, route);
  }

  /// <summary>
  /// Creates a GeneratorModel from all AppModels, validates the combined model,
  /// and returns both the model and any diagnostics.
  /// </summary>
  private static GeneratorModelWithDiagnostics? CreateGeneratorModelWithValidation
  (
    ImmutableArray<ExtractionResult> extractionResults,
    ImmutableArray<RouteDefinition?> attributedRoutes,
    ImmutableDictionary<string, Location> routeLocations,
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

    // Filter out null models and deduplicate by intercept site
    Dictionary<string, AppModel> uniqueApps = [];

    foreach (ExtractionResult result in extractionResults)
    {
      if (result.Model is null || result.Model.InterceptSites.Length == 0)
        continue;

      // Use first intercept site's attribute syntax as the deduplication key
      string key = result.Model.InterceptSites[0].GetAttributeSyntax();
      if (!uniqueApps.ContainsKey(key))
      {
        // Ensure each app has help and configuration enabled by default
        AppModel enrichedModel = result.Model with
        {
          HasHelp = true,
          HasConfiguration = true
        };
        uniqueApps[key] = enrichedModel;
      }
    }

    // If no apps found, we can't generate an interceptor
    if (uniqueApps.Count == 0)
      return new GeneratorModelWithDiagnostics(null, [.. allDiagnostics]);

    // Collect user usings from all apps
    ImmutableArray<string> userUsings = [.. uniqueApps.Values
      .SelectMany(a => a.UserUsings)
      .Distinct()];

    // Collect attributed routes (non-null only)
    ImmutableArray<RouteDefinition> routes = [.. attributedRoutes.Where(r => r is not null)!];

    // Validate each app's combined routes (fluent + attributed)
    // This catches duplicate routes between fluent and attributed definitions
    foreach (AppModel app in uniqueApps.Values)
    {
      // Combine this app's fluent routes with all attributed routes
      // (same combination that happens during emission)
      ImmutableArray<RouteDefinition> combinedRoutes = [.. app.Routes.Concat(routes)];

      ImmutableArray<Diagnostic> validationDiagnostics = ModelValidator.Validate(
        app with { Routes = combinedRoutes },
        routeLocations);

      allDiagnostics.AddRange(validationDiagnostics);
    }

    GeneratorModel model = new(
      Apps: [.. uniqueApps.Values],
      UserUsings: userUsings,
      AttributedRoutes: routes);

    return new GeneratorModelWithDiagnostics(model, [.. allDiagnostics]);
  }

  /// <summary>
  /// Route pattern with its source location for error reporting.
  /// Optionally includes the RouteDefinition for attributed routes.
  /// </summary>
  private sealed record RouteWithLocation(string Pattern, Location Location, RouteDefinition? Route);

  /// <summary>
  /// Generator model with collected diagnostics from extraction and validation.
  /// </summary>
  private sealed record GeneratorModelWithDiagnostics(GeneratorModel? Model, ImmutableArray<Diagnostic> Diagnostics);
}
