namespace TimeWarp.Nuru;

/// <summary>
/// Core CLI app that supports both direct execution and dependency injection.
/// For the full-featured experience, use <see cref="NuruCoreApp"/> from the TimeWarp.Nuru package.
/// </summary>
/// <remarks>
/// This class is split into multiple partial files for maintainability:
/// <list type="bullet">
///   <item><description><c>nuru-core-app.cs</c> - Factory methods, constructors, properties, and RunAsync orchestration</description></item>
///   <item><description><c>nuru-core-app.execution.cs</c> - Mediator and delegate execution methods</description></item>
///   <item><description><c>nuru-core-app.binding.cs</c> - Parameter binding and conversion methods</description></item>
///   <item><description><c>nuru-core-app.validation.cs</c> - Configuration validation and help display methods</description></item>
/// </list>
/// </remarks>
public partial class NuruCoreApp
{
  private readonly IServiceProvider? ServiceProvider;
  private readonly MediatorExecutor? MediatorExecutor;

  #region Static Factory Methods

  /// <summary>
  /// Creates a lightweight builder with Configuration, auto-help, and logging infrastructure.
  /// No DI container, Mediator, REPL, or Completion.
  /// </summary>
  /// <param name="args">Optional command line arguments.</param>
  /// <param name="options">Optional application options.</param>
  /// <returns>A lightweight <see cref="NuruCoreAppBuilder"/>.</returns>
  /// <remarks>
  /// Features included:
  /// - Type converters
  /// - Auto-help generation
  /// - Logging infrastructure
  ///
  /// Features NOT included:
  /// - DI Container
  /// - Mediator pattern
  /// - REPL
  /// - Completion
  /// - OpenTelemetry
  ///
  /// This builder is fully AOT-compatible.
  /// </remarks>
  /// <example>
  /// <code>
  /// NuruCoreAppBuilder builder = NuruCoreApp.CreateSlimBuilder();
  /// builder.Map("greet {name}", (string name) => $"Hello, {name}!");
  /// await builder.Build().RunAsync(args);
  /// </code>
  /// </example>
  public static NuruCoreAppBuilder CreateSlimBuilder(string[]? args = null, NuruCoreApplicationOptions? options = null)
  {
    options ??= new NuruCoreApplicationOptions();
    options.Args = args;
    return new NuruCoreAppBuilder(BuilderMode.Slim, options);
  }

  /// <summary>
  /// Creates a bare minimum builder with only type converters.
  /// User has total control over what features to add.
  /// </summary>
  /// <param name="args">Optional command line arguments.</param>
  /// <param name="options">Optional application options.</param>
  /// <returns>An empty <see cref="NuruCoreAppBuilder"/>.</returns>
  /// <remarks>
  /// Features included:
  /// - Type converters
  /// - Args storage
  ///
  /// Features NOT included (add manually if needed):
  /// - Configuration
  /// - Auto-help
  /// - Logging infrastructure
  /// - DI Container
  /// - Mediator pattern
  /// - REPL
  /// - Completion
  ///
  /// This builder is fully AOT-compatible.
  /// </remarks>
  /// <example>
  /// <code>
  /// NuruCoreAppBuilder builder = NuruCoreApp.CreateEmptyBuilder();
  /// builder.AddTypeConverter(new MyCustomConverter());
  /// builder.Map("cmd", () => "minimal");
  /// await builder.Build().RunAsync(args);
  /// </code>
  /// </example>
  public static NuruCoreAppBuilder CreateEmptyBuilder(string[]? args = null, NuruCoreApplicationOptions? options = null)
  {
    options ??= new NuruCoreApplicationOptions();
    options.Args = args;
    return new NuruCoreAppBuilder(BuilderMode.Empty, options);
  }

  #endregion

  #region Properties

  /// <summary>
  /// Gets the terminal I/O provider for interactive operations like REPL.
  /// </summary>
  public ITerminal Terminal { get; }

  /// <summary>
  /// Gets the logger factory.
  /// </summary>
  public ILoggerFactory LoggerFactory { get; }

  /// <summary>
  /// Gets the collection of registered endpoints.
  /// </summary>
  public EndpointCollection Endpoints { get; }

  /// <summary>
  /// Gets the type converter registry.
  /// </summary>
  public ITypeConverterRegistry TypeConverterRegistry { get; }

  /// <summary>
  /// Gets the REPL configuration options.
  /// </summary>
  public ReplOptions? ReplOptions { get; }

  /// <summary>
  /// Gets the application metadata for help display.
  /// </summary>
  public ApplicationMetadata? AppMetadata { get; }

  /// <summary>
  /// Gets the help configuration options.
  /// </summary>
  public HelpOptions HelpOptions { get; }

  /// <summary>
  /// Gets the session context for tracking REPL vs CLI execution state.
  /// </summary>
  public SessionContext SessionContext { get; }

  #endregion

  #region Constructors

