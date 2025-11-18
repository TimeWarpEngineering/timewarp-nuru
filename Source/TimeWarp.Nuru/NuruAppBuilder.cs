namespace TimeWarp.Nuru;

/// <summary>
/// Unified builder for configuring Nuru applications with or without dependency injection.
/// </summary>
public class NuruAppBuilder
{
  private readonly TypeConverterRegistry TypeConverterRegistry = new();
  private ServiceCollection? ServiceCollection;
  private bool AutoHelpEnabled;
  private ILoggerFactory? LoggerFactory;
  private IConfiguration? Configuration;
  private ReplOptions? ReplOptions;

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
  /// Adds dependency injection support to the application.
  /// This also enables Mediator support for command-based routing.
  /// </summary>
  /// <param name="configureMediatorOptions">Optional action to configure Mediator options.</param>
  public NuruAppBuilder AddDependencyInjection(Action<MediatorServiceConfiguration>? configureMediatorOptions = null)
  {
    if (ServiceCollection is null)
    {
      ServiceCollection = [];
      ServiceCollection.AddNuru();
      ServiceCollection.AddSingleton(EndpointCollection);
      ServiceCollection.AddSingleton<ITypeConverterRegistry>(TypeConverterRegistry);

      // Add Mediator support
      if (configureMediatorOptions is not null)
      {
        ServiceCollection.AddMediator(configureMediatorOptions);
      }
      else
      {
        // Add core mediator services without assembly scanning
        var defaultConfig = new MediatorServiceConfiguration();
        TimeWarp.Mediator.Registration.ServiceRegistrar.AddRequiredServices(ServiceCollection, defaultConfig);
      }
    }

    return this;
  }

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
  /// Configures services using the provided action, enabling fluent service registration
  /// while maintaining the builder chain.
  /// </summary>
  /// <param name="configure">The action to configure services.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// var app = new NuruAppBuilder()
  ///   .AddDependencyInjection()
  ///   .ConfigureServices(services =>
  ///   {
  ///     services.AddSingleton&lt;ICalculator, Calculator&gt;();
  ///     services.AddLogging(config => config.AddConsole());
  ///     services.Configure&lt;AppOptions&gt;(Configuration.GetSection("App"));
  ///   })
  ///   .AddRoute&lt;Command&gt;("route")
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
  /// var app = new NuruAppBuilder()
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
  ///   .AddRoute&lt;Command&gt;("route")
  ///   .Build();
  /// </code>
  /// </example>
  public NuruAppBuilder ConfigureServices(Action<IServiceCollection, IConfiguration?> configure)
  {
    configure?.Invoke(Services, Configuration);
    return this;
  }

  /// <summary>
  /// Adds a default route that executes when no arguments are provided.
  /// </summary>
  public NuruAppBuilder AddDefaultRoute(Delegate handler, string? description = null)
  {
    return AddRouteInternal(string.Empty, handler, description);
  }

  /// <summary>
  /// Adds a delegate-based route.
  /// </summary>
  public NuruAppBuilder AddRoute(string pattern, Delegate handler, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(pattern);
    return AddRouteInternal(pattern, handler, description);
  }

  private NuruAppBuilder AddRouteInternal(string pattern, Delegate handler, string? description)
  {
    ArgumentNullException.ThrowIfNull(handler);

    // Log route registration if logger is available
    if (LoggerFactory is not null)
    {
      ILogger<NuruAppBuilder> logger = LoggerFactory.CreateLogger<NuruAppBuilder>();
      if (EndpointCollection.Count == 0)
      {
        LoggerMessages.StartingRouteRegistration(logger, null);
      }

      LoggerMessages.RegisteringRoute(logger, pattern, null);
    }

    var endpoint = new Endpoint
    {
      RoutePattern = pattern,
      CompiledRoute = PatternParser.Parse(pattern, LoggerFactory),
      Handler = handler,
      Method = handler.Method,
      Description = description
    };

    EndpointCollection.Add(endpoint);
    return this;
  }

  /// <summary>
  /// Adds a Mediator command-based route.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  public NuruAppBuilder AddRoute<TCommand>(string pattern, string? description = null)
    where TCommand : IRequest, new()
  {
    return AddMediatorRoute(typeof(TCommand), pattern, description);
  }

  /// <summary>
  /// Adds a Mediator command-based route with response.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  public NuruAppBuilder AddRoute<TCommand, TResponse>(string pattern, string? description = null)
    where TCommand : IRequest<TResponse>, new()
  {
    return AddMediatorRoute(typeof(TCommand), pattern, description);
  }

