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

    // 5. Combine the first AppModel with attributed routes
    // Note: We take only the first RunAsync call - multiple calls are not supported
    IncrementalValueProvider<AppModel?> combinedModel = appModelsFromRunAsync
      .Collect()
      .Combine(collectedAttributedRoutes)
      .Select(static (data, ct) => CombineModels(data.Left, data.Right, ct));

    // 6. Emit generated code
    context.RegisterSourceOutput(combinedModel, static (ctx, model) =>
    {
      if (model is null)
        return;

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
  /// Combines the base AppModel from fluent routes with attributed routes.
  /// </summary>
  private static AppModel? CombineModels
  (
    ImmutableArray<AppModel?> appModels,
    ImmutableArray<RouteDefinition?> attributedRoutes,
    CancellationToken cancellationToken
  )
  {
    // Find the first valid AppModel (from RunAsync call)
    AppModel? baseModel = null;
    foreach (AppModel? model in appModels)
    {
      if (model is not null)
      {
        baseModel = model;
        break;
      }
    }

    // If no RunAsync call found, we can't generate an interceptor
    if (baseModel is null)
      return null;

    // Filter out null attributed routes
    ImmutableArray<RouteDefinition>.Builder validAttributedRoutes =
      ImmutableArray.CreateBuilder<RouteDefinition>();

    foreach (RouteDefinition? route in attributedRoutes)
    {
      if (route is not null)
        validAttributedRoutes.Add(route);
    }

    // If no attributed routes, return base model as-is
    if (validAttributedRoutes.Count == 0)
      return baseModel;

    // Merge attributed routes with fluent routes
    ImmutableArray<RouteDefinition> mergedRoutes = baseModel.Routes
      .AddRange(validAttributedRoutes.ToImmutable());

    return baseModel with { Routes = mergedRoutes };
  }
}
