
namespace TimeWarp.Nuru.Endpoints;

/// <summary>
/// A collection of route endpoints ordered by specificity for efficient matching.
/// </summary>
public class EndpointCollection : IEnumerable<RouteEndpoint>
{
  private readonly List<RouteEndpoint> EndpointsList = [];
  private readonly object LockObject = new();

  /// <summary>
  /// Gets all endpoints in the collection, ordered by specificity (most specific first).
  /// </summary>
  public IReadOnlyList<RouteEndpoint> Endpoints
  {
    get
    {
      lock (LockObject)
      {
        return EndpointsList.ToList();
      }
    }
  }

  /// <summary>
  /// Adds a new endpoint to the collection and re-sorts by specificity.
  /// </summary>
  /// <param name="endpoint">The endpoint to add.</param>
  public void Add(RouteEndpoint endpoint)
  {
    ArgumentNullException.ThrowIfNull(endpoint);

    lock (LockObject)
    {
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
  }

  /// <summary>
  /// Removes all endpoints from the collection.
  /// </summary>
  public void Clear()
  {
    lock (LockObject)
    {
      EndpointsList.Clear();
    }
  }

  /// <summary>
  /// Gets the count of endpoints in the collection.
  /// </summary>
  public int Count
  {
    get
    {
      lock (LockObject)
      {
        return EndpointsList.Count;
      }
    }
  }

  public IEnumerator<RouteEndpoint> GetEnumerator()
  {
    lock (LockObject)
    {
      return EndpointsList.ToList().GetEnumerator();
    }
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}
