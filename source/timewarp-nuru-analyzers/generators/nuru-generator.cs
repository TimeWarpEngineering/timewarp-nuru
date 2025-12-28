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
  /// Combines all AppModels from fluent routes with attributed routes.
  /// Collects ALL intercept sites so every RunAsync call is intercepted.
  /// </summary>
  private static AppModel? CombineModels
  (
    ImmutableArray<AppModel?> appModels,
    ImmutableArray<RouteDefinition?> attributedRoutes,
    CancellationToken cancellationToken
  )
  {
    // Collect ALL intercept sites and routes from all AppModels
    ImmutableArray<InterceptSiteModel>.Builder allInterceptSites =
      ImmutableArray.CreateBuilder<InterceptSiteModel>();
    ImmutableArray<RouteDefinition>.Builder allRoutes =
      ImmutableArray.CreateBuilder<RouteDefinition>();

    string? name = null;
    string? description = null;
    string? aiPrompt = null;
    bool hasHelp = false;
    HelpModel? helpOptions = null;
    bool hasRepl = false;
    ReplModel? replOptions = null;
    bool hasConfiguration = false;
    bool hasCheckUpdatesRoute = false;
    ImmutableArray<BehaviorDefinition>.Builder allBehaviors =
      ImmutableArray.CreateBuilder<BehaviorDefinition>();
    ImmutableArray<ServiceDefinition>.Builder allServices =
      ImmutableArray.CreateBuilder<ServiceDefinition>();

    foreach (AppModel? model in appModels)
    {
      if (model is null)
        continue;

      // Collect all intercept sites
      allInterceptSites.AddRange(model.InterceptSites);

      // Collect all routes
      allRoutes.AddRange(model.Routes);

      // Merge other properties (last one wins for simple values)
      name ??= model.Name;
      description ??= model.Description;
      aiPrompt ??= model.AiPrompt;
      hasHelp = hasHelp || model.HasHelp;
      helpOptions ??= model.HelpOptions;
      hasRepl = hasRepl || model.HasRepl;
      replOptions ??= model.ReplOptions;
      hasConfiguration = hasConfiguration || model.HasConfiguration;
      hasCheckUpdatesRoute = hasCheckUpdatesRoute || model.HasCheckUpdatesRoute;
      allBehaviors.AddRange(model.Behaviors);
      allServices.AddRange(model.Services);
    }

    // If no RunAsync calls found, we can't generate an interceptor
    if (allInterceptSites.Count == 0)
      return null;

    // Add attributed routes
    foreach (RouteDefinition? route in attributedRoutes)
    {
      if (route is not null)
        allRoutes.Add(route);
    }

    // Create combined model with all intercept sites
    return new AppModel
    (
      VariableName: null, // Combined models don't track variable names
      Name: name,
      Description: description,
      AiPrompt: aiPrompt,
      HasHelp: hasHelp,
      HelpOptions: helpOptions,
      HasRepl: hasRepl,
      ReplOptions: replOptions,
      HasConfiguration: hasConfiguration,
      HasCheckUpdatesRoute: hasCheckUpdatesRoute,
      Routes: allRoutes.ToImmutable(),
      Behaviors: allBehaviors.ToImmutable(),
      Services: allServices.ToImmutable(),
      InterceptSites: allInterceptSites.ToImmutable()
    );
  }
}
