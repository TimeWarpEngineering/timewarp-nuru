namespace TimeWarp.Nuru;

/// <summary>
/// Represents a literal matcher in a route pattern that must match exactly.
/// </summary>
public class LiteralMatcher : RouteMatcher
{
  /// <summary>
  /// Gets the literal value that must be matched.
  /// </summary>
  public string Value { get; }

  public LiteralMatcher(string value)
  {
    Value = value ?? throw new ArgumentNullException(nameof(value));
  }

  public override bool TryMatch(string arg, out string? extractedValue)
  {
    extractedValue = null;
    return string.Equals(arg, Value, StringComparison.Ordinal);
  }

  public override string ToDisplayString() => Value;
}
