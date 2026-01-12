namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Design-time representation of a complete CLI application.
/// This is the top-level IR passed from extractors to emitters.
/// </summary>
/// <param name="VariableName">Variable name for debugging/identification (e.g., "app" from "var app = ...")</param>
/// <param name="Name">Application name for help/version output</param>
/// <param name="Description">Application description for help output</param>
/// <param name="AiPrompt">AI prompt for --capabilities output</param>
/// <param name="HasHelp">Whether help is enabled</param>
/// <param name="HelpOptions">Help configuration, if enabled</param>
/// <param name="HasRepl">Whether REPL mode is enabled</param>
/// <param name="ReplOptions">REPL configuration, if enabled</param>
/// <param name="HasConfiguration">Whether configuration is enabled</param>
/// <param name="HasCheckUpdatesRoute">Whether the --check-updates route is enabled</param>
/// <param name="Routes">All route definitions from all DSLs</param>
/// <param name="Behaviors">Pipeline behaviors with ordering</param>
/// <param name="Services">Registered services for DI</param>
/// <param name="InterceptSites">Locations of all RunAsync() calls for interceptor</param>
/// <param name="UserUsings">User's using directives to include in generated code</param>
/// <param name="CustomConverters">Custom type converters registered via AddTypeConverter()</param>
/// <param name="LoggingConfiguration">Logging configuration from AddLogging(), if configured</param>
/// <param name="DiscoverEndpoints">Whether DiscoverEndpoints() was called to include all [NuruRoute] classes</param>
/// <param name="ExplicitEndpointTypes">Fully qualified type names from Map&lt;T&gt;() calls for specific endpoint inclusion</param>
public sealed record AppModel(
  string? VariableName,
  string? Name,
  string? Description,
  string? AiPrompt,
  bool HasHelp,
  HelpModel? HelpOptions,
  bool HasRepl,
  ReplModel? ReplOptions,
  bool HasConfiguration,
  bool HasCheckUpdatesRoute,
  ImmutableArray<RouteDefinition> Routes,
  ImmutableArray<BehaviorDefinition> Behaviors,
  ImmutableArray<ServiceDefinition> Services,
  ImmutableArray<InterceptSiteModel> InterceptSites,
  ImmutableArray<string> UserUsings,
  ImmutableArray<CustomConverterDefinition> CustomConverters,
  LoggingConfiguration? LoggingConfiguration,
  bool DiscoverEndpoints = false,
  ImmutableArray<string> ExplicitEndpointTypes = default)
{
  /// <summary>
  /// Creates an empty AppModel with required intercept sites.
  /// </summary>
  public static AppModel Empty(ImmutableArray<InterceptSiteModel> interceptSites) => new(
    VariableName: null,
    Name: null,
    Description: null,
    AiPrompt: null,
    HasHelp: false,
    HelpOptions: null,
    HasRepl: false,
    ReplOptions: null,
    HasConfiguration: false,
    HasCheckUpdatesRoute: false,
    Routes: [],
    Behaviors: [],
    Services: [],
    InterceptSites: interceptSites,
    UserUsings: [],
    CustomConverters: [],
    LoggingConfiguration: null,
    DiscoverEndpoints: false,
    ExplicitEndpointTypes: []);

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
  /// Gets whether this app has logging configured via AddLogging().
  /// </summary>
  public bool HasLogging => LoggingConfiguration is not null;

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
