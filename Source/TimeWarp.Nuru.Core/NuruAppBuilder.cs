namespace TimeWarp.Nuru;

/// <summary>
/// Unified builder for configuring Nuru applications with or without dependency injection.
/// </summary>
public partial class NuruAppBuilder : IDisposable
{
  private readonly TypeConverterRegistry TypeConverterRegistry = new();
  private ApplicationMetadata? AppMetadata;
  private bool AutoHelpEnabled;
  private IConfiguration? Configuration;
  private ILoggerFactory? LoggerFactory;
  private ReplOptions? ReplOptions;
  private ServiceCollection? ServiceCollection;
  private ITerminal? Terminal;

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
  public NuruAppBuilder AddAutoHelp()
  {
    AutoHelpEnabled = true;
    return this;
  }

  /// <summary>
  /// Builds and returns a runnable NuruCoreApp.
  /// </summary>
  public NuruCoreApp Build()
  {
    if (AutoHelpEnabled)
    {
      HelpRouteGenerator.GenerateHelpRoutes(this, EndpointCollection, AppMetadata);
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

      // Register app metadata if configured
      if (AppMetadata is not null)
      {
        ServiceCollection.AddSingleton(AppMetadata);
      }

      // Register terminal if configured
      if (Terminal is not null)
      {
        ServiceCollection.AddSingleton<ITerminal>(Terminal);
      }

      ServiceProvider serviceProvider = ServiceCollection.BuildServiceProvider();

      NuruCoreApp app = new(serviceProvider);
      appHolder.SetApp(app);
      return app;
    }
    else
    {
      // Direct path - return lightweight app without DI
      return new NuruCoreApp(
        EndpointCollection,
        TypeConverterRegistry,
        loggerFactory,
        NuruConsole.Default,
        Terminal ?? NuruTerminal.Default,
        ReplOptions,
        AppMetadata);
    }
  }

  /// <summary>
  /// Sets the application metadata for help display.
  /// </summary>
  /// <param name="name">The application name. If null, will be auto-detected.</param>
  /// <param name="description">The application description.</param>
  public NuruAppBuilder WithMetadata(string? name = null, string? description = null)
  {
    AppMetadata = new ApplicationMetadata(name, description);
    return this;
  }

  /// <summary>
  /// Disposes resources used by the builder.
  /// </summary>
  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Disposes resources used by the builder.
  /// </summary>
  /// <param name="disposing">True if called from Dispose(), false if from finalizer.</param>
  protected virtual void Dispose(bool disposing)
  {
    if (disposing)
    {
      ConfigurationManager?.Dispose();
    }
  }
}
