namespace TimeWarp.Nuru;

/// <summary>
/// Configuration, dependency injection, and service methods for NuruCoreAppBuilder.
/// </summary>
public partial class NuruCoreAppBuilder<TSelf>
{
  /// <summary>
  /// Adds standard .NET configuration sources to the application.
  /// </summary>
  /// <param name="args">Optional command line arguments to include in configuration.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// This method is interpreted by the source generator at compile time.
  /// The generated code builds configuration from:
  /// <list type="bullet">
  /// <item><description>appsettings.json</description></item>
  /// <item><description>appsettings.{Environment}.json</description></item>
  /// <item><description>{ApplicationName}.settings.json</description></item>
  /// <item><description>{ApplicationName}.settings.{Environment}.json</description></item>
  /// <item><description>User secrets (Development/DEBUG builds only)</description></item>
  /// <item><description>Environment variables</description></item>
  /// <item><description>Command line arguments</description></item>
  /// </list>
  ///
  /// Configuration sources are loaded in order (later sources override earlier ones).
  /// The resulting IConfiguration is available for injection into handlers.
  ///
  /// To use user secrets in runfiles, add:
  /// <code>
  /// #:property UserSecretsId=your-guid-here
  /// </code>
  /// </remarks>
  public virtual TSelf AddConfiguration(string[]? args = null)
  {
    // This method is interpreted by the source generator at compile time.
    // The actual configuration loading is emitted as generated code.
    // This stub exists for API compatibility.
    return (TSelf)this;
  }

  /// <summary>
  /// Configures services using the provided action, enabling fluent service registration
  /// while maintaining the builder chain.
  /// </summary>
  /// <param name="configure">The action to configure services.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// NuruCoreApp app = NuruApp.CreateBuilder([])
  ///   .AddDependencyInjection()
  ///   .ConfigureServices(services =>
  ///   {
  ///     services.AddSingleton&lt;ICalculator, Calculator&gt;();
  ///     services.AddLogging(config => config.AddConsole());
  ///     services.Configure&lt;AppOptions&gt;(Configuration.GetSection("App"));
  ///   })
  ///   .Map&lt;Command&gt;("route")
  ///   .Build();
  /// </code>
  /// </example>
  public virtual TSelf ConfigureServices(Action<IServiceCollection> configure)
  {
    // This method is interpreted by the source generator at compile time.
    // The generated code handles service registration via static instantiation.
    // This stub exists for API compatibility - it's a no-op at runtime.
    return (TSelf)this;
  }

  /// <summary>
  /// Configures services using the provided action with access to configuration,
  /// enabling fluent service registration while maintaining the builder chain.
  /// </summary>
  /// <param name="configure">The action to configure services with access to configuration (may be null if AddConfiguration not called).</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// NuruCoreApp app = NuruApp.CreateBuilder([])
  ///   .AddDependencyInjection()
  ///   .AddConfiguration(args)
  ///   .ConfigureServices((services, config) =>
  ///   {
  ///     if (config != null)
  ///     {
  ///       services.AddDbContext&lt;MyDbContext&gt;(options =>
  ///         options.UseSqlServer(config.GetConnectionString("Default")));
  ///     }
  ///   })
  ///   .Map&lt;Command&gt;("route")
  ///   .Build();
  /// </code>
  /// </example>
  public virtual TSelf ConfigureServices(Action<IServiceCollection, IConfiguration?> configure)
  {
    // This method is interpreted by the source generator at compile time.
    // The generated code handles service registration via static instantiation.
    // This stub exists for API compatibility - it's a no-op at runtime.
    return (TSelf)this;
  }

  /// <summary>
  /// Configures logging for the application using the provided ILoggerFactory.
  /// If not called, NullLoggerFactory is used (zero overhead).
  /// </summary>
  /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
  public virtual TSelf UseLogging(ILoggerFactory loggerFactory)
  {
    ArgumentNullException.ThrowIfNull(loggerFactory);
    LoggerFactory = loggerFactory;
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the terminal I/O provider for interactive operations like REPL.
  /// If not called, defaults to <see cref="TimeWarpTerminal.Default"/>.
  /// </summary>
  /// <param name="terminal">The terminal implementation to use.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// // For testing REPL
  /// TestTerminal terminal = new();
  /// terminal.QueueLine("help");
  /// terminal.QueueLine("exit");
  ///
  /// NuruCoreApp app = NuruApp.CreateBuilder([])
  ///     .UseTerminal(terminal)
  ///     .AddReplSupport()
  ///     .Build();
  /// </code>
  /// </example>
  public virtual TSelf UseTerminal(ITerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    Terminal = terminal;
    return (TSelf)this;
  }
}
