namespace TimeWarp.Nuru.Endpoints;

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
  void AddRoute(string routePattern, Delegate handler);

  /// <summary>
  /// Adds a route with the specified pattern, handler, and metadata to the endpoint collection.
  /// </summary>
  /// <param name="routePattern">The route pattern (e.g., "git commit --amend").</param>
  /// <param name="handler">The delegate to invoke when the route is matched.</param>
  /// <param name="metadata">Additional metadata to associate with the endpoint.</param>
  void AddRoute(string routePattern, Delegate handler, params object[] metadata);

  /// <summary>
  /// Gets the endpoint collection being built.
  /// </summary>
  EndpointCollection EndpointCollection { get; }
}
