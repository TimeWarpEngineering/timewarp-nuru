namespace TimeWarp.Nuru;

/// <summary>
/// Configuration, dependency injection, and service methods for NuruAppBuilder.
/// </summary>
public partial class NuruAppBuilder
{
  /// <summary>
  /// Adds standard .NET configuration sources to the application.
  /// This includes appsettings.json, environment-specific settings, user secrets (Development only),
  /// environment variables, and command line arguments.
  /// For file-based apps, configuration files are located relative to the source file directory.
  /// </summary>
  /// <param name="args">Optional command line arguments to include in configuration.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// Configuration sources are loaded in this order (later sources override earlier ones):
  /// 1. appsettings.json
  /// 2. appsettings.{Environment}.json
  /// 3. {ApplicationName}.settings.json
  /// 4. {ApplicationName}.settings.{Environment}.json
  /// 5. User secrets (Development environment only, requires UserSecretsId)
  /// 6. Environment variables
  /// 7. Command line arguments
  ///
  /// To use user secrets in runfiles, add:
  /// <code>
  /// #:property UserSecretsId=your-guid-here
  /// </code>
  /// Or use the assembly attribute:
  /// <code>
  /// [assembly: Microsoft.Extensions.Configuration.UserSecrets.UserSecretsId("your-guid-here")]
  /// </code>
  /// </remarks>
  public NuruAppBuilder AddConfiguration(string[]? args = null)
  {
    // Ensure DI is enabled
    if (ServiceCollection is null)
    {
      throw new InvalidOperationException("Configuration requires dependency injection. Call AddDependencyInjection() first.");
    }

    string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
      ?? "Production";

    string? sanitizedApplicationName = GetSanitizedApplicationName();
    string basePath = DetermineConfigurationBasePath();

    IConfigurationBuilder configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
      .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

    // Add application-specific settings files (matches .NET 10 convention from https://github.com/dotnet/runtime/pull/116987)
    if (!string.IsNullOrEmpty(sanitizedApplicationName))
    {
      configuration
        .AddJsonFile($"{sanitizedApplicationName}.settings.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"{sanitizedApplicationName}.settings.{environmentName}.json", optional: true, reloadOnChange: true);
    }

    // Add user secrets in Development environment (optional parameter means it won't throw if UserSecretsId is missing)
    if (environmentName == "Development")
    {
      configuration.AddUserSecrets(Assembly.GetEntryAssembly()!, optional: true, reloadOnChange: true);
    }

    configuration.AddEnvironmentVariables();

    if (args?.Length > 0)
    {
      configuration.AddCommandLine(args);
    }

    IConfigurationRoot configurationRoot = configuration.Build();
    Configuration = configurationRoot;
    ServiceCollection.AddSingleton<IConfiguration>(configurationRoot);

    return this;
  }

  /// <summary>
  /// Adds dependency injection support to the application.
  /// Uses martinothamar/Mediator source generator for full AOT compatibility.
  /// </summary>
  /// <remarks>
  /// <para>
  /// If your application uses Mediator-based command handlers (IRequest/IRequestHandler),
  /// you must call AddMediator() in ConfigureServices:
  /// </para>
  /// <code>
  /// builder.ConfigureServices(services => services.AddMediator());
  /// </code>
  /// <para>
  /// The source generator creates the AddMediator() method based on handler types found
  /// in your project at compile time. Configuration is done via [assembly: MediatorOptions] attribute.
  /// </para>
  /// </remarks>
  public NuruAppBuilder AddDependencyInjection()
  {
    if (ServiceCollection is null)
    {
      ServiceCollection = [];
      ServiceCollection.AddNuru();
      ServiceCollection.AddSingleton(EndpointCollection);
      ServiceCollection.AddSingleton<ITypeConverterRegistry>(TypeConverterRegistry);

      // Note: AddMediator() must be called by the consuming application, not the library.
      // The source generator discovers handlers in the assembly where AddMediator() is called.
      // Applications using Mediator-based handlers should call services.AddMediator() in ConfigureServices.
    }

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
  /// NuruCoreApp app = new NuruAppBuilder()
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
  public NuruAppBuilder ConfigureServices(Action<IServiceCollection> configure)
  {
    configure?.Invoke(Services);
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
  /// NuruCoreApp app = new NuruAppBuilder()
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
  public NuruAppBuilder ConfigureServices(Action<IServiceCollection, IConfiguration?> configure)
  {
    configure?.Invoke(Services, Configuration);
    return this;
  }

  /// <summary>
  /// Configures logging for the application using the provided ILoggerFactory.
  /// If not called, NullLoggerFactory is used (zero overhead).
  /// </summary>
  /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
  public NuruAppBuilder UseLogging(ILoggerFactory loggerFactory)
  {
    ArgumentNullException.ThrowIfNull(loggerFactory);
    LoggerFactory = loggerFactory;
    return this;
  }

  /// <summary>
  /// Sets the terminal I/O provider for interactive operations like REPL.
  /// If not called, defaults to <see cref="NuruTerminal.Default"/>.
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
  /// NuruCoreApp app = new NuruAppBuilder()
  ///     .UseTerminal(terminal)
  ///     .AddReplSupport()
  ///     .Build();
  /// </code>
  /// </example>
  public NuruAppBuilder UseTerminal(ITerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    Terminal = terminal;
    return this;
  }

  /// <summary>
  /// Determines the configuration base path using a fallback chain.
  /// </summary>
  /// <returns>The configuration base path to use.</returns>
  /// <remarks>
  /// Fallback chain:
  /// 1. Assembly directory (works for published executables with deployed config files)
  /// 2. Source file directory from AppContext.EntryPointFileDirectoryPath() (runtime data, .NET 10 file-based apps)
  /// 3. Source file directory from Path.EntryPointFileDirectoryPath() (compile-time CallerFilePath fallback)
  /// 4. Current directory (final fallback)
  ///
  /// Logs configuration source at Debug level for troubleshooting.
  /// Uses AppContext first since it's set at runtime for file-based apps, falls back to Path extension
  /// which uses CallerFilePath (useful for published apps where AppContext data isn't set).
  /// </remarks>
  private string DetermineConfigurationBasePath()
  {
    ILogger logger = (LoggerFactory ?? NullLoggerFactory.Instance).CreateLogger<NuruAppBuilder>();
    string basePath = AppContext.BaseDirectory;
    bool configInAssemblyDir = false;

    string? sanitizedName = GetSanitizedApplicationName();
    if (!string.IsNullOrEmpty(sanitizedName))
    {
      string assemblyConfigPath = Path.Combine(basePath, $"{sanitizedName}.settings.json");
      configInAssemblyDir = File.Exists(assemblyConfigPath) || File.Exists(Path.Combine(basePath, "appsettings.json"));
    }

    // If no config in assembly directory, try source directory from entry point
    if (!configInAssemblyDir)
    {
      // Try AppContext first (runtime data for file-based apps), then Path extension (compile-time fallback)
      string? sourceDir = AppContext.EntryPointFileDirectoryPath();
      string source;

      if (!string.IsNullOrEmpty(sourceDir))
      {
        source = "AppContext.EntryPointFileDirectoryPath (runtime)";
      }
      else
      {
        sourceDir = Path.EntryPointFileDirectoryPath();
        source = "Path.EntryPointFileDirectoryPath (CallerFilePath)";
      }

      if (!string.IsNullOrEmpty(sourceDir) && Directory.Exists(sourceDir))
      {
        basePath = sourceDir;
        LoggerMessages.ConfigurationBasePath(logger, basePath, source, null);
        return basePath;
      }
    }
    else
    {
      LoggerMessages.ConfigurationBasePath(logger, basePath, "Assembly directory", null);
      return basePath;
    }

    // Final fallback to current directory if assembly dir and source dir don't have configs
    if (!configInAssemblyDir && basePath == AppContext.BaseDirectory)
    {
      basePath = Directory.GetCurrentDirectory();
      LoggerMessages.ConfigurationBasePath(logger, basePath, "Current directory - fallback", null);
    }

    return basePath;
  }

  /// <summary>
  /// Gets the sanitized application name for use in file names.
  /// </summary>
  /// <returns>Sanitized application name safe for use in file names, or null if no entry assembly.</returns>
  /// <remarks>
  /// Retrieves the entry assembly name and replaces path separators with underscores.
  /// File-based apps may have application names containing path separators (e.g., "path/to/app.cs").
  /// This method ensures the name is safe for use in configuration file names.
  /// </remarks>
  private static string? GetSanitizedApplicationName()
  {
    string? applicationName = Assembly.GetEntryAssembly()?.GetName().Name;

    if (string.IsNullOrEmpty(applicationName))
      return applicationName;

    return applicationName
      .Replace(Path.DirectorySeparatorChar, '_')
      .Replace(Path.AltDirectorySeparatorChar, '_');
  }
}
