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
/// Types of parsing errors that map to analyzer diagnostics.
/// </summary>
public enum ParseErrorType
{
  /// <summary>
  /// Generic parse error without specific diagnostic mapping.
  /// </summary>
  Generic,
  /// <summary>
  /// Invalid parameter syntax (e.g., angle brackets instead of curly braces) - NURU001.
  /// </summary>
  InvalidParameterSyntax,
  /// <summary>
  /// Unbalanced braces in route pattern - NURU002.
  /// </summary>
  UnbalancedBraces,
  /// <summary>
  /// Invalid option format - NURU003.
  /// </summary>
  InvalidOptionFormat,
  /// <summary>
  /// Invalid type constraint - NURU004.
  /// </summary>
  InvalidTypeConstraint,
  /// <summary>
  /// Catch-all parameter not at end - NURU005.
  /// </summary>
  CatchAllNotAtEnd,
  /// <summary>
  /// Duplicate parameter names - NURU006.
  /// </summary>
  DuplicateParameterNames,
  /// <summary>
  /// Conflicting optional parameters - NURU007.
  /// </summary>
  ConflictingOptionalParameters,
  /// <summary>
  /// Mixed catch-all with optional parameters - NURU008.
  /// </summary>
  MixedCatchAllWithOptional,
  /// <summary>
  /// Option with duplicate alias - NURU009.
  /// </summary>
  DuplicateOptionAlias
}

/// <summary>
/// Represents a parsing error with position information and optional suggestions.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Position">The character position where the error occurred.</param>
/// <param name="Length">The length of the problematic text.</param>
/// <param name="ErrorType">The type of error for diagnostic mapping.</param>
/// <param name="Suggestion">Optional suggestion for fixing the error.</param>
public record ParseError(
    string Message,
    int Position,
    int Length,
    ParseErrorType ErrorType = ParseErrorType.Generic,
    string? Suggestion = null);

