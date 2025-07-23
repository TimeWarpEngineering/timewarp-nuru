namespace TimeWarp.Nuru.Parsing.Segments;

/// <summary>
/// Represents a literal segment in a route pattern that must match exactly.
/// </summary>
public class LiteralSegment : RouteSegment
{
    /// <summary>
    /// Gets the literal value that must be matched.
    /// </summary>
    public string Value { get; }

    public LiteralSegment(string value)
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