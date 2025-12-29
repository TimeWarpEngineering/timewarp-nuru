// IR builder that mirrors the NuruCoreAppBuilder DSL for semantic interpretation.
//
// This builder is used by the DslInterpreter to "execute" the DSL at design time,
// accumulating state that will be converted to an AppModel.
//
// Key design:
// - CRTP pattern matches NuruCoreAppBuilder<TSelf>
// - Method names mirror DSL methods exactly
// - Build() marks as built but doesn't finalize (for RunAsync intercept sites)
// - FinalizeModel() creates the actual AppModel
// - Implements IIrAppBuilder for polymorphic dispatch in interpreter

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// IR builder that mirrors the NuruCoreAppBuilder DSL for semantic interpretation.
/// Uses CRTP pattern to enable proper fluent chaining.
/// </summary>
/// <typeparam name="TSelf">The concrete builder type for fluent returns.</typeparam>
public class IrAppBuilder<TSelf> : IIrAppBuilder where TSelf : IrAppBuilder<TSelf>
{
  // State fields mirroring AppModel properties
  private string? VariableName;
  private string? Name;
  private string? Description;
  private string? AiPrompt;
  private bool HasHelp;
  private HelpModel? HelpOptions;
  private bool HasRepl;
  private ReplModel? ReplOptions;
  private bool HasConfiguration;
  private bool HasCheckUpdatesRoute;
  private readonly List<RouteDefinition> Routes = [];
  private readonly List<BehaviorDefinition> Behaviors = [];
  private readonly List<ServiceDefinition> Services = [];
  private readonly List<InterceptSiteModel> InterceptSites = [];
  private bool IsBuilt;

