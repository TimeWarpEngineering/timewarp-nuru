namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Top-level model passed to the emitter containing all apps and shared metadata.
/// Supports multiple isolated apps with per-app interceptor methods.
/// </summary>
/// <param name="Apps">Individual app models, each with isolated routes and intercept sites</param>
/// <param name="UserUsings">User's using directives to include in generated code</param>
/// <param name="Endpoints">Routes from [NuruRoute] endpoint classes (shared across all apps)</param>
/// <param name="Version">Assembly version (from AssemblyInformationalVersionAttribute or AssemblyVersion)</param>
/// <param name="CommitHash">Git commit hash (from TimeWarp.Build.Tasks, may be null)</param>
/// <param name="CommitDate">Git commit date (from TimeWarp.Build.Tasks, may be null)</param>
public sealed record GeneratorModel(
  ImmutableArray<AppModel> Apps,
  ImmutableArray<string> UserUsings,
  ImmutableArray<RouteDefinition> Endpoints,
  string? Version,
  string? CommitHash,
  string? CommitDate)
{
  /// <summary>
  /// Gets whether any app has help enabled.
  /// </summary>
  public bool HasHelp => Apps.Any(a => a.HasHelp);

  /// <summary>
  /// Gets whether any app has REPL enabled.
  /// </summary>
  public bool HasRepl => Apps.Any(a => a.HasRepl);

  /// <summary>
  /// Gets whether any app has configuration enabled.
  /// </summary>
  public bool HasConfiguration => Apps.Any(a => a.HasConfiguration);

  /// <summary>
  /// Gets whether any app has check-updates route enabled.
  /// </summary>
  public bool HasCheckUpdatesRoute => Apps.Any(a => a.HasCheckUpdatesRoute);

  /// <summary>
  /// Gets whether any app has telemetry enabled.
  /// </summary>
  public bool HasTelemetry => Apps.Any(a => a.HasTelemetry);

  /// <summary>
  /// Gets whether any app uses runtime Microsoft DI instead of source-gen DI.
  /// </summary>
  public bool UsesMicrosoftDependencyInjection => Apps.Any(a => a.UseMicrosoftDependencyInjection);

  /// <summary>
  /// Gets whether any non-runtime-DI app has services with constructor dependencies.
  /// When true, runtime DI infrastructure must be emitted for these services.
  /// </summary>
  public bool NeedsRuntimeDIForConstructorDependencies =>
    Apps.Any(a => !a.UseMicrosoftDependencyInjection && a.Services.Any(s => s.HasConstructorDependencies));

  /// <summary>
  /// Gets whether runtime DI infrastructure needs to be emitted (either for explicit
  /// UseMicrosoftDependencyInjection calls or for services with constructor dependencies).
  /// </summary>
  public bool NeedsRuntimeDIInfrastructure => UsesMicrosoftDependencyInjection || NeedsRuntimeDIForConstructorDependencies;

  /// <summary>
  /// Gets all behaviors from all apps (deduplicated).
  /// </summary>
  public IEnumerable<BehaviorDefinition> AllBehaviors =>
    Apps.SelectMany(a => a.Behaviors).DistinctBy(b => b.FullTypeName);

  /// <summary>
  /// Gets all services from all apps (deduplicated).
  /// </summary>
  public IEnumerable<ServiceDefinition> AllServices =>
    Apps.SelectMany(a => a.Services).DistinctBy(s => s.ImplementationTypeName);

  /// <summary>
  /// Gets all custom converters from all apps (deduplicated).
  /// </summary>
  public IEnumerable<CustomConverterDefinition> AllConverters =>
    Apps.SelectMany(a => a.CustomConverters).DistinctBy(c => c.ConverterTypeName);

  /// <summary>
  /// Gets the first app's name (for help/version output).
  /// </summary>
  public string? Name => Apps.FirstOrDefault(a => a.Name is not null)?.Name;

  /// <summary>
  /// Gets the first app's description (for help output).
  /// </summary>
  public string? Description => Apps.FirstOrDefault(a => a.Description is not null)?.Description;

  /// <summary>
  /// Gets the first app's AI prompt (for --capabilities output).
  /// </summary>
  public string? AiPrompt => Apps.FirstOrDefault(a => a.AiPrompt is not null)?.AiPrompt;

  /// <summary>
  /// Gets help options from the first app that has them.
  /// </summary>
  public HelpModel? HelpOptions => Apps.FirstOrDefault(a => a.HelpOptions is not null)?.HelpOptions;

  /// <summary>
  /// Gets REPL options from the first app that has them.
  /// </summary>
  public ReplModel? ReplOptions => Apps.FirstOrDefault(a => a.ReplOptions is not null)?.ReplOptions;

  /// <summary>
  /// Gets all routes from all apps combined (for help output).
  /// </summary>
  public IEnumerable<RouteDefinition> AllRoutes =>
    Apps.SelectMany(a => a.Routes).Concat(Endpoints);
}
