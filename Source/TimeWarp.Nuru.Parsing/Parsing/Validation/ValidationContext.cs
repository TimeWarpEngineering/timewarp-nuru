namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Context that holds metadata collected during semantic validation.
/// </summary>
internal sealed class ValidationContext
{
  /// <summary>
  /// Groups all parameters by name for duplicate detection.
  /// Includes both positional parameters and parameters in options.
  /// </summary>
  public Dictionary<string, List<SegmentSyntax>> ParametersByName { get; } = [];
  /// <summary>
  /// Maps option aliases to their option syntax for duplicate detection.
  /// </summary>
  public Dictionary<string, OptionSyntax> OptionAliases { get; } = [];
  /// <summary>
  /// All parameter segments in the route.
  /// </summary>
  public List<ParameterSyntax> Parameters { get; } = [];
  /// <summary>
  /// All option segments in the route.
  /// </summary>
  public List<OptionSyntax> Options { get; } = [];
  /// <summary>
  /// All literal segments in the route.
  /// </summary>
  public List<LiteralSyntax> Literals { get; } = [];
  /// <summary>
  /// All segments in order of appearance.
  /// </summary>
  public List<SegmentSyntax> AllSegments { get; } = [];
  /// <summary>
  /// Index of the end-of-options separator (--) if present.
  /// </summary>
  public int? EndOfOptionsIndex { get; set; }
  /// <summary>
  /// Whether the route contains optional parameters.
  /// </summary>
  public bool HasOptionalParameters { get; set; }
  /// <summary>
  /// Whether the route contains a catch-all parameter.
  /// </summary>
  public bool HasCatchAllParameter { get; set; }
}