  /// <summary>
  /// Sets the variable name for debugging/identification.
  /// Called by interpreter when processing local declarations.
  /// </summary>
  public TSelf SetVariableName(string variableName)
  {
    VariableName = variableName;
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the application name.
  /// Mirrors: NuruCoreAppBuilder.WithName()
  /// </summary>
  public TSelf WithName(string name)
  {
    Name = name;
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the application description.
  /// Mirrors: NuruCoreAppBuilder.WithDescription()
  /// </summary>
  public TSelf WithDescription(string description)
  {
    Description = description;
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the AI prompt for --capabilities output.
  /// Mirrors: NuruCoreAppBuilder.WithAiPrompt()
  /// </summary>
  public TSelf WithAiPrompt(string aiPrompt)
  {
    AiPrompt = aiPrompt;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables help with default options.
  /// Mirrors: NuruCoreAppBuilder.AddHelp()
  /// </summary>
  public TSelf AddHelp()
  {
    HasHelp = true;
    HelpOptions = HelpModel.Default;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables help with custom options.
  /// Mirrors: NuruCoreAppBuilder.AddHelp(Action&lt;HelpOptions&gt;)
  /// </summary>
  /// <param name="helpOptions">The configured help options.</param>
  public TSelf AddHelp(HelpModel helpOptions)
  {
    HasHelp = true;
    HelpOptions = helpOptions;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables REPL with default options.
  /// Mirrors: NuruCoreAppBuilder.AddRepl()
  /// </summary>
  public TSelf AddRepl()
  {
    HasRepl = true;
    ReplOptions = ReplModel.Default;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables REPL with custom options.
  /// Mirrors: NuruCoreAppBuilder.AddRepl(Action&lt;ReplOptions&gt;)
  /// </summary>
  /// <param name="replOptions">The configured REPL options.</param>
  public TSelf AddRepl(ReplModel replOptions)
  {
    HasRepl = true;
    ReplOptions = replOptions;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables configuration.
  /// Mirrors: NuruCoreAppBuilder.AddConfiguration()
  /// </summary>
  public TSelf AddConfiguration()
  {
    HasConfiguration = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables the --check-updates route for GitHub version checking.
  /// Mirrors: NuruAppBuilderExtensions.AddCheckUpdatesRoute()
  /// </summary>
  public TSelf AddCheckUpdatesRoute()
  {
    HasCheckUpdatesRoute = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Adds a behavior (pipeline middleware).
  /// Mirrors: NuruCoreAppBuilder.AddBehavior(Type)
  /// </summary>
  /// <param name="behavior">The behavior definition.</param>
  public TSelf AddBehavior(BehaviorDefinition behavior)
  {
    Behaviors.Add(behavior);
    return (TSelf)this;
  }

  /// <summary>
  /// Adds a service registration.
  /// Mirrors: NuruCoreAppBuilder.ConfigureServices()
  /// </summary>
  /// <param name="service">The service definition.</param>
  public TSelf AddService(ServiceDefinition service)
  {
    Services.Add(service);
    return (TSelf)this;
  }

  /// <summary>
  /// No-op for UseTerminal (runtime only).
  /// Mirrors: NuruCoreAppBuilder.UseTerminal()
  /// </summary>
  public TSelf UseTerminal()
  {
    // No-op - terminal is runtime only
    return (TSelf)this;
  }

  /// <summary>
  /// Begins mapping a new route.
  /// Mirrors: NuruCoreAppBuilder.Map()
  /// </summary>
  /// <param name="pattern">The route pattern string.</param>
  /// <returns>An IrRouteBuilder for configuring the route.</returns>
  public IrRouteBuilder<TSelf> Map(string pattern)
  {
    ImmutableArray<SegmentDefinition> segments = PatternStringExtractor.ExtractSegments(pattern);
    return new IrRouteBuilder<TSelf>((TSelf)this, pattern, segments, RegisterRoute);
  }

  /// <summary>
  /// Creates a route group with a shared prefix.
  /// Mirrors: NuruCoreAppBuilder.WithGroupPrefix()
  /// </summary>
  /// <param name="prefix">The prefix for all routes in this group.</param>
  /// <returns>A group builder for configuring nested routes.</returns>
  public IrGroupBuilder<TSelf> WithGroupPrefix(string prefix)
  {
    return new IrGroupBuilder<TSelf>((TSelf)this, prefix, RegisterRoute);
  }

  /// <summary>
  /// Adds an intercept site from a RunAsync() call.
  /// Called by interpreter when RunAsync() is encountered.
  /// </summary>
  public TSelf AddInterceptSite(InterceptSiteModel site)
  {
    InterceptSites.Add(site);
    return (TSelf)this;
  }

  /// <summary>
  /// Marks the builder as "built".
  /// Mirrors: NuruCoreAppBuilder.Build()
  /// Does not finalize - call FinalizeModel() after all RunAsync() sites are captured.
  /// </summary>
  public TSelf Build()
  {
    IsBuilt = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Finalizes and returns the AppModel.
  /// Called by interpreter after all statements have been processed.
  /// </summary>
  /// <returns>The completed AppModel.</returns>
  /// <exception cref="InvalidOperationException">Thrown if Build() was not called.</exception>
  public AppModel FinalizeModel()
  {
    if (!IsBuilt)
    {
      throw new InvalidOperationException("Build() must be called before FinalizeModel().");
    }

    return new AppModel(
      VariableName: VariableName,
      Name: Name,
      Description: Description,
      AiPrompt: AiPrompt,
      HasHelp: HasHelp,
      HelpOptions: HelpOptions,
      HasRepl: HasRepl,
      ReplOptions: ReplOptions,
      HasConfiguration: HasConfiguration,
      HasCheckUpdatesRoute: HasCheckUpdatesRoute,
      Routes: [.. Routes],
      Behaviors: [.. Behaviors],
      Services: [.. Services],
      InterceptSites: [.. InterceptSites],
      UserUsings: []);  // Usings are populated by AppExtractor, not the builder
  }

  /// <summary>
  /// Callback for IrRouteBuilder to register a completed route.
  /// </summary>
  private void RegisterRoute(RouteDefinition route) => Routes.Add(route);

  // ═══════════════════════════════════════════════════════════════════════════════
  // EXPLICIT INTERFACE IMPLEMENTATIONS
  // ═══════════════════════════════════════════════════════════════════════════════
  // These return interface types for polymorphic dispatch in DslInterpreter.
  // The public methods above return concrete types for direct usage with CRTP.

  IIrRouteBuilder IIrRouteSource.Map(string pattern) => Map(pattern);
  IIrGroupBuilder IIrRouteSource.WithGroupPrefix(string prefix) => WithGroupPrefix(prefix);
  IIrAppBuilder IIrAppBuilder.SetVariableName(string variableName) => SetVariableName(variableName);
  IIrAppBuilder IIrAppBuilder.Build() => Build();
  IIrAppBuilder IIrAppBuilder.WithName(string name) => WithName(name);
  IIrAppBuilder IIrAppBuilder.WithDescription(string description) => WithDescription(description);
  IIrAppBuilder IIrAppBuilder.WithAiPrompt(string aiPrompt) => WithAiPrompt(aiPrompt);
  IIrAppBuilder IIrAppBuilder.AddHelp() => AddHelp();
  IIrAppBuilder IIrAppBuilder.AddHelp(HelpModel helpOptions) => AddHelp(helpOptions);
  IIrAppBuilder IIrAppBuilder.AddRepl() => AddRepl();
  IIrAppBuilder IIrAppBuilder.AddRepl(ReplModel replOptions) => AddRepl(replOptions);
  IIrAppBuilder IIrAppBuilder.AddConfiguration() => AddConfiguration();
  IIrAppBuilder IIrAppBuilder.AddCheckUpdatesRoute() => AddCheckUpdatesRoute();
  IIrAppBuilder IIrAppBuilder.AddBehavior(BehaviorDefinition behavior) => AddBehavior(behavior);
  IIrAppBuilder IIrAppBuilder.AddService(ServiceDefinition service) => AddService(service);
  IIrAppBuilder IIrAppBuilder.UseTerminal() => UseTerminal();
  IIrAppBuilder IIrAppBuilder.AddInterceptSite(InterceptSiteModel site) => AddInterceptSite(site);
}

/// <summary>
/// Non-generic convenience type for direct use.
/// </summary>
public sealed class IrAppBuilder : IrAppBuilder<IrAppBuilder> { }
