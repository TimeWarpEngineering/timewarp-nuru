namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Represents a parsed route pattern that has been broken down into its components
/// for efficient matching against command-line arguments.
/// </summary>
public class ParsedRoute
{
  /// <summary>
  /// Gets or sets the positional template - the ordered segments (literals and parameters)
  /// that must be matched before any options.
  /// </summary>
  public required IReadOnlyList<RouteSegment> PositionalTemplate { get; set; }
  /// <summary>
  /// Gets or sets the required options that must be present (e.g., ["--amend"]).
  /// </summary>
  public IReadOnlyList<string> RequiredOptions { get; set; } = Array.Empty<string>();
  /// <summary>
  /// Gets or sets the option segments that must be matched.
  /// </summary>
  public IReadOnlyList<OptionSegment> OptionSegments { get; set; } = Array.Empty<OptionSegment>();
  /// <summary>
  /// Gets or sets the name of the catch-all parameter if present (e.g., "args" for {*args}).
  /// </summary>
  public string? CatchAllParameterName { get; set; }
  /// <summary>
  /// Gets whether this route has a catch-all parameter.
  /// </summary>
  public bool HasCatchAll => CatchAllParameterName != null;
  /// <summary>
  /// Gets or sets the specificity score used for ordering route matches.
  /// Higher values indicate more specific routes that should be tried first.
  /// </summary>
  public int Specificity { get; set; }
  /// <summary>
  /// Gets the minimum number of positional arguments required to match this route.
  /// For routes with catch-all, this is the number of segments minus one.
  /// For routes without catch-all, this is the exact number of segments.
  /// </summary>
  public int MinimumRequiredArgs => CatchAllParameterName != null ? PositionalTemplate.Count - 1 : PositionalTemplate.Count;
}
