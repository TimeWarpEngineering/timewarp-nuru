namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Base class for all parsing errors with position information.
/// </summary>
public abstract record ParseError(int Position, int Length)
{
  /// <summary>
  /// Returns a formatted string representation of the error.
  /// </summary>
  public abstract override string ToString();
}

/// <summary>
/// Invalid parameter syntax error (e.g., using angle brackets instead of curly braces).
/// </summary>
public record InvalidParameterSyntaxError(
  int Position,
  int Length,
  string InvalidSyntax,
  string Suggestion
) : ParseError(Position, Length)
{
  public override string ToString() =>
    $"Error at position {Position}: Invalid parameter syntax '{InvalidSyntax}' - use curly braces: {{{Suggestion}}}";
}

/// <summary>
/// Unbalanced braces error in route pattern.
/// </summary>
public record UnbalancedBracesError(
  int Position,
  int Length,
  string Pattern
) : ParseError(Position, Length)
{
  public override string ToString() =>
    $"Error at position {Position}: Unbalanced braces in route pattern: {Pattern}";
}

/// <summary>
/// Invalid option format error.
/// </summary>
public record InvalidOptionFormatError(
  int Position,
  int Length,
  string InvalidOption
) : ParseError(Position, Length)
{
  public override string ToString() =>
    $"Error at position {Position}: Invalid option format '{InvalidOption}' - options must start with '--' or '-'";
}

/// <summary>
/// Invalid type constraint error.
/// </summary>
public record InvalidTypeConstraintError(
  int Position,
  int Length,
  string InvalidType
) : ParseError(Position, Length)
{
  private const string SupportedTypes = "string, int, double, bool, DateTime, Guid, long, decimal, TimeSpan";

  public override string ToString() =>
    $"Error at position {Position}: Invalid type constraint '{InvalidType}' - supported types: {SupportedTypes}";
}

/// <summary>
/// Invalid character error in route pattern.
/// </summary>
public record InvalidCharacterError(
  int Position,
  int Length,
  string Character
) : ParseError(Position, Length)
{
  public override string ToString() =>
    $"Error at position {Position}: Invalid character '{Character}'";
}

/// <summary>
/// Unexpected token error when expecting a specific token type.
/// </summary>
public record UnexpectedTokenError(
  int Position,
  int Length,
  string Found,
  string Expected
) : ParseError(Position, Length)
{
  public override string ToString() =>
    $"Error at position {Position}: {Expected}, but found '{Found}'";
}

/// <summary>
/// Null pattern error.
/// </summary>
public record NullPatternError(
  int Position,
  int Length
) : ParseError(Position, Length)
{
  public override string ToString() =>
    "Route pattern cannot be null";
}