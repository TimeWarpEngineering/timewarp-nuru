namespace TimeWarp.Nuru;

/// <summary>
/// Represents a compiled route pattern that has been broken down into its components
/// for efficient matching against command-line arguments.
/// </summary>
public class CompiledRoute
{
  /// <summary>
  /// Gets or sets the ordered segments (literals, parameters, and options) that must be matched sequentially.
  /// This preserves the positional order from the route pattern AST.
  /// </summary>
  public required IReadOnlyList<RouteMatcher> Segments { get; set; }

  /// <summary>
  /// Gets or sets the message type for this route, indicating whether it's a query, command, or idempotent command.
  /// Defaults to <see cref="MessageType.Command"/> (the safest assumption for AI agents).
  /// </summary>
  public MessageType MessageType { get; set; } = MessageType.Command;

  /// <summary>
  /// Gets or sets the name of the catch-all parameter if present (e.g., "args" for {*args}).
  /// </summary>
  public string? CatchAllParameterName { get; set; }

  /// <summary>
  /// Gets whether this route has a catch-all parameter.
  /// </summary>
  public bool HasCatchAll => CatchAllParameterName is not null;

  /// <summary>
  /// Gets or sets the specificity score used for ordering route matches.
  /// Higher values indicate more specific routes that should be tried first.
  /// </summary>
  public int Specificity { get; set; }

  // Private backing fields for cached matchers

  /// <summary>
  /// Gets the positional matchers (literals and parameters) for backward compatibility.
  /// This filters out OptionMatchers from the Segments list.
  /// Results are cached after first access.
  /// </summary>
  public IReadOnlyList<RouteMatcher> PositionalMatchers =>
    field ??= Segments.Where(s => s is not OptionMatcher).ToArray();

  /// <summary>
  /// Gets the option matchers for backward compatibility.
  /// This filters OptionMatchers from the Segments list.
  /// Results are cached after first access.
  /// </summary>
  public IReadOnlyList<OptionMatcher> OptionMatchers =>
    field ??= Segments.OfType<OptionMatcher>().ToArray();

  /// <summary>
  /// Gets the repeated option matchers from this route.
  /// Results are cached after first access.
  /// </summary>
  public IReadOnlyList<OptionMatcher> RepeatedOptions =>
    field ??= OptionMatchers.Where(o => o.IsRepeated).ToArray();
}
