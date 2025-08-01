
namespace TimeWarp.Nuru.Endpoints;

/// <summary>
/// A collection of route endpoints ordered by specificity for efficient matching.
/// Thread-safety is not needed as routes are configured once at startup in CLI apps.
/// </summary>
public class EndpointCollection : IEnumerable<RouteEndpoint>
{
  private readonly List<RouteEndpoint> EndpointsList = [];

  /// <summary>
  /// Gets all endpoints in the collection, ordered by specificity (most specific first).
  /// </summary>
  public IReadOnlyList<RouteEndpoint> Endpoints => EndpointsList;

  /// <summary>
  /// Adds a new endpoint to the collection and re-sorts by specificity.
  /// </summary>
  /// <param name="endpoint">The endpoint to add.</param>
  public void Add(RouteEndpoint endpoint)
  {
    ArgumentNullException.ThrowIfNull(endpoint);

    // Check for duplicate routes
    RouteEndpoint? existingRoute = EndpointsList.FirstOrDefault(e =>
              e.RoutePattern.Equals(endpoint.RoutePattern, StringComparison.OrdinalIgnoreCase));

    if (existingRoute is not null)
    {
      // Warn about duplicate route
      System.Console.Error.WriteLine($"Warning: Duplicate route pattern '{endpoint.RoutePattern}' detected. The new handler will override the previous one.");
      EndpointsList.Remove(existingRoute);
    }

    EndpointsList.Add(endpoint);
  }

  /// <summary>
  /// Sorts the endpoints by order and specificity. Called once during build.
  /// </summary>
  internal void Sort()
  {
    // Sort by Order descending (higher order = higher priority)
    // Then by Specificity descending for routes with same order
    EndpointsList.Sort((a, b) =>
    {
      int orderComparison = b.Order.CompareTo(a.Order);
      return orderComparison != 0
                ? orderComparison
                : b.ParsedRoute.Specificity.CompareTo(a.ParsedRoute.Specificity);
    });
  }

  /// <summary>
  /// Gets the count of endpoints in the collection.
  /// </summary>
  public int Count => EndpointsList.Count;

  public IEnumerator<RouteEndpoint> GetEnumerator()
  {
    // No need to copy - the list won't be modified during enumeration in a CLI app
    return EndpointsList.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}
