namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Represents an option matcher in a route pattern that must be matched.
/// </summary>
public class OptionMatcher : RouteMatcher
{
  /// <summary>
  /// Gets the option match pattern (e.g., "--amend" or "-m").
  /// </summary>
  public string MatchPattern { get; }
  /// <summary>
  /// Gets whether this option expects a value.
  /// </summary>
  public bool ExpectsValue { get; }
  /// <summary>
  /// Gets the parameter name for the option value, if any.
  /// </summary>
  public string? ParameterName { get; }
  /// <summary>
  /// Gets the alternate form for this option (e.g., "-m" for "--message").
  /// </summary>
  public string? AlternateForm { get; }
  /// <summary>
  /// Gets the description for this option.
  /// </summary>
  public string? Description { get; }
  /// <summary>
  /// Gets whether this option is optional (can be omitted).
  /// </summary>
  public bool IsOptional { get; }
  /// <summary>
  /// Gets whether this option can be repeated to collect multiple values.
  /// </summary>
  public bool IsRepeated { get; }

  public OptionMatcher
  (
    string matchPattern,
    bool expectsValue = false,
    string? parameterName = null,
    string? alternateForm = null,
    string? description = null,
    bool isOptional = false,
    bool isRepeated = false
  )
  {
    MatchPattern = matchPattern ?? throw new ArgumentNullException(nameof(matchPattern));
    ExpectsValue = expectsValue;
    ParameterName = parameterName;
    AlternateForm = alternateForm;
    Description = description;
    IsOptional = isOptional;
    IsRepeated = isRepeated;
  }

  public override bool TryMatch(string arg, out string? extractedValue)
  {
    ArgumentNullException.ThrowIfNull(arg);

    extractedValue = null;

    // Direct match for the option pattern
    if (arg == MatchPattern)
      return true;

    // Check if arg matches the alternate form
    if (AlternateForm is not null && arg == AlternateForm)
      return true;

    // For short options, check grouped options (e.g., -abc contains -a)
    if (AlternateForm?.StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal) == true && AlternateForm.Length == 2)
    {
      if (arg.StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal) && arg.Length > 2 && !arg.StartsWith(CommonStrings.DoubleDash, StringComparison.Ordinal))
      {
        char shortChar = AlternateForm[1];
        return arg.Contains(shortChar.ToString(), StringComparison.Ordinal);
      }
    }

    return false;
  }

  public override string ToDisplayString() => MatchPattern;
}
