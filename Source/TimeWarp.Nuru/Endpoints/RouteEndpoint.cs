namespace TimeWarp.Nuru.Endpoints;

/// <summary>
/// Represents an endpoint that can be matched by a route pattern.
/// Similar to ASP.NET Core's RouteEndpoint but adapted for CLI scenarios.
/// </summary>
public class RouteEndpoint
{
  /// <summary>
  /// Gets or sets the route pattern string (e.g., "git commit --amend").
  /// </summary>
  public required string RoutePattern { get; set; }
  /// <summary>
  /// Gets or sets the parsed representation of the route pattern for efficient matching.
  /// </summary>
  public required ParsedRoute ParsedRoute { get; set; }
  /// <summary>
  /// Gets or sets the handler delegate that will be invoked when this endpoint is matched.
  /// </summary>
  public required Delegate Handler { get; set; }
  /// <summary>
  /// Gets or sets the method info for the handler (used for parameter binding).
  /// </summary>
  public required MethodInfo Method { get; set; }
  /// <summary>
  /// Gets or sets the order/specificity of this route (higher values match first).
  /// </summary>
  public int Order { get; set; }
  /// <summary>
  /// Gets or sets a user-friendly description of this endpoint.
  /// </summary>
  public string? Description { get; set; }
  /// <summary>
  /// Gets or sets the command type if this is a command-based route (using Mediator pattern).
  /// If null, this is a delegate-based route.
  /// </summary>
  public Type? CommandType { get; set; }
}
