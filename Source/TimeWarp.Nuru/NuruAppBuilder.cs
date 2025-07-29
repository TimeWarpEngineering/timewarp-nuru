
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
  /// Gets the service collection if dependency injection has been added.
  /// </summary>
  public IServiceCollection? Services => ServiceCollection;

  /// <summary>
  /// Adds dependency injection support to the application.
  /// </summary>
  public NuruAppBuilder AddDependencyInjection()
  {
    if (ServiceCollection == null)
    {
      ServiceCollection = new ServiceCollection();
      ServiceCollection.AddNuru();
      ServiceCollection.AddSingleton(EndpointCollection);
      ServiceCollection.AddSingleton<ITypeConverterRegistry>(TypeConverterRegistry);
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
    if (ServiceCollection == null)
    {
      throw new InvalidOperationException("Dependency injection must be added before using Mediator commands. Call AddDependencyInjection() first.");
    }

    var endpoint = new RouteEndpoint
    {
      RoutePattern = pattern,
      ParsedRoute = RoutePatternParser.Parse(pattern),
      Handler = new Action(() => { }), // Placeholder
      Method = typeof(Action).GetMethod("Invoke")!,
      Description = description,
      CommandType = typeof(TCommand)
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
    if (ServiceCollection == null)
    {
      throw new InvalidOperationException("Dependency injection must be added before using Mediator commands. Call AddDependencyInjection() first.");
    }

    var endpoint = new RouteEndpoint
    {
      RoutePattern = pattern,
      ParsedRoute = RoutePatternParser.Parse(pattern),
      Handler = new Action(() => { }), // Placeholder
      Method = typeof(Action).GetMethod("Invoke")!,
      Description = description,
      CommandType = typeof(TCommand)
    };

    EndpointCollection.Add(endpoint);
    return this;
  }

  /// <summary>
  /// Builds and returns a runnable NuruApp.
  /// </summary>
  public NuruApp Build()
  {
    EndpointCollection.Sort();
    
    if (ServiceCollection != null)
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
