namespace TimeWarp.Nuru.Endpoints;

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

    ParsedRoute parsedRoute = RoutePatternParser.Parse(routePattern);
    MethodInfo method = handler.Method;

    var endpoint = new RouteEndpoint
    {
      RoutePattern = routePattern,
      ParsedRoute = parsedRoute,
      Handler = handler,
      Method = method,
      Order = parsedRoute.Specificity,
      Description = description
    };

    EndpointCollection.Add(endpoint);
  }
}
