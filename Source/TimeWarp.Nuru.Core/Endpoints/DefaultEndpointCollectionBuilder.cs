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

  public void Map(string routePattern, Delegate handler, string? description = null)
  {
    if (string.IsNullOrWhiteSpace(routePattern))
      throw new ArgumentException("Route pattern cannot be null or empty.", nameof(routePattern));

    ArgumentNullException.ThrowIfNull(handler);

    CompiledRoute compiledRoute = PatternParser.Parse(routePattern);
    MethodInfo method = handler.Method;

    Endpoint endpoint = new()
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
