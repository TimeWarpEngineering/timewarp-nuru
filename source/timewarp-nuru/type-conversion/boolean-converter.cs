namespace TimeWarp.Nuru;

/// <summary>
/// Parses boolean values from strings, supporting extended CLI-friendly spellings
/// beyond the standard <c>true</c>/<c>false</c>: <c>yes</c>/<c>no</c>, <c>1</c>/<c>0</c>,
/// <c>on</c>/<c>off</c>, <c>enabled</c>/<c>disabled</c>.
/// </summary>
/// <remarks>
/// This is the single source of truth for boolean parsing across both the
/// source-generated fast path and the runtime <see cref="DefaultTypeConverters"/> fallback.
/// </remarks>
public static class BooleanConverter
{
  private static readonly HashSet<string> TrueValues = new(StringComparer.OrdinalIgnoreCase)
  {
    "true", "yes", "1", "on", "enabled"
  };

  private static readonly HashSet<string> FalseValues = new(StringComparer.OrdinalIgnoreCase)
  {
    "false", "no", "0", "off", "disabled"
  };

  /// <summary>
  /// Attempts to parse the string as a boolean, accepting extended spellings.
  /// </summary>
  public static bool TryParse(string value, out bool result)
  {
    if (TrueValues.Contains(value))
    {
      result = true;
      return true;
    }

    if (FalseValues.Contains(value))
    {
      result = false;
      return true;
    }

    result = false;
    return false;
  }

  /// <summary>
  /// Parses the string as a boolean, throwing <see cref="FormatException"/> on failure.
  /// </summary>
  public static bool Parse(string value) =>
    TryParse(value, out bool result) ? result : throw new FormatException($"Cannot parse '{value}' as a boolean value.");
}
