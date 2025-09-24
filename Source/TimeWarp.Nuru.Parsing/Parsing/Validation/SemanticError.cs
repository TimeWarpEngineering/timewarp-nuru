namespace TimeWarp.Nuru.Parsing.Validation;

/// <summary>
/// Represents a semantic validation error in a route pattern.
/// Semantic errors occur when the syntax is valid but the meaning is incorrect.
/// </summary>
public record SemanticError(
    string Message,
    int Position,
    int Length,
    SemanticErrorType ErrorType
)
{
  /// <summary>
  /// Creates a formatted error message suitable for display.
  /// </summary>
  public string FormatError()
  {
    // Similar to parse errors but for semantic issues
    return $"Semantic Error at position {Position}: {Message}";
  }
}

/// <summary>
/// Types of semantic validation errors that can occur in route patterns.
/// </summary>
public enum SemanticErrorType
{
  /// <summary>
  /// The same parameter name is used multiple times in the route.
  /// </summary>
  DuplicateParameterNames,
  /// <summary>
  /// Multiple consecutive optional positional parameters create ambiguity.
  /// </summary>
  ConflictingOptionalParameters,
  /// <summary>
  /// A catch-all parameter is not the last positional segment.
  /// </summary>
  CatchAllNotAtEnd,
  /// <summary>
  /// Route mixes optional parameters with catch-all parameter.
  /// </summary>
  MixedCatchAllWithOptional,
  /// <summary>
  /// The same option alias is used for multiple options.
  /// </summary>
  DuplicateOptionAlias,
  /// <summary>
  /// An optional parameter appears before a required parameter.
  /// </summary>
  OptionalBeforeRequired,
  /// <summary>
  /// Invalid use of end-of-options separator.
  /// </summary>
  InvalidEndOfOptionsSeparator,
  /// <summary>
  /// Options appear after the end-of-options separator.
  /// </summary>
  OptionsAfterEndOfOptionsSeparator,
  /// <summary>
  /// Parameter appears after catch-all.
  /// </summary>
  ParameterAfterCatchAll,
  /// <summary>
  /// Parameter appears after repeated parameter.
  /// </summary>
  ParameterAfterRepeated
}
