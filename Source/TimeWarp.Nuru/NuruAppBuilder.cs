namespace TimeWarp.Nuru;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Nuru.Parsing;

/// <summary>
/// Unified builder for configuring Nuru applications with or without dependency injection.
/// </summary>
public class NuruAppBuilder
{
  private readonly TypeConverterRegistry TypeConverterRegistry = new();
  private ServiceCollection? ServiceCollection;
  private bool AutoHelpEnabled;
  private ILoggerFactory? LoggerFactory;

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

    var endpoint = new RouteEndpoint
    {
      RoutePattern = pattern,
      CompiledRoute = RoutePatternParser.Parse(pattern, LoggerFactory),
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

    var endpoint = new RouteEndpoint
    {
      RoutePattern = pattern,
      CompiledRoute = RoutePatternParser.Parse(pattern, LoggerFactory),
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
      ServiceProvider serviceProvider = ServiceCollection.BuildServiceProvider();
      return new NuruApp(serviceProvider);
    }
    else
    {
      // Direct path - return lightweight app without DI (pass logger factory for future use)
      return new NuruApp(EndpointCollection, TypeConverterRegistry, loggerFactory);
    }
  }

  private void GenerateHelpRoutes()
  {
    // Get a snapshot of existing endpoints (before we add help routes)
    List<RouteEndpoint> existingEndpoints = [.. EndpointCollection.Endpoints];

    // Group endpoints by their command prefix
    Dictionary<string, List<RouteEndpoint>> commandGroups = [];

    foreach (RouteEndpoint endpoint in existingEndpoints)
    {
      string commandPrefix = GetCommandPrefix(endpoint);

      if (!commandGroups.TryGetValue(commandPrefix, out List<RouteEndpoint>? group))
      {
        group = [];
        commandGroups[commandPrefix] = group;
      }

      group.Add(endpoint);
    }

    // Add help routes for each command group
    foreach ((string prefix, List<RouteEndpoint> endpoints) in commandGroups)
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
        List<RouteEndpoint> capturedEndpoints = [.. endpoints];
        AddRoute(helpRoute, () => ShowCommandGroupHelp(prefix, capturedEndpoints), description);
      }
    }

    // Add base --help route if not already present
    if (!existingEndpoints.Any(e => e.RoutePattern == "--help"))
    {
      AddRoute("--help", () =>
      {
        NuruConsole.WriteLine(RouteHelpProvider.GetHelpText(EndpointCollection));
      },
      description: "Show available commands");
    }
  }

  private static string GetCommandPrefix(RouteEndpoint endpoint)
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

  private static void ShowCommandGroupHelp(string commandPrefix, List<RouteEndpoint> endpoints)
  {
    NuruConsole.WriteLine($"Usage patterns for '{commandPrefix}':");
    NuruConsole.WriteLine(string.Empty);

    foreach (RouteEndpoint endpoint in endpoints)
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
    foreach (RouteEndpoint endpoint in endpoints)
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
      foreach (RouteEndpoint endpoint in endpoints)
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
}
