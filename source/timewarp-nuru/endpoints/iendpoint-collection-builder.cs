namespace TimeWarp.Nuru;

/// <summary>
/// Defines the interface for building a collection of route endpoints.
/// </summary>
public interface IEndpointCollectionBuilder
{
  /// <summary>
  /// Adds a route with the specified pattern and handler to the endpoint collection.
  /// </summary>
  /// <param name="routePattern">The route pattern (e.g., "git commit --amend").</param>
  /// <param name="handler">The delegate to invoke when the route is matched.</param>
  /// <param name="description">Optional description of what this route does.</param>
  void Map(string routePattern, Delegate handler, string? description = null);

  /// <summary>
  /// Gets the endpoint collection being built.
  /// </summary>
  EndpointCollection EndpointCollection { get; }
}
