// Main incremental source generator that wires together locators, extractors, and emitters
// to produce the RunAsync interceptor.
//
// Pipeline:
//   1. Locate RunAsync call sites → InterceptSiteModel
//   2. Locate [NuruRoute] classes → RouteDefinition
//   3. Combine and extract full AppModel
//   4. Emit generated interceptor code

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// V2 source generator that produces compile-time route interceptors.
/// This generator supports three DSLs: Fluent, Mini-Language, and Attributed Routes.
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

    // 2. Locate attributed routes ([NuruRoute] decorated classes)
    IncrementalValuesProvider<RouteDefinition?> attributedRoutes = context.SyntaxProvider
      .ForAttributeWithMetadataName(
        NuruRouteAttributeFullName,
        predicate: static (node, _) => node is ClassDeclarationSyntax,
        transform: static (ctx, ct) => ExtractAttributedRoute(ctx, ct))
      .Where(static route => route is not null);

    // 3. Collect attributed routes into an array
    IncrementalValueProvider<ImmutableArray<RouteDefinition?>> collectedAttributedRoutes =
      attributedRoutes.Collect();

    // 4. For each RunAsync call site, extract the AppModel with fluent routes
    IncrementalValuesProvider<AppModel?> appModelsFromRunAsync = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => RunAsyncLocator.IsPotentialMatch(node),
        transform: static (ctx, ct) => AppExtractor.Extract(ctx, ct))
      .Where(static model => model is not null);

    // 5. Combine all AppModels with attributed routes into GeneratorModel
    // Each app keeps its own routes isolated for per-app interceptor generation
    IncrementalValueProvider<GeneratorModel?> generatorModel = appModelsFromRunAsync
      .Collect()
      .Combine(collectedAttributedRoutes)
      .Select(static (data, ct) => CreateGeneratorModel(data.Left, data.Right, ct));

    // 6. Emit generated code
    context.RegisterSourceOutput(generatorModel, static (ctx, model) =>
    {
      if (model is null)
        return;

      // Emit the interceptor (includes InterceptsLocationAttribute definition)
      string source = InterceptorEmitter.Emit(model);
      ctx.AddSource("NuruGenerated.g.cs", source);
    });
  }

  /// <summary>
  /// Extracts a RouteDefinition from a class with [NuruRoute] attribute.
  /// </summary>
  private static RouteDefinition? ExtractAttributedRoute
  (
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.TargetNode is not ClassDeclarationSyntax classDeclaration)
      return null;

    return AttributedRouteExtractor.Extract(
      classDeclaration,
      context.SemanticModel,
      cancellationToken);
  }

  /// <summary>
  /// Creates a GeneratorModel from all AppModels, keeping routes isolated per app.
  /// Each app gets its own interceptor method with only its routes.
  /// </summary>
  private static GeneratorModel? CreateGeneratorModel
  (
    ImmutableArray<AppModel?> appModels,
    ImmutableArray<RouteDefinition?> attributedRoutes,
    CancellationToken cancellationToken
  )
  {
    // Filter out null models and deduplicate by intercept site
    // Deduplication is needed because the generator processes the file once per RunAsync call,
    // and each processing may return the same AppModel multiple times.
    Dictionary<string, AppModel> uniqueApps = [];

    foreach (AppModel? model in appModels)
    {
      if (model is null || model.InterceptSites.Length == 0)
        continue;

      // Use first intercept site's attribute syntax as the deduplication key
      string key = model.InterceptSites[0].GetAttributeSyntax();
      if (!uniqueApps.ContainsKey(key))
      {
        // Ensure each app has help and configuration enabled by default
        AppModel enrichedModel = model with
        {
          HasHelp = true,
          HasConfiguration = true
        };
        uniqueApps[key] = enrichedModel;
      }
    }

    // If no apps found, we can't generate an interceptor
    if (uniqueApps.Count == 0)
      return null;

    // Collect user usings from all apps
    ImmutableArray<string> userUsings = [.. uniqueApps.Values
      .SelectMany(a => a.UserUsings)
      .Distinct()];

    // Collect attributed routes (non-null only)
    ImmutableArray<RouteDefinition> routes = [.. attributedRoutes.Where(r => r is not null)!];

    return new GeneratorModel(
      Apps: [.. uniqueApps.Values],
      UserUsings: userUsings,
      AttributedRoutes: routes);
  }
}
