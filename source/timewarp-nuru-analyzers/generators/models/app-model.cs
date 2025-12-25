namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Design-time representation of a complete CLI application.
/// This is the top-level IR passed from extractors to emitters.
/// </summary>
/// <param name="Name">Application name for help/version output</param>
/// <param name="Description">Application description for help output</param>
/// <param name="AiPrompt">AI prompt for --capabilities output</param>
/// <param name="HasHelp">Whether help is enabled</param>
/// <param name="HelpOptions">Help configuration, if enabled</param>
/// <param name="HasRepl">Whether REPL mode is enabled</param>
/// <param name="ReplOptions">REPL configuration, if enabled</param>
/// <param name="HasConfiguration">Whether configuration is enabled</param>
/// <param name="Routes">All route definitions from all DSLs</param>
/// <param name="Behaviors">Pipeline behaviors with ordering</param>
/// <param name="Services">Registered services for DI</param>
/// <param name="InterceptSites">Locations of all RunAsync() calls for interceptor</param>
internal sealed record AppModel(
  string? Name,
  string? Description,
  string? AiPrompt,
  bool HasHelp,
  HelpModel? HelpOptions,
  bool HasRepl,
  ReplModel? ReplOptions,
  bool HasConfiguration,
  ImmutableArray<RouteDefinition> Routes,
  ImmutableArray<BehaviorDefinition> Behaviors,
  ImmutableArray<ServiceDefinition> Services,
  ImmutableArray<InterceptSiteModel> InterceptSites)
{
  /// <summary>
  /// Creates an empty AppModel with required intercept sites.
  /// </summary>
  public static AppModel Empty(ImmutableArray<InterceptSiteModel> interceptSites) => new(
    Name: null,
    Description: null,
    AiPrompt: null,
    HasHelp: false,
    HelpOptions: null,
    HasRepl: false,
    ReplOptions: null,
    HasConfiguration: false,
    Routes: [],
    Behaviors: [],
    Services: [],
    InterceptSites: interceptSites);

  /// <summary>
  /// Creates an empty AppModel with a single intercept site.
  /// </summary>
  public static AppModel Empty(InterceptSiteModel interceptSite) =>
    Empty([interceptSite]);

  /// <summary>
  /// Gets whether this app has any routes defined.
  /// </summary>
  public bool HasRoutes => Routes.Length > 0;

  /// <summary>
  /// Gets whether this app has any behaviors registered.
  /// </summary>
  public bool HasBehaviors => Behaviors.Length > 0;

  /// <summary>
  /// Gets whether this app has any services registered.
  /// </summary>
  public bool HasServices => Services.Length > 0;

  /// <summary>
  /// Gets routes sorted by specificity (highest first).
  /// </summary>
  public IEnumerable<RouteDefinition> RoutesBySpecificity =>
    Routes.OrderByDescending(r => r.ComputedSpecificity);

  /// <summary>
  /// Gets all query routes.
  /// </summary>
  public IEnumerable<RouteDefinition> Queries =>
    Routes.Where(r => r.MessageType == "Query");

  /// <summary>
  /// Gets all command routes.
  /// </summary>
  public IEnumerable<RouteDefinition> Commands =>
    Routes.Where(r => r.MessageType is "Command" or "IdempotentCommand");
}
