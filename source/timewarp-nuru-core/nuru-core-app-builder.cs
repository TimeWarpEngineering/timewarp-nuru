namespace TimeWarp.Nuru;

/// <summary>
/// Core builder for configuring lightweight Nuru applications.
/// For full-featured applications with IHostApplicationBuilder support, use NuruAppBuilder from TimeWarp.Nuru package.
/// </summary>
/// <typeparam name="TSelf">The derived builder type for fluent API support (CRTP pattern).</typeparam>
public partial class NuruCoreAppBuilder<TSelf>
  where TSelf : NuruCoreAppBuilder<TSelf>
{
  private protected readonly TypeConverterRegistry TypeConverterRegistry = new();
  private protected ApplicationMetadata? AppMetadata;
  private protected bool AutoHelpEnabled;
  private protected IConfiguration? Configuration;
  private protected HelpOptions HelpOptions = new();
  private protected ILoggerFactory? LoggerFactory;
  private protected ReplOptions? ReplOptions;
  private protected ServiceCollection? ServiceCollection;
  private protected ITerminal? Terminal;

  /// <summary>
  /// Gets the collection of registered endpoints.
  /// </summary>
  public EndpointCollection EndpointCollection { get; } = [];

  /// <summary>
  /// Gets the service collection. Throws if dependency injection has not been added.
  /// Call AddDependencyInjection() first to enable DI and Mediator support.
  /// </summary>
  public IServiceCollection Services
  {
    get
    {
      if (ServiceCollection is null)
      {
        throw new InvalidOperationException(
          "Dependency injection has not been enabled. Call AddDependencyInjection() first.");
      }

      return ServiceCollection;
    }
  }

  /// <summary>
  /// Enables automatic help generation for all routes.
  /// Help routes will be generated at build time.
  /// </summary>
  public virtual TSelf AddAutoHelp()
  {
    AutoHelpEnabled = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Configures help output filtering and display options.
  /// </summary>
  /// <param name="configure">Action to configure help options.</param>
  public virtual TSelf ConfigureHelp(Action<HelpOptions> configure)
  {
    ArgumentNullException.ThrowIfNull(configure);
    configure(HelpOptions);
    return (TSelf)this;
  }

  /// <summary>
  /// Builds and returns a runnable NuruCoreApp.
  /// </summary>
  public NuruCoreApp Build()
  {
    // Add routes from NuruRouteRegistry (auto-registered via [NuruRoute] attributes)
    // Build HashSet of existing patterns for O(1) lookup instead of LINQ Any() which is O(n)
    HashSet<string> existingPatterns = new(StringComparer.OrdinalIgnoreCase);
    foreach (Endpoint endpoint in EndpointCollection.Endpoints)
    {
      existingPatterns.Add(endpoint.RoutePattern);
    }

    foreach (RegisteredRoute registered in NuruRouteRegistry.RegisteredRoutes)
    {
      // Skip if this exact pattern is already mapped (user explicit Map() takes precedence)
      if (existingPatterns.Contains(registered.Pattern))
        continue;

      Endpoint endpoint = new()
      {
        RoutePattern = registered.Pattern,
        CompiledRoute = registered.CompiledRoute,
        Description = registered.Description,
        CommandType = registered.RequestType,
        MessageType = registered.CompiledRoute.MessageType
      };

      EndpointCollection.Add(endpoint);
    }

    if (AutoHelpEnabled)
    {
      HelpRouteGenerator.GenerateHelpRoutes(this, EndpointCollection, AppMetadata, HelpOptions);
    }

    EndpointCollection.Sort();

    // Use NullLoggerFactory if none provided (zero overhead)
    ILoggerFactory loggerFactory = LoggerFactory ?? NullLoggerFactory.Instance;

    if (ServiceCollection is not null)
    {
      // DI path - register logger factory and build service provider
      ServiceCollection.AddSingleton(loggerFactory);

      // Register ILogger<T> generic implementation (matches Microsoft.Extensions.Logging behavior)
      ServiceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

      // Register NuruCoreAppHolder for deferred app access (needed for interactive mode route)
      NuruCoreAppHolder appHolder = new();
      ServiceCollection.AddSingleton(appHolder);

      // Register REPL options if configured
      if (ReplOptions is not null)
      {
        ServiceCollection.AddSingleton(ReplOptions);
      }

      // Register help options
      ServiceCollection.AddSingleton(HelpOptions);

      // Register app metadata if configured
      if (AppMetadata is not null)
      {
        ServiceCollection.AddSingleton(AppMetadata);
      }

      // Register terminal (use configured terminal or default)
      ServiceCollection.AddSingleton<ITerminal>(Terminal ?? TimeWarpTerminal.Default);

      ServiceProvider serviceProvider = ServiceCollection.BuildServiceProvider();

      NuruCoreApp app = new(serviceProvider);
      appHolder.SetApp(app);
      return app;
    }
    else
    {
      // Direct path - return lightweight app without DI
      // Create a shared SessionContext for REPL/CLI context tracking
      SessionContext sessionContext = new();
      return new NuruCoreApp(
        EndpointCollection,
        TypeConverterRegistry,
        loggerFactory,
        TestTerminalContext.Resolve(Terminal),
        ReplOptions,
        AppMetadata,
        HelpOptions,
        sessionContext);
    }
  }

  /// <summary>
  /// Sets the application metadata for help display.
  /// </summary>
  /// <param name="name">The application name. If null, will be auto-detected.</param>
  /// <param name="description">The application description.</param>
  public virtual TSelf WithMetadata(string? name = null, string? description = null)
  {
    AppMetadata = new ApplicationMetadata(name, description);
    return (TSelf)this;
  }
}
