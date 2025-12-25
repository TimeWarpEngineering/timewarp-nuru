// Fluent builder for assembling AppModel from extraction phases.
//
// The builder collects data from different analysis phases:
// - RunAsync location -> intercept site
// - CreateBuilder/Build chain -> app metadata
// - Route extraction -> routes from all DSLs
// - ConfigureServices -> service registrations
// - AddBehavior -> pipeline behaviors

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Fluent builder for assembling AppModel from extraction phases.
/// Each piece may come from a different extractor.
/// </summary>
internal sealed class AppModelBuilder
{
  private string? Name;
  private string? Description;
  private string? AiPrompt;
  private bool HasHelp;
  private HelpModel? HelpOptions;
  private bool HasRepl;
  private ReplModel? ReplOptions;
  private bool HasConfiguration;
  private ImmutableArray<RouteDefinition>.Builder Routes = ImmutableArray.CreateBuilder<RouteDefinition>();
  private ImmutableArray<BehaviorDefinition>.Builder Behaviors = ImmutableArray.CreateBuilder<BehaviorDefinition>();
  private ImmutableArray<ServiceDefinition>.Builder Services = ImmutableArray.CreateBuilder<ServiceDefinition>();
  private InterceptSiteModel? InterceptSite;

  /// <summary>
  /// Sets the application name (from .WithName() fluent call).
  /// </summary>
  public AppModelBuilder WithName(string? name)
  {
    Name = name;
    return this;
  }

  /// <summary>
  /// Sets the application description (from .WithDescription() fluent call).
  /// </summary>
  public AppModelBuilder WithDescription(string? description)
  {
    Description = description;
    return this;
  }

  /// <summary>
  /// Sets the AI prompt (from .WithAiPrompt() fluent call).
  /// </summary>
  public AppModelBuilder WithAiPrompt(string? aiPrompt)
  {
    AiPrompt = aiPrompt;
    return this;
  }

  /// <summary>
  /// Enables help with default configuration.
  /// </summary>
  public AppModelBuilder WithHelp()
  {
    HasHelp = true;
    HelpOptions = HelpModel.Default;
    return this;
  }

  /// <summary>
  /// Enables help with custom configuration.
  /// </summary>
  public AppModelBuilder WithHelp(HelpModel options)
  {
    HasHelp = true;
    HelpOptions = options;
    return this;
  }

  /// <summary>
  /// Enables REPL with default configuration.
  /// </summary>
  public AppModelBuilder WithRepl()
  {
    HasRepl = true;
    ReplOptions = ReplModel.Default;
    return this;
  }

  /// <summary>
  /// Enables REPL with custom configuration.
  /// </summary>
  public AppModelBuilder WithRepl(ReplModel options)
  {
    HasRepl = true;
    ReplOptions = options;
    return this;
  }

  /// <summary>
  /// Enables configuration support.
  /// </summary>
  public AppModelBuilder WithConfiguration()
  {
    HasConfiguration = true;
    return this;
  }

  /// <summary>
  /// Adds a single route.
  /// </summary>
  public AppModelBuilder AddRoute(RouteDefinition route)
  {
    Routes.Add(route);
    return this;
  }

  /// <summary>
  /// Adds multiple routes.
  /// </summary>
  public AppModelBuilder AddRoutes(IEnumerable<RouteDefinition> routes)
  {
    Routes.AddRange(routes);
    return this;
  }

  /// <summary>
  /// Adds a single behavior.
  /// </summary>
  public AppModelBuilder AddBehavior(BehaviorDefinition behavior)
  {
    Behaviors.Add(behavior);
    return this;
  }

  /// <summary>
  /// Adds multiple behaviors.
  /// </summary>
  public AppModelBuilder AddBehaviors(IEnumerable<BehaviorDefinition> behaviors)
  {
    Behaviors.AddRange(behaviors);
    return this;
  }

  /// <summary>
  /// Adds a single service registration.
  /// </summary>
  public AppModelBuilder AddService(ServiceDefinition service)
  {
    Services.Add(service);
    return this;
  }

  /// <summary>
  /// Adds multiple service registrations.
  /// </summary>
  public AppModelBuilder AddServices(IEnumerable<ServiceDefinition> services)
  {
    Services.AddRange(services);
    return this;
  }

  /// <summary>
  /// Sets the intercept site (from RunAsync location).
  /// </summary>
  public AppModelBuilder WithInterceptSite(InterceptSiteModel site)
  {
    InterceptSite = site;
    return this;
  }

  /// <summary>
  /// Sets the intercept site from a SemanticModel and InvocationExpression.
  /// Uses the new .NET 10 / C# 14 InterceptableLocation API.
  /// </summary>
  public AppModelBuilder WithInterceptSite(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
  {
    InterceptSite = InterceptSiteExtractor.Extract(semanticModel, invocation);
    return this;
  }

  /// <summary>
  /// Builds the immutable AppModel.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown when required fields are missing.</exception>
  public AppModel Build()
  {
    if (InterceptSite is null)
    {
      throw new InvalidOperationException("InterceptSite is required. Call WithInterceptSite() before Build().");
    }

    return new AppModel
    (
      Name: Name,
      Description: Description,
      AiPrompt: AiPrompt,
      HasHelp: HasHelp,
      HelpOptions: HelpOptions,
      HasRepl: HasRepl,
      ReplOptions: ReplOptions,
      HasConfiguration: HasConfiguration,
      Routes: Routes.ToImmutable(),
      Behaviors: Behaviors.ToImmutable(),
      Services: Services.ToImmutable(),
      InterceptSite: InterceptSite
    );
  }

  /// <summary>
  /// Resets the builder to initial state for reuse.
  /// </summary>
  public void Reset()
  {
    Name = null;
    Description = null;
    AiPrompt = null;
    HasHelp = false;
    HelpOptions = null;
    HasRepl = false;
    ReplOptions = null;
    HasConfiguration = false;
    Routes = ImmutableArray.CreateBuilder<RouteDefinition>();
    Behaviors = ImmutableArray.CreateBuilder<BehaviorDefinition>();
    Services = ImmutableArray.CreateBuilder<ServiceDefinition>();
    InterceptSite = null;
  }
}
