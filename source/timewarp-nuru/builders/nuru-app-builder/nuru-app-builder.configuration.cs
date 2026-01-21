namespace TimeWarp.Nuru;

/// <summary>
/// Configuration, dependency injection, and service methods for NuruAppBuilder.
/// </summary>
public partial class NuruAppBuilder
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
  public virtual NuruAppBuilder AddConfiguration(string[]? args = null)
  {
    // This method is interpreted by the source generator at compile time.
    // The actual configuration loading is emitted as generated code.
    // This stub exists for API compatibility.
    return this;
  }

  /// <summary>
  /// Configures services using the provided action, enabling fluent service registration
  /// while maintaining the builder chain.
  /// </summary>
  /// <param name="configure">The action to configure services.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// NuruApp app = NuruApp.CreateBuilder([])
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
  public virtual NuruAppBuilder ConfigureServices(Action<IServiceCollection> configure)
  {
    // This method is interpreted by the source generator at compile time.
    // The generated code handles service registration via static instantiation.
    // This stub exists for API compatibility - it's a no-op at runtime.
    return this;
  }

  /// <summary>
  /// Configures services using the provided action with access to configuration,
  /// enabling fluent service registration while maintaining the builder chain.
  /// </summary>
  /// <param name="configure">The action to configure services with access to configuration (may be null if AddConfiguration not called).</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// NuruApp app = NuruApp.CreateBuilder([])
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
  public virtual NuruAppBuilder ConfigureServices(Action<IServiceCollection, IConfiguration?> configure)
  {
    // This method is interpreted by the source generator at compile time.
    // The generated code handles service registration via static instantiation.
    // This stub exists for API compatibility - it's a no-op at runtime.
    return this;
  }

  /// <summary>
  /// Configures logging for the application using the provided ILoggerFactory.
  /// If not called, NullLoggerFactory is used (zero overhead).
  /// </summary>
  /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
  public virtual NuruAppBuilder UseLogging(ILoggerFactory loggerFactory)
  {
    ArgumentNullException.ThrowIfNull(loggerFactory);
    LoggerFactory = loggerFactory;
    return this;
  }

  /// <summary>
  /// Sets the terminal I/O provider for interactive operations like REPL.
  /// If not called, defaults to <see cref="TimeWarpTerminal.Default"/>
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
  /// NuruApp app = NuruApp.CreateBuilder([])
  ///     .UseTerminal(terminal)
  ///     .AddReplSupport()
  ///     .Build();
  /// </code>
  /// </example>
  public virtual NuruAppBuilder UseTerminal(ITerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    Terminal = terminal;
    return this;
  }

  /// <summary>
  /// Configures OpenTelemetry with OTLP export. When not called, all telemetry
  /// code is eliminated by AOT trimming.
  /// </summary>
  /// <remarks>
  /// This method is interpreted by the source generator at compile time.
  /// When called, the generator emits:
  /// <list type="bullet">
  /// <item><description>ActivitySource and Meter infrastructure</description></item>
  /// <item><description>TracerProvider and MeterProvider setup with OTLP export</description></item>
  /// <item><description>Command instrumentation (spans, metrics)</description></item>
  /// <item><description>Automatic telemetry flush on RunAsync completion</description></item>
  /// </list>
  /// When not called, none of this code is generated, making OpenTelemetry packages
  /// fully trimmable by the AOT linker.
  /// </remarks>
  public virtual NuruAppBuilder UseTelemetry()
  {
    // This method is interpreted by the source generator at compile time.
    // The generated code emits telemetry infrastructure when this is called.
    // This stub exists for API compatibility.
    return this;
  }

  /// <summary>
  /// Configures OpenTelemetry with OTLP export and custom options.
  /// </summary>
  /// <param name="configure">Action to configure telemetry options.</param>
  /// <remarks>
  /// This method is interpreted by the source generator at compile time.
  /// When called, the generator emits:
  /// <list type="bullet">
  /// <item><description>ActivitySource and Meter infrastructure</description></item>
  /// <item><description>TracerProvider and MeterProvider setup with OTLP export</description></item>
  /// <item><description>Command instrumentation (spans, metrics)</description></item>
  /// <item><description>Automatic telemetry flush on RunAsync completion</description></item>
  /// </list>
  /// When not called, none of this code is generated, making OpenTelemetry packages
  /// fully trimmable by the AOT linker.
  /// </remarks>
  public virtual NuruAppBuilder UseTelemetry(Action<NuruTelemetryOptions> configure)
  {
    // This method is interpreted by the source generator at compile time.
    // The generated code emits telemetry infrastructure when this is called.
    // This stub exists for API compatibility.
    return this;
  }

  /// <summary>
  /// Enables runtime Microsoft.Extensions.DependencyInjection instead of source-generated DI.
  /// Use this when you need full DI container support including:
  /// <list type="bullet">
  /// <item><description>Factory delegate registrations</description></item>
  /// <item><description>Services with constructor dependencies</description></item>
  /// <item><description>Extension method registrations (AddDbContext, AddSerilog, etc.)</description></item>
  /// <item><description>Internal type implementations from external assemblies</description></item>
  /// </list>
  /// </summary>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// <para>
  /// By default, Nuru uses source-generated DI which provides AOT compatibility and fast startup.
  /// However, source-gen DI has limitations - it can only instantiate services with visible
  /// parameterless constructors or explicitly registered dependencies.
  /// </para>
  /// <para>
  /// When you call this method, the generator emits code that uses a real ServiceProvider
  /// at runtime, enabling full MS DI semantics at the cost of:
  /// <list type="bullet">
  /// <item><description>Slightly slower startup (~2-10ms for ServiceProvider.Build())</description></item>
  /// <item><description>Larger binary size</description></item>
  /// <item><description>Reduced AOT trimming effectiveness</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// NuruApp app = NuruApp.CreateBuilder()
  ///   .UseMicrosoftDependencyInjection()  // Enable full DI
  ///   .ConfigureServices(services =>
  ///   {
  ///     services.AddDbContext&lt;MyDbContext&gt;();  // Now works!
  ///     services.AddSingleton&lt;IService&gt;(sp => new Service(sp.GetRequiredService&lt;IDep&gt;()));
  ///   })
  ///   .Build();
  /// </code>
  /// </example>
  public virtual NuruAppBuilder UseMicrosoftDependencyInjection()
  {
    // This method is interpreted by the source generator at compile time.
    // When called, the generator emits runtime DI code instead of static instantiation.
    // This stub exists for API compatibility.
    return this;
  }
}
