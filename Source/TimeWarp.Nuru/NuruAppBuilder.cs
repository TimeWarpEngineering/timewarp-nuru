
namespace TimeWarp.Nuru;

/// <summary>
/// Unified builder for configuring Nuru applications with or without dependency injection.
/// </summary>
public class NuruAppBuilder
{
  private readonly EndpointCollection EndpointCollection = [];
  private readonly TypeConverterRegistry TypeConverterRegistry = new();
  private ServiceCollection? ServiceCollection;

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
  /// Adds a Mediator command-based route with response.
  /// Requires AddDependencyInjection() to be called first.
  /// </summary>
  public NuruAppBuilder AddRoute<TCommand, TResponse>(string pattern, string? description = null)
      where TCommand : IRequest<TResponse>, new()
  {
    return AddMediatorRoute(typeof(TCommand), pattern, description);
  }

  /// <summary>
  /// Builds and returns a runnable NuruApp.
  /// </summary>
  public NuruApp Build()
  {
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
}
