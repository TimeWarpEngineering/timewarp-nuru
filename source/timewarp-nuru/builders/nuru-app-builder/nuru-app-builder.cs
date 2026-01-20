namespace TimeWarp.Nuru;

/// <summary>
/// Builder for configuring Nuru applications.
/// Combines lightweight configuration with IHostApplicationBuilder support.
/// </summary>
public partial class NuruAppBuilder
{
  private protected readonly TypeConverterRegistry TypeConverterRegistry = new();
  private protected IConfiguration? Configuration;
  private protected HelpOptions HelpOptions = new();
  private protected ILoggerFactory? LoggerFactory;
  private protected ReplOptions? ReplOptions;
  private protected ServiceCollection? ServiceCollection;
  private protected ITerminal? Terminal;

  /// <summary>
  /// Callback to configure completion source registry at runtime.
  /// Set by EnableCompletion(), invoked by generated code during app initialization.
  /// </summary>
  internal Action<CompletionSourceRegistry>? CompletionRegistryConfiguration { get; set; }

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
  public virtual NuruAppBuilder AddBehavior(Type behaviorType)
  {
    // No-op at runtime - the source generator extracts behavior info at compile-time
    // and generates the pipeline wrapping code in the interceptor.
    _ = behaviorType; // Suppress unused parameter warning
    return this;
  }

  /// <summary>
  /// Sets the application name for help display.
  /// </summary>
  /// <param name="name">The application name.</param>
  public virtual NuruAppBuilder WithName(string name)
  {
    // No-op at runtime - the source generator extracts the name at compile-time
    _ = name;
    return this;
  }

  /// <summary>
  /// Sets the application description for help display.
  /// </summary>
  /// <param name="description">The application description.</param>
  public virtual NuruAppBuilder WithDescription(string description)
  {
    // No-op at runtime - the source generator extracts the description at compile-time
    _ = description;
    return this;
  }

  /// <summary>
  /// Configures help output filtering and display options.
  /// </summary>
  /// <param name="configure">Action to configure help options.</param>
  public virtual NuruAppBuilder ConfigureHelp(Action<HelpOptions> configure)
  {
    ArgumentNullException.ThrowIfNull(configure);
    configure(HelpOptions);
    return this;
  }

  /// <summary>
  /// Builds and returns a runnable NuruApp.
  /// </summary>
  public NuruApp Build()
  {
    // DSL is parsed at compile-time. RunAsync() is intercepted.
    return new NuruApp(Terminal)
    {
      ReplOptions = ReplOptions,
      LoggerFactory = LoggerFactory,
      CompletionRegistryConfiguration = CompletionRegistryConfiguration
    };
  }
}
