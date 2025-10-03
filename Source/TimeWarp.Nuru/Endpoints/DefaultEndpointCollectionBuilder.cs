namespace TimeWarp.Nuru;

/// <summary>
/// Default implementation of IEndpointCollectionBuilder.
/// </summary>
public class DefaultEndpointCollectionBuilder : IEndpointCollectionBuilder
{
  public EndpointCollection EndpointCollection { get; }

  public DefaultEndpointCollectionBuilder(EndpointCollection endpointCollection)
  {
    EndpointCollection = endpointCollection ?? throw new ArgumentNullException(nameof(endpointCollection));
  }

  public void AddRoute(string routePattern, Delegate handler, string? description = null)
  {
    if (string.IsNullOrWhiteSpace(routePattern))
      throw new ArgumentException("Route pattern cannot be null or empty.", nameof(routePattern));

    ArgumentNullException.ThrowIfNull(handler);

    CompiledRoute compiledRoute = RoutePatternParser.Parse(routePattern);
    MethodInfo method = handler.Method;

    var endpoint = new Endpoint
    {
      RoutePattern = routePattern,
      CompiledRoute = compiledRoute,
      Handler = handler,
      Method = method,
      Order = compiledRoute.Specificity,
      Description = description
    };

    EndpointCollection.Add(endpoint);
  }
}
