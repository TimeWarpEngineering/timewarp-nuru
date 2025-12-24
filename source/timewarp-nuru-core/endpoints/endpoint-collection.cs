
namespace TimeWarp.Nuru;

/// <summary>
/// A collection of route endpoints ordered by specificity for efficient matching.
/// Thread-safety is not needed as routes are configured once at startup in CLI apps.
/// </summary>
public class EndpointCollection : IEnumerable<Endpoint>
{
  private readonly List<Endpoint> EndpointsList = [];

  /// <summary>
  /// Gets all endpoints in the collection, ordered by specificity (most specific first).
  /// </summary>
  public IReadOnlyList<Endpoint> Endpoints => EndpointsList;

  /// <summary>
  /// Adds a new endpoint to the collection and re-sorts by specificity.
  /// </summary>
  /// <param name="endpoint">The endpoint to add.</param>
#pragma warning disable CA1822 // V2 path doesn't access instance data but V1 does
  public void Add(Endpoint endpoint)
#pragma warning restore CA1822
  {
#if !USE_NEW_GEN
    ArgumentNullException.ThrowIfNull(endpoint);

    // Check for duplicate routes
    Endpoint? existingRoute = EndpointsList.FirstOrDefault(e =>
              e.RoutePattern.Equals(endpoint.RoutePattern, StringComparison.OrdinalIgnoreCase));

    if (existingRoute is not null)
    {
      // Warn about duplicate route
      NuruConsole.Default.WriteErrorLine($"Warning: Duplicate route pattern '{endpoint.RoutePattern}' detected. The new handler will override the previous one.");
      EndpointsList.Remove(existingRoute);
    }

    EndpointsList.Add(endpoint);
#else
    // V2: Fluent API is syntax for generator only, don't add at runtime
    _ = endpoint; // Suppress unused parameter warning
#endif
  }

  /// <summary>
  /// Sorts the endpoints by order and specificity. Called once during build.
  /// Uses a stable sort to preserve insertion order for routes with equal Order and Specificity.
  /// </summary>
  internal void Sort()
  {
    // Sort by Order descending (higher order = higher priority)
    // Then by Specificity descending for routes with same order
    // Use LINQ for stable sort - preserves insertion order as tie-breaker
    List<Endpoint> sorted =
    [
      .. EndpointsList
        .Select((ep, index) => (ep, index))
        .OrderByDescending(x => x.ep.Order)
        .ThenByDescending(x => x.ep.CompiledRoute.Specificity)
        .ThenBy(x => x.index)
        .Select(x => x.ep)
    ];

    EndpointsList.Clear();
    EndpointsList.AddRange(sorted);
  }

  /// <summary>
  /// Gets the count of endpoints in the collection.
  /// </summary>
  public int Count => EndpointsList.Count;

  public IEnumerator<Endpoint> GetEnumerator()
  {
    // No need to copy - the list won't be modified during enumeration in a CLI app
    return EndpointsList.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}
