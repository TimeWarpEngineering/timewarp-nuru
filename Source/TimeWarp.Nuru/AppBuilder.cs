
namespace TimeWarp.Nuru;

/// <summary>
/// Builder for configuring a TimeWarp.Console application.
/// </summary>
public class AppBuilder
{
  private readonly ServiceCollection ServiceCollection = [];
  private readonly EndpointCollection EndpointCollection = [];

  public IServiceCollection Services => ServiceCollection;

  public AppBuilder()
  {
    // Add default services
    ServiceCollection.AddNuru();
    ServiceCollection.AddSingleton(EndpointCollection);
  }

  /// <summary>
  /// Adds a delegate-based route.
  /// </summary>
  public AppBuilder AddRoute(string pattern, Delegate handler, string? description = null)
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
  /// </summary>
  public AppBuilder AddRoute<TCommand>(string pattern, string? description = null)
      where TCommand : IRequest, new()
  {
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
  /// </summary>
  public AppBuilder AddRoute<TCommand, TResponse>(string pattern, string? description = null)
      where TCommand : IRequest<TResponse>, new()
  {
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
  /// Builds the service provider and returns a runnable app.
  /// </summary>
  public NuruApp Build()
  {
    EndpointCollection.Sort();
    ServiceProvider serviceProvider = ServiceCollection.BuildServiceProvider();
    return new NuruApp(serviceProvider);
  }
}
