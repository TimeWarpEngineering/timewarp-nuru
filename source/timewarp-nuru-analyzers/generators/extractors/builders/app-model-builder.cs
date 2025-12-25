// Fluent builder for assembling AppModel from extraction phases.
//
// The builder collects data from different analysis phases:
// - RunAsync location -> intercept site
// - CreateBuilder/Build chain -> app metadata
// - Route extraction -> routes from all DSLs
// - ConfigureServices -> service registrations
// - AddBehavior -> pipeline behaviors

namespace TimeWarp.Nuru.Generators;

using System.Collections.Immutable;

/// <summary>
/// Fluent builder for assembling AppModel from extraction phases.
/// Each piece may come from a different extractor.
/// </summary>
internal sealed class AppModelBuilder
{
  private string? _name;
  private string? _description;
  private string? _aiPrompt;
  private bool _hasHelp;
  private HelpModel? _helpOptions;
  private bool _hasRepl;
  private ReplModel? _replOptions;
  private bool _hasConfiguration;
  private ImmutableArray<RouteDefinition>.Builder _routes = ImmutableArray.CreateBuilder<RouteDefinition>();
  private ImmutableArray<BehaviorDefinition>.Builder _behaviors = ImmutableArray.CreateBuilder<BehaviorDefinition>();
  private ImmutableArray<ServiceDefinition>.Builder _services = ImmutableArray.CreateBuilder<ServiceDefinition>();
  private InterceptSiteModel? _interceptSite;

  /// <summary>
  /// Sets the application name (from .WithName() fluent call).
  /// </summary>
  public AppModelBuilder WithName(string? name)
  {
    _name = name;
    return this;
  }

  /// <summary>
  /// Sets the application description (from .WithDescription() fluent call).
  /// </summary>
  public AppModelBuilder WithDescription(string? description)
  {
    _description = description;
    return this;
  }

  /// <summary>
  /// Sets the AI prompt (from .WithAiPrompt() fluent call).
  /// </summary>
  public AppModelBuilder WithAiPrompt(string? aiPrompt)
  {
    _aiPrompt = aiPrompt;
    return this;
  }

  /// <summary>
  /// Enables help with default configuration.
  /// </summary>
  public AppModelBuilder WithHelp()
  {
    _hasHelp = true;
    _helpOptions = HelpModel.Default;
    return this;
  }

  /// <summary>
  /// Enables help with custom configuration.
  /// </summary>
  public AppModelBuilder WithHelp(HelpModel options)
  {
    _hasHelp = true;
    _helpOptions = options;
    return this;
  }

  /// <summary>
  /// Enables REPL with default configuration.
  /// </summary>
  public AppModelBuilder WithRepl()
  {
    _hasRepl = true;
    _replOptions = ReplModel.Default;
    return this;
  }

  /// <summary>
  /// Enables REPL with custom configuration.
  /// </summary>
  public AppModelBuilder WithRepl(ReplModel options)
  {
    _hasRepl = true;
    _replOptions = options;
    return this;
  }

  /// <summary>
  /// Enables configuration support.
  /// </summary>
  public AppModelBuilder WithConfiguration()
  {
    _hasConfiguration = true;
    return this;
  }

  /// <summary>
  /// Adds a single route.
  /// </summary>
  public AppModelBuilder AddRoute(RouteDefinition route)
  {
    _routes.Add(route);
    return this;
  }

  /// <summary>
  /// Adds multiple routes.
  /// </summary>
  public AppModelBuilder AddRoutes(IEnumerable<RouteDefinition> routes)
  {
    _routes.AddRange(routes);
    return this;
  }

  /// <summary>
  /// Adds a single behavior.
  /// </summary>
  public AppModelBuilder AddBehavior(BehaviorDefinition behavior)
  {
    _behaviors.Add(behavior);
    return this;
  }

  /// <summary>
  /// Adds multiple behaviors.
  /// </summary>
  public AppModelBuilder AddBehaviors(IEnumerable<BehaviorDefinition> behaviors)
  {
    _behaviors.AddRange(behaviors);
    return this;
  }

  /// <summary>
  /// Adds a single service registration.
  /// </summary>
  public AppModelBuilder AddService(ServiceDefinition service)
  {
    _services.Add(service);
    return this;
  }

  /// <summary>
  /// Adds multiple service registrations.
  /// </summary>
  public AppModelBuilder AddServices(IEnumerable<ServiceDefinition> services)
  {
    _services.AddRange(services);
    return this;
  }

  /// <summary>
  /// Sets the intercept site (from RunAsync location).
  /// </summary>
  public AppModelBuilder WithInterceptSite(InterceptSiteModel site)
  {
    _interceptSite = site;
    return this;
  }

  /// <summary>
  /// Sets the intercept site from a Roslyn Location.
  /// </summary>
  public AppModelBuilder WithInterceptSite(Location location)
  {
    _interceptSite = InterceptSiteModel.FromLocation(location);
    return this;
  }

  /// <summary>
  /// Builds the immutable AppModel.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown when required fields are missing.</exception>
  public AppModel Build()
  {
    if (_interceptSite is null)
    {
      throw new InvalidOperationException("InterceptSite is required. Call WithInterceptSite() before Build().");
    }

    return new AppModel(
      Name: _name,
      Description: _description,
      AiPrompt: _aiPrompt,
      HasHelp: _hasHelp,
      HelpOptions: _helpOptions,
      HasRepl: _hasRepl,
      ReplOptions: _replOptions,
      HasConfiguration: _hasConfiguration,
      Routes: _routes.ToImmutable(),
      Behaviors: _behaviors.ToImmutable(),
      Services: _services.ToImmutable(),
      InterceptSite: _interceptSite);
  }

  /// <summary>
  /// Resets the builder to initial state for reuse.
  /// </summary>
  public void Reset()
  {
    _name = null;
    _description = null;
    _aiPrompt = null;
    _hasHelp = false;
    _helpOptions = null;
    _hasRepl = false;
    _replOptions = null;
    _hasConfiguration = false;
    _routes = ImmutableArray.CreateBuilder<RouteDefinition>();
    _behaviors = ImmutableArray.CreateBuilder<BehaviorDefinition>();
    _services = ImmutableArray.CreateBuilder<ServiceDefinition>();
    _interceptSite = null;
  }
}
