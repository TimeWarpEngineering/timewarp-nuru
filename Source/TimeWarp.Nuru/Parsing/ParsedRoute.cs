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
  /// Gets or sets the route parameters extracted from the pattern (e.g., {name}, {id:int}).
  /// Key is the parameter name, value contains parameter metadata.
  /// </summary>
  public Dictionary<string, RouteParameter> Parameters { get; set; } = new();

  /// <summary>
  /// Gets or sets whether this route has a catch-all parameter (e.g., {*args}).
  /// </summary>
  public bool HasCatchAll { get; set; }

  /// <summary>
  /// Gets or sets the name of the catch-all parameter if HasCatchAll is true.
  /// </summary>
  public string? CatchAllParameterName { get; set; }

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
  public int MinimumRequiredArgs => HasCatchAll ? PositionalTemplate.Count - 1 : PositionalTemplate.Count;
}

/// <summary>
/// Represents a parameter in a route pattern.
/// </summary>
public class RouteParameter
{
  /// <summary>
  /// Gets or sets the parameter name (without braces).
  /// </summary>
  public required string Name { get; set; }

  /// <summary>
  /// Gets or sets the position in the segments array (-1 for option parameters).
  /// </summary>
  public int Position { get; set; } = -1;

  /// <summary>
  /// Gets or sets whether this parameter is optional.
  /// </summary>
  public bool IsOptional { get; set; }

  /// <summary>
  /// Gets or sets the type constraint (e.g., "int" from {id:int}).
  /// </summary>
  public string? TypeConstraint { get; set; }

  /// <summary>
  /// Gets or sets the option name this parameter is associated with (e.g., "--message" for --message {msg}).
  /// </summary>
  public string? AssociatedOption { get; set; }
}
