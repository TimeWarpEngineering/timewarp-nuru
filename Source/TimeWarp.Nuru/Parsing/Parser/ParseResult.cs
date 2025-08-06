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
  /// List of errors encountered during parsing.
  /// </summary>
  public IReadOnlyList<ParseError> Errors { get; init; } = [];
}

/// <summary>
/// Represents a parsing error with position information and optional suggestions.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Position">The character position where the error occurred.</param>
/// <param name="Length">The length of the problematic text.</param>
/// <param name="Suggestion">Optional suggestion for fixing the error.</param>
public record ParseError(string Message, int Position, int Length, string? Suggestion = null);

