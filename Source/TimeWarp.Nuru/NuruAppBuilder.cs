namespace TimeWarp.Nuru;

/// <summary>
/// Unified builder for configuring Nuru applications with or without dependency injection.
/// </summary>
public class NuruAppBuilder
{
  private readonly TypeConverterRegistry TypeConverterRegistry = new();
  private ServiceCollection? ServiceCollection;
  private bool AutoHelpEnabled;

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
  /// Adds a delegate-based route.
  /// </summary>
  public NuruAppBuilder AddRoute(string pattern, Delegate handler, string? description = null)
  {
    ArgumentNullException.ThrowIfNull(pattern);
    ArgumentNullException.ThrowIfNull(handler);

    var endpoint = new RouteEndpoint
    {
      RoutePattern = pattern,
      ParsedRoute = RoutePatternParser.Parse(pattern),
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
      ParsedRoute = RoutePatternParser.Parse(pattern),
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

    if (ServiceCollection is not null)
    {
      // DI path - build service provider and return DI-enabled app
      ServiceProvider serviceProvider = ServiceCollection.BuildServiceProvider();
      return new NuruApp(serviceProvider);
    }
    else
    {
      // Direct path - return lightweight app without DI
      return new NuruApp(EndpointCollection, TypeConverterRegistry);
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
        Console.WriteLine(RouteHelpProvider.GetHelpText(EndpointCollection));
      },
      description: "Show available commands");
    }
  }

  private static string GetCommandPrefix(RouteEndpoint endpoint)
  {
    List<string> parts = [];

    foreach (RouteSegment segment in endpoint.ParsedRoute.PositionalTemplate)
    {
      if (segment is LiteralSegment literal)
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
    Console.WriteLine($"Usage patterns for '{commandPrefix}':");
    Console.WriteLine();

    foreach (RouteEndpoint endpoint in endpoints)
    {
      Console.WriteLine($"  {endpoint.RoutePattern}");
      if (!string.IsNullOrEmpty(endpoint.Description))
      {
        Console.WriteLine($"    {endpoint.Description}");
      }
    }

    // Show consolidated argument and option information
    HashSet<string> shownParams = [];

    Console.WriteLine("\nArguments:");
    foreach (RouteEndpoint endpoint in endpoints)
    {
      foreach (RouteSegment segment in endpoint.ParsedRoute.PositionalTemplate)
      {
        if (segment is ParameterSegment param && shownParams.Add(param.Name))
        {
          bool isOptional = endpoint.RoutePattern.Contains($"{{{param.Name}?", StringComparison.Ordinal) ||
                           (endpoint.RoutePattern.Contains($"{{{param.Name}:", StringComparison.Ordinal) &&
                            endpoint.RoutePattern.Contains("?}", StringComparison.Ordinal));
          string status = isOptional ? "(Optional)" : "(Required)";
          string typeInfo = $"Type: {param.Constraint ?? "string"}";
          if (param.Description is not null)
          {
            Console.WriteLine($"  {param.Name,-20} {status,-12} {typeInfo,-15} {param.Description}");
          }
          else
          {
            Console.WriteLine($"  {param.Name,-20} {status,-12} {typeInfo}");
          }
        }
      }
    }

    HashSet<string> shownOptions = [];

    if (endpoints.Any(e => e.ParsedRoute.OptionSegments.Count > 0))
    {
      Console.WriteLine("\nOptions:");
      foreach (RouteEndpoint endpoint in endpoints)
      {
        foreach (OptionSegment option in endpoint.ParsedRoute.OptionSegments)
        {
          if (shownOptions.Add(option.Name))
          {
            string optionName = option.Name.StartsWith("--", StringComparison.Ordinal) ? option.Name : $"--{option.Name}";
            if (option.ShortAlias is not null)
            {
              optionName = $"{optionName},{option.ShortAlias}";
            }

            string paramInfo = option.ExpectsValue && option.ValueParameterName is not null ? $" <{option.ValueParameterName}>" : "";

            if (option.Description is not null)
            {
              Console.WriteLine($"  {optionName + paramInfo,-30} {option.Description}");
            }
            else
            {
              Console.WriteLine($"  {optionName}{paramInfo}");
            }
          }
        }
      }
    }
  }
}