  /// <summary>
  /// Direct constructor - used by NuruAppBuilder for non-DI path.
  /// </summary>
  internal NuruCoreApp
  (
    EndpointCollection endpoints,
    ITypeConverterRegistry typeConverterRegistry,
    ILoggerFactory loggerFactory,
    ITerminal terminal,
    ReplOptions? replOptions = null,
    ApplicationMetadata? appMetadata = null,
    HelpOptions? helpOptions = null,
    SessionContext? sessionContext = null
  )
  {
    Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
    TypeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));
    LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    ReplOptions = replOptions;
    AppMetadata = appMetadata;
    HelpOptions = helpOptions ?? new HelpOptions();
    SessionContext = sessionContext ?? new SessionContext();

    // Create a minimal service provider for delegate parameter injection
    // Resolves NuruCoreApp (for interactive mode), ILoggerFactory, and ILogger<T>
    ServiceProvider = new LightweightServiceProvider(this, loggerFactory);
  }

  /// <summary>
  /// DI constructor - with service provider for Mediator support.
  /// </summary>
  public NuruCoreApp(IServiceProvider serviceProvider)
  {
    ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    Endpoints = serviceProvider.GetRequiredService<EndpointCollection>();
    TypeConverterRegistry = serviceProvider.GetRequiredService<ITypeConverterRegistry>();
    MediatorExecutor = serviceProvider.GetService<MediatorExecutor>();
    LoggerFactory = serviceProvider.GetService<ILoggerFactory>() ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
    ReplOptions = serviceProvider.GetService<ReplOptions>();
    AppMetadata = serviceProvider.GetService<ApplicationMetadata>();
    HelpOptions = serviceProvider.GetService<HelpOptions>() ?? new HelpOptions();
    Terminal = TestTerminalContext.Resolve(serviceProvider.GetService<ITerminal>());
    SessionContext = serviceProvider.GetRequiredService<SessionContext>();
  }

  #endregion

  #region Run Orchestration

  /// <summary>
  /// Runs the application with the provided command line arguments.
  /// </summary>
  /// <param name="args">Command line arguments.</param>
  /// <returns>Exit code (0 for success, non-zero for failure).</returns>
  public async Task<int> RunAsync(string[] args)
  {
    ArgumentNullException.ThrowIfNull(args);

#if NURU_TIMING_DEBUG
    System.Diagnostics.Stopwatch swRunAsync = System.Diagnostics.Stopwatch.StartNew();
#endif

    // Test harness handoff (only top-level)
    if (NuruTestContext.TryExecuteTestRunner(this, out Task<int> testResult))
    {
      return await testResult.ConfigureAwait(false);
    }

    ITerminal effectiveTerminal = TestTerminalContext.Resolve(Terminal);

    // Update session context with terminal color support for help output
    SessionContext.SupportsColor = effectiveTerminal.SupportsColor;

#if NURU_TIMING_DEBUG
    long setupTicks = swRunAsync.ElapsedTicks;
#endif

    try
    {
      // Filter out configuration override args before route matching
      // Configuration overrides follow the pattern --Section:Key=value (must start with -- and contain :)
      // This allows legitimate values with colons (e.g., connection strings like //host:port/db)
      // Using loop instead of LINQ to avoid JIT overhead on cold start
      string[] routeArgs = FilterConfigurationArgs(args);

      // Parse and match route (using filtered args)
      ILogger logger = LoggerFactory.CreateLogger("RouteBasedCommandResolver");
      EndpointResolutionResult result = EndpointResolver.Resolve(routeArgs, Endpoints, TypeConverterRegistry, logger);

#if NURU_TIMING_DEBUG
      long resolveTicks = swRunAsync.ElapsedTicks;
#endif

      // Exit early if route resolution failed
      if (!result.Success || result.MatchedEndpoint is null)
      {
        await effectiveTerminal.WriteErrorLineAsync(
          result.ErrorMessage ?? "No matching command found."
        ).ConfigureAwait(false);

        ShowAvailableCommands(effectiveTerminal);
        return 1;
      }

      if (!await ValidateConfigurationAsync(args, effectiveTerminal).ConfigureAwait(false)) return 1;

#if NURU_TIMING_DEBUG
      long validateTicks = swRunAsync.ElapsedTicks;
#endif

      // Execute based on endpoint strategy
      int executeResult = result.MatchedEndpoint.Strategy switch
      {
        ExecutionStrategy.Mediator when ServiceProvider is null =>
          throw new InvalidOperationException
          (
            $"Command '{result.MatchedEndpoint.RoutePattern}' requires dependency injection. " +
            "Call AddDependencyInjection() before Build()."
          ),

        ExecutionStrategy.Mediator =>
          await ExecuteMediatorCommandAsync
          (
            result.MatchedEndpoint.CommandType!,
            result
          ).ConfigureAwait(false),

        ExecutionStrategy.Delegate =>
          await ExecuteDelegateAsync
          (
            result.MatchedEndpoint.Handler!,
            result.ExtractedValues!,
            result.MatchedEndpoint
          ).ConfigureAwait(false),

        ExecutionStrategy.Invalid =>
          throw new InvalidOperationException
          (
            $"Endpoint '{result.MatchedEndpoint.RoutePattern}' has invalid configuration. " +
            "This is a framework bug."
          ),

        _ => throw new InvalidOperationException("Unknown execution strategy")
      };

#if NURU_TIMING_DEBUG
      long executeTicks = swRunAsync.ElapsedTicks;
      double ticksPerUs = System.Diagnostics.Stopwatch.Frequency / 1_000_000.0;
      Console.WriteLine($"[TIMING RunAsync] Setup={(setupTicks / ticksPerUs):F0}us, Resolve={(resolveTicks - setupTicks) / ticksPerUs:F0}us, Validate={(validateTicks - resolveTicks) / ticksPerUs:F0}us, Execute={(executeTicks - validateTicks) / ticksPerUs:F0}us, Total={(executeTicks / ticksPerUs):F0}us");
#endif

      return executeResult;
    }
#pragma warning disable CA1031 // Do not catch general exception types
    // This is the top-level exception handler for the CLI app. We need to catch all exceptions
    // to provide meaningful error messages to users rather than crashing.
    catch (Exception ex)
#pragma warning restore CA1031
    {
      await Terminal.WriteErrorLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
      return 1;
    }
  }

  #endregion

  [GeneratedRegex(@"^--[\w-]+:[\w:-]+", RegexOptions.Compiled)]
  private static partial Regex ConfigurationOverrideRegex();
}
