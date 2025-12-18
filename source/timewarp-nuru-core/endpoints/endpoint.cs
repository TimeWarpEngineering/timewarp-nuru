namespace TimeWarp.Nuru;

/// <summary>
/// Represents an endpoint that can be matched by a route pattern.
/// Similar to ASP.NET Core's RouteEndpoint but adapted for CLI scenarios.
/// </summary>
public class Endpoint
{
  /// <summary>
  /// Gets or sets the route pattern string (e.g., "git commit --amend").
  /// </summary>
  public required string RoutePattern { get; set; }
  /// <summary>
  /// Gets or sets the compiled representation of the route pattern for efficient matching.
  /// </summary>
  public required CompiledRoute CompiledRoute { get; set; }
  /// <summary>
  /// Gets or sets the handler delegate that will be invoked when this endpoint is matched.
  /// For Mediator commands, this will be null and CommandType will be used instead.
  /// </summary>
  public Delegate? Handler { get; set; }
  /// <summary>
  /// Gets or sets the method info for the handler (used for parameter binding).
  /// For Mediator commands, this will be null.
  /// </summary>
  public MethodInfo? Method { get; set; }
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
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
  public Type? CommandType { get; set; }
  /// <summary>
  /// Gets or sets the message type for this endpoint, indicating whether it's a query, command, or idempotent command.
  /// Defaults to <see cref="MessageType.Command"/> (the safest assumption for AI agents).
  /// </summary>
  public MessageType MessageType { get; set; } = MessageType.Command;

  /// <summary>
  /// Gets the execution strategy for this endpoint based on its configuration.
  /// </summary>
  public ExecutionStrategy Strategy =>
    CommandType is not null ? ExecutionStrategy.Mediator :
    Handler is not null ? ExecutionStrategy.Delegate :
    ExecutionStrategy.Invalid;

  /// <summary>
  /// Gets the help route pattern for this endpoint (e.g., "hello --help").
  /// </summary>
  public string GetHelpRoute()
  {
    // Extract positional literals from the parsed route
    List<string> positionalParts = [];

    foreach (RouteMatcher segment in CompiledRoute.PositionalMatchers)
    {
      if (segment is LiteralMatcher literal)
      {
        positionalParts.Add(literal.Value);
      }
      else
      {
        // Stop at first parameter - we only want the command prefix
        break;
      }
    }

    // If no positional parts, just return --help
    if (positionalParts.Count == 0)
    {
      return "--help";
    }

    // Otherwise, append --help to the command prefix
    return $"{string.Join(" ", positionalParts)} --help";
  }
}
