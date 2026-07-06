#region Purpose
// Runtime matcher for a declared option token (long form and optional alternate form).
#endregion

#region Design
// Matching is EXACT string equality against the declared forms, mirroring the
// source-generated matcher path. Short forms may be multi-character (-bl).
// POSIX-style flag grouping (-abc matching -a) was removed deliberately: it was
// undocumented, matched the short char ANYWHERE in the arg (-e matched -help),
// and conflicts with multi-char short options (kanban 454-005/454-014). If
// bundling is ever wanted, design it as an opt-in feature with validator support.
#endregion

namespace TimeWarp.Nuru;

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
  /// <summary>
  /// Gets whether the parameter value for this option is optional.
  /// True for {mode?}, false for {mode}. Only relevant when ExpectsValue is true.
  /// </summary>
  public bool ParameterIsOptional { get; }

  public OptionMatcher
  (
    string matchPattern,
    bool expectsValue = false,
    string? parameterName = null,
    string? alternateForm = null,
    string? description = null,
    bool isOptional = false,
    bool isRepeated = false,
    bool parameterIsOptional = false
  )
  {
    MatchPattern = matchPattern ?? throw new ArgumentNullException(nameof(matchPattern));
    ExpectsValue = expectsValue;
    ParameterName = parameterName;
    AlternateForm = alternateForm;
    Description = description;
    IsOptional = isOptional;
    IsRepeated = isRepeated;
    ParameterIsOptional = parameterIsOptional;
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

    return false;
  }

  public override string ToDisplayString() => MatchPattern;
}
