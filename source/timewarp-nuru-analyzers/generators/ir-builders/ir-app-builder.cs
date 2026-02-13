// IR builder that mirrors the NuruAppBuilder DSL for semantic interpretation.
//
// This builder is used by the DslInterpreter to "execute" the DSL at design time,
// accumulating state that will be converted to an AppModel.
//
// Key design:
// - CRTP pattern matches NuruAppBuilder<TSelf>
// - Method names mirror DSL methods exactly
// - Build() marks as built but doesn't finalize (for RunAsync intercept sites)
// - FinalizeModel() creates the actual AppModel
// - Implements IIrAppBuilder for polymorphic dispatch in interpreter

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// IR builder that mirrors the NuruAppBuilder DSL for semantic interpretation.
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
  private LoggingConfiguration? LoggingConfiguration;
  private readonly List<RouteDefinition> Routes = [];
  private readonly List<BehaviorDefinition> Behaviors = [];
  private readonly List<ServiceDefinition> Services = [];
  private readonly Dictionary<string, List<InterceptSiteModel>> InterceptSitesByMethod = [];
  private readonly List<CustomConverterDefinition> CustomConverters = [];
  private bool IsBuilt;
  private bool DiscoverEndpointsEnabled;
  private bool TelemetryEnabled;
  private bool CompletionEnabled;
  private bool MicrosoftDependencyInjectionEnabled;
  private string? ConfigureServicesBody;
  private readonly List<string> ExplicitEndpointTypes = [];
  private readonly List<ExtensionMethodCall> ExtensionMethods = [];
  private readonly List<string> FilterGroupTypeNames = [];
  private readonly List<HttpClientConfiguration> HttpClientConfigurations = [];

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
  /// Mirrors: NuruAppBuilder.WithName()
  /// </summary>
  public TSelf WithName(string name)
  {
    Name = name;
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the application description.
  /// Mirrors: NuruAppBuilder.WithDescription()
  /// </summary>
  public TSelf WithDescription(string description)
  {
    Description = description;
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the AI prompt for --capabilities output.
  /// Mirrors: NuruAppBuilder.WithAiPrompt()
  /// </summary>
  public TSelf WithAiPrompt(string aiPrompt)
  {
    AiPrompt = aiPrompt;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables help with default options.
  /// Mirrors: NuruAppBuilder.AddHelp()
  /// </summary>
  public TSelf AddHelp()
  {
    HasHelp = true;
    HelpOptions = HelpModel.Default;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables help with custom options.
  /// Mirrors: NuruAppBuilder.AddHelp(Action&lt;HelpOptions&gt;)
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
  /// Mirrors: NuruAppBuilder.AddRepl()
  /// </summary>
  public TSelf AddRepl()
  {
    HasRepl = true;
    ReplOptions = ReplModel.Default;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables REPL with custom options.
  /// Mirrors: NuruAppBuilder.AddRepl(Action&lt;ReplOptions&gt;)
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
  /// Mirrors: NuruAppBuilder.AddConfiguration()
  /// </summary>
  public TSelf AddConfiguration()
  {
    HasConfiguration = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the logging configuration.
  /// Called when AddLogging(...) is detected in ConfigureServices.
  /// </summary>
  /// <param name="config">The logging configuration.</param>
  public TSelf SetLoggingConfiguration(LoggingConfiguration config)
  {
    LoggingConfiguration = config;
    return (TSelf)this;
  }

  /// <summary>
  /// Adds an HttpClient configuration from AddHttpClient().
  /// </summary>
  /// <param name="config">The HttpClient configuration.</param>
  public TSelf AddHttpClientConfiguration(HttpClientConfiguration config)
  {
    HttpClientConfigurations.Add(config);
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
  /// Mirrors: NuruAppBuilder.AddBehavior(Type)
  /// </summary>
  /// <param name="behavior">The behavior definition.</param>
  public TSelf AddBehavior(BehaviorDefinition behavior)
  {
    Behaviors.Add(behavior);
    return (TSelf)this;
  }

  /// <summary>
  /// Adds a service registration.
  /// Mirrors: NuruAppBuilder.ConfigureServices()
  /// </summary>
  /// <param name="service">The service definition.</param>
  public TSelf AddService(ServiceDefinition service)
  {
    Services.Add(service);
    return (TSelf)this;
  }

  /// <summary>
  /// Adds an extension method call detected during service extraction.
  /// Used for NURU052 warnings about unanalyzable service registrations.
  /// </summary>
  /// <param name="extensionMethod">The extension method call.</param>
  public TSelf AddExtensionMethodCall(ExtensionMethodCall extensionMethod)
  {
    ExtensionMethods.Add(extensionMethod);
    return (TSelf)this;
  }

  /// <summary>
  /// No-op for UseTerminal (runtime only).
  /// Mirrors: NuruAppBuilder.UseTerminal()
  /// </summary>
  public TSelf UseTerminal()
  {
    // No-op - terminal is runtime only
    return (TSelf)this;
  }

  /// <summary>
  /// Enables telemetry (OpenTelemetry instrumentation).
  /// Mirrors: NuruAppBuilder.UseTelemetry()
  /// </summary>
  public TSelf UseTelemetry()
  {
    TelemetryEnabled = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables shell completion support.
  /// Mirrors: NuruAppBuilderCompletionExtensions.EnableCompletion()
  /// </summary>
  public TSelf EnableCompletion()
  {
    CompletionEnabled = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables runtime Microsoft.Extensions.DependencyInjection instead of source-gen DI.
  /// Mirrors: NuruAppBuilder.UseMicrosoftDependencyInjection()
  /// </summary>
  public TSelf UseMicrosoftDependencyInjection()
  {
    MicrosoftDependencyInjectionEnabled = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the raw ConfigureServices lambda body for runtime invocation.
  /// Used when UseMicrosoftDependencyInjection is enabled.
  /// </summary>
  /// <param name="lambdaBody">The lambda body source text.</param>
  public TSelf SetConfigureServicesBody(string lambdaBody)
  {
    ConfigureServicesBody = lambdaBody;
    return (TSelf)this;
  }

  /// <summary>
  /// Registers a custom type converter for code generation.
  /// Mirrors: NuruAppBuilder.AddTypeConverter()
  /// </summary>
  /// <param name="converter">The converter definition.</param>
  public TSelf AddTypeConverter(CustomConverterDefinition converter)
  {
    CustomConverters.Add(converter);
    return (TSelf)this;
  }

  /// <summary>
  /// Begins mapping a new route.
  /// Mirrors: NuruAppBuilder.Map()
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
  /// Mirrors: NuruAppBuilder.WithGroupPrefix()
  /// </summary>
  /// <param name="prefix">The prefix for all routes in this group.</param>
  /// <returns>A group builder for configuring nested routes.</returns>
  public IrGroupBuilder<TSelf> WithGroupPrefix(string prefix)
  {
    return new IrGroupBuilder<TSelf>((TSelf)this, prefix, RegisterRoute);
  }

  /// <summary>
  /// Marks that all [NuruRoute] endpoints should be discovered and included.
  /// Mirrors: NuruAppBuilder.DiscoverEndpoints()
  /// </summary>
  public TSelf DiscoverEndpoints()
  {
    DiscoverEndpointsEnabled = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Marks that [NuruRoute] endpoints should be discovered and filtered by group types.
  /// Mirrors: NuruAppBuilder.DiscoverEndpoints(params Type[])
  /// </summary>
  /// <param name="groupTypeNames">Fully qualified type names of the group classes to filter by.</param>
  public TSelf DiscoverEndpoints(ImmutableArray<string> groupTypeNames)
  {
    DiscoverEndpointsEnabled = true;
    FilterGroupTypeNames.AddRange(groupTypeNames);
    return (TSelf)this;
  }

  /// <summary>
  /// Adds a specific endpoint type to include.
  /// Mirrors: NuruAppBuilder.Map&lt;TEndpoint&gt;()
  /// </summary>
  /// <param name="endpointTypeName">Fully qualified type name of the endpoint.</param>
  public TSelf MapEndpoint(string endpointTypeName)
  {
    ExplicitEndpointTypes.Add(endpointTypeName);
    return (TSelf)this;
  }

  /// <summary>
  /// Adds an intercept site for a specific method.
  /// Called by interpreter when entry point calls are encountered.
  /// </summary>
  /// <param name="methodName">The method name (e.g., "RunAsync", "RunReplAsync").</param>
  /// <param name="site">The intercept site model.</param>
  public TSelf AddInterceptSite(string methodName, InterceptSiteModel site)
  {
    if (!InterceptSitesByMethod.TryGetValue(methodName, out List<InterceptSiteModel>? sites))
    {
      sites = [];
      InterceptSitesByMethod[methodName] = sites;
    }

    sites.Add(site);
    return (TSelf)this;
  }

  /// <summary>
  /// Marks the builder as "built".
  /// Mirrors: NuruAppBuilder.Build()
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
      Version: null,      // Populated later from AssemblyMetadataExtractor
      CommitHash: null,   // Populated later from AssemblyMetadataExtractor
      CommitDate: null,   // Populated later from AssemblyMetadataExtractor
      HasHelp: HasHelp,
      HelpOptions: HelpOptions,
      HasRepl: HasRepl,
      ReplOptions: ReplOptions,
      HasConfiguration: HasConfiguration,
      HasCheckUpdatesRoute: HasCheckUpdatesRoute,
      Routes: [.. Routes],
      Behaviors: [.. Behaviors],
      Services: [.. Services],
      InterceptSitesByMethod: InterceptSitesByMethod.ToImmutableDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.ToImmutableArray()),
      UserUsings: [],  // Usings are populated by AppExtractor, not the builder
      CustomConverters: [.. CustomConverters],
      LoggingConfiguration: LoggingConfiguration,
      DiscoverEndpoints: DiscoverEndpointsEnabled,
      ExplicitEndpointTypes: [.. ExplicitEndpointTypes],
      HasTelemetry: TelemetryEnabled,
      HasCompletion: CompletionEnabled,
      UseMicrosoftDependencyInjection: MicrosoftDependencyInjectionEnabled,
      ConfigureServicesLambdaBody: ConfigureServicesBody,
      ExtensionMethods: [.. ExtensionMethods],
      FilterGroupTypeNames: [.. FilterGroupTypeNames]);
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
  IIrAppBuilder IIrAppBuilder.SetLoggingConfiguration(LoggingConfiguration config) => SetLoggingConfiguration(config);
  IIrAppBuilder IIrAppBuilder.AddHttpClientConfiguration(HttpClientConfiguration config) => AddHttpClientConfiguration(config);
  IIrAppBuilder IIrAppBuilder.AddCheckUpdatesRoute() => AddCheckUpdatesRoute();
  IIrAppBuilder IIrAppBuilder.AddBehavior(BehaviorDefinition behavior) => AddBehavior(behavior);
  IIrAppBuilder IIrAppBuilder.AddService(ServiceDefinition service) => AddService(service);
  IIrAppBuilder IIrAppBuilder.AddExtensionMethodCall(ExtensionMethodCall extensionMethod) => AddExtensionMethodCall(extensionMethod);
  IIrAppBuilder IIrAppBuilder.UseTerminal() => UseTerminal();
  IIrAppBuilder IIrAppBuilder.UseTelemetry() => UseTelemetry();
  IIrAppBuilder IIrAppBuilder.UseMicrosoftDependencyInjection() => UseMicrosoftDependencyInjection();
  IIrAppBuilder IIrAppBuilder.SetConfigureServicesBody(string lambdaBody) => SetConfigureServicesBody(lambdaBody);
  IIrAppBuilder IIrAppBuilder.AddTypeConverter(CustomConverterDefinition converter) => AddTypeConverter(converter);
  IIrAppBuilder IIrAppBuilder.AddInterceptSite(string methodName, InterceptSiteModel site) => AddInterceptSite(methodName, site);
  IIrAppBuilder IIrAppBuilder.DiscoverEndpoints() => DiscoverEndpoints();
  IIrAppBuilder IIrAppBuilder.DiscoverEndpoints(ImmutableArray<string> groupTypeNames) => DiscoverEndpoints(groupTypeNames);
  IIrAppBuilder IIrAppBuilder.MapEndpoint(string endpointTypeName) => MapEndpoint(endpointTypeName);
  IIrAppBuilder IIrAppBuilder.EnableCompletion() => EnableCompletion();
}

/// <summary>
/// Non-generic convenience type for direct use.
/// </summary>
public sealed class IrAppBuilder : IrAppBuilder<IrAppBuilder> { }
