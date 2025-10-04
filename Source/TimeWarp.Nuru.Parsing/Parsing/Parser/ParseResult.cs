namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Represents the result of a parsing operation.
/// </summary>
/// <typeparam name="T">The type of the parsed value.</typeparam>
public class ParseResult<T>
{
  /// <summary>
  /// The parsed value, if parsing was successful.
  /// </summary>
  public T? Value { get; init; }
  /// <summary>
  /// True if parsing was successful, false otherwise.
  /// </summary>
  public bool Success { get; init; }
  /// <summary>
  /// List of parse errors encountered during parsing.
  /// </summary>
  public IReadOnlyList<ParseError>? ParseErrors { get; init; }
  /// <summary>
  /// List of semantic errors encountered during validation.
  /// </summary>
  public IReadOnlyList<SemanticError>? SemanticErrors { get; init; }
}
