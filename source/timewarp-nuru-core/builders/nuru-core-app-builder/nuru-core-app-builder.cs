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
    // V2: DSL is parsed at compile-time. RunAsync() is intercepted.
    return new NuruCoreApp(Terminal);
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
