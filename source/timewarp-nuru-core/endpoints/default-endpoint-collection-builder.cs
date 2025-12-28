namespace TimeWarp.Nuru;

/// <summary>
/// Default implementation of IEndpointCollectionBuilder.
/// This is a compile-time DSL shell - the source generator handles actual work.
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
    // Source generator handles route registration at compile time
    _ = routePattern;
    _ = handler;
    _ = description;
  }
}