  private NuruAppBuilder AddMediatorRoute(Type commandType, string pattern, string? description)
  {
    if (ServiceCollection is null)
    {
      throw new InvalidOperationException("Dependency injection must be added before using Mediator commands. Call AddDependencyInjection() first.");
    }

    var endpoint = new Endpoint
    {
      RoutePattern = pattern,
      CompiledRoute = PatternParser.Parse(pattern, LoggerFactory),
      Description = description,
      CommandType = commandType
    };

    EndpointCollection.Add(endpoint);
    return this;
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
  /// Registers a custom type converter for parameter conversion.
  /// </summary>
  /// <param name="converter">The type converter to register.</param>
  public NuruAppBuilder AddTypeConverter(IRouteTypeConverter converter)
  {
    ArgumentNullException.ThrowIfNull(converter);
    TypeConverterRegistry.RegisterConverter(converter);
    return this;
  }

  /// <summary>
  /// Adds REPL (Read-Eval-Print Loop) support to application.
  /// This stores REPL configuration options for use when REPL mode is activated.
  /// </summary>
  /// <param name="configureOptions">Optional action to configure REPL options.</param>
  /// <returns>The builder for chaining.</returns>
  public NuruAppBuilder AddReplSupport(Action<ReplOptions>? configureOptions = null)
  {
    var replOptions = new ReplOptions();
    configureOptions?.Invoke(replOptions);

    ReplOptions = replOptions;

    return this;
  }

  /// <summary>
  /// Builds and returns a runnable NuruApp.
  /// </summary>
  public NuruApp Build()
  {
    if (AutoHelpEnabled)
    {
      GenerateHelpRoutes();
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

      // Register REPL options if configured
      if (ReplOptions is not null)
      {
        ServiceCollection.AddSingleton(ReplOptions);
      }

      ServiceProvider serviceProvider = ServiceCollection.BuildServiceProvider();

      return new NuruApp(serviceProvider);
    }
    else
    {
      // Direct path - return lightweight app without DI (pass logger factory for future use)
      return new NuruApp(EndpointCollection, TypeConverterRegistry, loggerFactory, ReplOptions);
    }
  }

  private void GenerateHelpRoutes()
  {
    // Get a snapshot of existing endpoints (before we add help routes)
    List<Endpoint> existingEndpoints = [.. EndpointCollection.Endpoints];

    // Group endpoints by their command prefix
    Dictionary<string, List<Endpoint>> commandGroups = [];

    foreach (Endpoint endpoint in existingEndpoints)
    {
      string commandPrefix = GetCommandPrefix(endpoint);

      if (!commandGroups.TryGetValue(commandPrefix, out List<Endpoint>? group))
      {
        group = [];
        commandGroups[commandPrefix] = group;
      }

      group.Add(endpoint);
    }

    // Add help routes for each command group
    foreach ((string prefix, List<Endpoint> endpoints) in commandGroups)
    {
      if (string.IsNullOrEmpty(prefix))
      {
        // Skip empty prefix - will be handled by base --help
        continue;
      }

      string helpRoute = $"{prefix} --help";
      string description = $"Show help for {prefix} command";

      // Only add if not already present
      if (!existingEndpoints.Any(e => e.RoutePattern == helpRoute))
      {
        // Capture endpoints by value to avoid issues with collection modification
        List<Endpoint> capturedEndpoints = [.. endpoints];
        AddRoute(helpRoute, () => ShowCommandGroupHelp(prefix, capturedEndpoints), description);
      }
    }

    // Add base --help route if not already present
    if (!existingEndpoints.Any(e => e.RoutePattern == "--help"))
    {
      AddRoute("--help", () =>
      {
        NuruConsole.WriteLine(HelpProvider.GetHelpText(EndpointCollection));
      },
      description: "Show available commands");
    }
  }

  private static string GetCommandPrefix(Endpoint endpoint)
  {
    List<string> parts = [];

    foreach (RouteMatcher segment in endpoint.CompiledRoute.PositionalMatchers)
    {
      if (segment is LiteralMatcher literal)
      {
        parts.Add(literal.Value);
      }
      else
      {
        // Stop at first parameter
        break;
      }
    }

    return string.Join(" ", parts);
  }

  private static void ShowCommandGroupHelp(string commandPrefix, List<Endpoint> endpoints)
  {
    NuruConsole.WriteLine($"Usage patterns for '{commandPrefix}':");
    NuruConsole.WriteLine(string.Empty);

    foreach (Endpoint endpoint in endpoints)
    {
      NuruConsole.WriteLine($"  {endpoint.RoutePattern}");
      if (!string.IsNullOrEmpty(endpoint.Description))
      {
        NuruConsole.WriteLine($"    {endpoint.Description}");
      }
    }

    // Show consolidated argument and option information
    HashSet<string> shownParams = [];

    NuruConsole.WriteLine("\nArguments:");
    foreach (Endpoint endpoint in endpoints)
    {
      foreach (RouteMatcher segment in endpoint.CompiledRoute.PositionalMatchers)
      {
        if (segment is ParameterMatcher param && shownParams.Add(param.Name))
        {
          bool isOptional = endpoint.RoutePattern.Contains($"{{{param.Name}?", StringComparison.Ordinal) ||
                           (endpoint.RoutePattern.Contains($"{{{param.Name}:", StringComparison.Ordinal) &&
                            endpoint.RoutePattern.Contains("?}", StringComparison.Ordinal));
          string status = isOptional ? "(Optional)" : "(Required)";
          string typeInfo = $"Type: {param.Constraint ?? "string"}";
          if (param.Description is not null)
          {
            NuruConsole.WriteLine($"  {param.Name,-20} {status,-12} {typeInfo,-15} {param.Description}");
          }
          else
          {
            NuruConsole.WriteLine($"  {param.Name,-20} {status,-12} {typeInfo}");
          }
        }
      }
    }

    HashSet<string> shownOptions = [];

    if (endpoints.Any(e => e.CompiledRoute.OptionMatchers.Count > 0))
    {
      NuruConsole.WriteLine("\nOptions:");
      foreach (Endpoint endpoint in endpoints)
      {
        foreach (OptionMatcher option in endpoint.CompiledRoute.OptionMatchers)
        {
          if (shownOptions.Add(option.MatchPattern))
          {
            string optionName = option.MatchPattern.StartsWith("--", StringComparison.Ordinal) ? option.MatchPattern : $"--{option.MatchPattern}";
            if (option.AlternateForm is not null)
            {
              optionName = $"{optionName},{option.AlternateForm}";
            }

            string paramInfo = option.ExpectsValue && option.ParameterName is not null ? $" <{option.ParameterName}>" : "";

            if (option.Description is not null)
            {
              NuruConsole.WriteLine($"  {optionName + paramInfo,-30} {option.Description}");
            }
            else
            {
              NuruConsole.WriteLine($"  {optionName}{paramInfo}");
            }
          }
        }
      }
    }
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
