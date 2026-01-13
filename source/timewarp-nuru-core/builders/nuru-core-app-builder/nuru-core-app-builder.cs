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
  /// Call AddDependencyInjection() first to enable DI support.
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
  /// Registers a pipeline behavior that wraps handler execution.
  /// Behaviors are instantiated once (Singleton) and called in registration order.
  /// This method is analyzed at compile-time by the source generator.
  /// </summary>
  /// <param name="behaviorType">The type implementing <see cref="INuruBehavior"/>.</param>
  /// <returns>The builder for method chaining.</returns>
  /// <example>
  /// <code>
  /// NuruApp.CreateBuilder(args)
  ///   .AddBehavior(typeof(LoggingBehavior))
  ///   .AddBehavior(typeof(PerformanceBehavior))
  ///   .Map("ping").WithHandler(() => "pong").Done()
  ///   .Build();
  /// </code>
  /// </example>
  public virtual TSelf AddBehavior(Type behaviorType)
  {
    // No-op at runtime - the source generator extracts behavior info at compile-time
    // and generates the pipeline wrapping code in the interceptor.
    _ = behaviorType; // Suppress unused parameter warning
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the application name for help display.
  /// </summary>
  /// <param name="name">The application name.</param>
  public virtual TSelf WithName(string name)
  {
    // No-op at runtime - the source generator extracts the name at compile-time
    _ = name;
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the application description for help display.
  /// </summary>
  /// <param name="description">The application description.</param>
  public virtual TSelf WithDescription(string description)
  {
    // No-op at runtime - the source generator extracts the description at compile-time
    _ = description;
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
    // DSL is parsed at compile-time. RunAsync() is intercepted.
    return new NuruCoreApp(Terminal);
  }
}
