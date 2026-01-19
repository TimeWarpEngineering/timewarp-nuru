namespace TimeWarp.Nuru;

/// <summary>
/// Interface for matching parsed input against registered routes.
/// </summary>
public interface IRouteMatchEngine
{
  /// <summary>
  /// Matches parsed input against all registered endpoints.
  /// </summary>
  /// <param name="input">The tokenized input.</param>
  /// <param name="endpoints">All registered endpoints.</param>
  /// <returns>Match states for all routes (viable and non-viable).</returns>
  IReadOnlyList<RouteMatchState> Match(ParsedInput input, EndpointCollection endpoints);
}
