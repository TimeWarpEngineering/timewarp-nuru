namespace TimeWarp.Nuru.Parsing.Segments;

/// <summary>
/// Represents an option segment in a route pattern that must be matched.
/// </summary>
public class OptionSegment : RouteSegment
{
  /// <summary>
  /// Gets the option name (e.g., "--amend" or "-m").
  /// </summary>
  public string Name { get; }
  /// <summary>
  /// Gets whether this option expects a value.
  /// </summary>
  public bool ExpectsValue { get; }
  /// <summary>
  /// Gets the parameter name for the option value, if any.
  /// </summary>
  public string? ValueParameterName { get; }
  /// <summary>
  /// Gets the short form alias for this option (e.g., "-m" for "--message").
  /// </summary>
  public string? ShortAlias { get; }

  public OptionSegment(string name, bool expectsValue = false, string? valueParameterName = null, string? shortAlias = null)
  {
    Name = name ?? throw new ArgumentNullException(nameof(name));
    ExpectsValue = expectsValue;
    ValueParameterName = valueParameterName;
    ShortAlias = shortAlias;
  }

  public override bool TryMatch(string arg, out string? extractedValue)
  {
    ArgumentNullException.ThrowIfNull(arg);

    extractedValue = null;

    // Direct match for the option name
    if (arg == Name)
      return true;

    // Check if arg matches the short alias
    if (ShortAlias is not null && arg == ShortAlias)
      return true;

    // For short options, check grouped options (e.g., -abc contains -a)
    if (ShortAlias?.StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal) == true && ShortAlias.Length == 2)
    {
      if (arg.StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal) && arg.Length > 2 && !arg.StartsWith(CommonStrings.DoubleDash, StringComparison.Ordinal))
      {
        char shortChar = ShortAlias[1];
        return arg.Contains(shortChar.ToString(), StringComparison.Ordinal);
      }
    }

    return false;
  }

  public override string ToDisplayString() => Name;
}
