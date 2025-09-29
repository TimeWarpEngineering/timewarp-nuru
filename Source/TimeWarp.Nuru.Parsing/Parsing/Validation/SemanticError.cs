namespace TimeWarp.Nuru.Parsing.Validation;

/// <summary>
/// Base class for all semantic validation errors in route patterns.
/// Semantic errors occur when the syntax is valid but the meaning is incorrect.
/// </summary>
public abstract record SemanticError(int Position, int Length)
{
  /// <summary>
  /// Returns a formatted string representation of the error.
  /// </summary>
  public abstract override string ToString();
}

/// <summary>
/// Duplicate parameter names error.
/// </summary>
public record DuplicateParameterNamesError(
  int Position,
  int Length,
  string ParameterName
) : SemanticError(Position, Length)
{
  public override string ToString() =>
    $"Semantic Error at position {Position}: Duplicate parameter name '{ParameterName}' found in route pattern";
}

/// <summary>
/// Conflicting optional parameters error.
/// </summary>
public record ConflictingOptionalParametersError(
  int Position,
  int Length,
  IReadOnlyList<string> ConflictingParameters
) : SemanticError(Position, Length)
{
  public override string ToString() =>
    $"Semantic Error at position {Position}: Multiple consecutive optional parameters create ambiguity: {string.Join(", ", ConflictingParameters)}";
}

/// <summary>
/// Catch-all parameter not at end error.
/// </summary>
public record CatchAllNotAtEndError(
  int Position,
  int Length,
  string CatchAllParameter,
  string FollowingSegment
) : SemanticError(Position, Length)
{
  public override string ToString() =>
    $"Semantic Error at position {Position}: Catch-all parameter '{CatchAllParameter}' must be the last segment in the route (found '{FollowingSegment}' after it)";
}

/// <summary>
/// Mixed catch-all with optional parameters error.
/// </summary>
public record MixedCatchAllWithOptionalError(
  int Position,
  int Length,
  string CatchAllParam,
  IReadOnlyList<string> OptionalParams
) : SemanticError(Position, Length)
{
  public override string ToString() =>
    $"Semantic Error at position {Position}: Cannot mix optional parameters [{string.Join(", ", OptionalParams)}] with catch-all parameter '{CatchAllParam}' in the same route";
}

/// <summary>
/// Duplicate option alias error.
/// </summary>
public record DuplicateOptionAliasError(
  int Position,
  int Length,
  string Alias,
  IReadOnlyList<string> ConflictingOptions
) : SemanticError(Position, Length)
{
  public override string ToString() =>
    $"Semantic Error at position {Position}: Option has duplicate short form '{Alias}' (conflicts with: {string.Join(", ", ConflictingOptions)})";
}

/// <summary>
/// Optional parameter before required parameter error.
/// </summary>
public record OptionalBeforeRequiredError(
  int Position,
  int Length,
  string OptionalParam,
  string RequiredParam
) : SemanticError(Position, Length)
{
  public override string ToString() =>
    $"Semantic Error at position {Position}: Optional parameter '{OptionalParam}' appears before required parameter '{RequiredParam}'";
}

/// <summary>
/// Invalid end-of-options separator error.
/// </summary>
public record InvalidEndOfOptionsSeparatorError(
  int Position,
  int Length,
  string Reason
) : SemanticError(Position, Length)
{
  public override string ToString() =>
    $"Semantic Error at position {Position}: Invalid use of end-of-options separator: {Reason}";
}

/// <summary>
/// Options after end-of-options separator error.
/// </summary>
public record OptionsAfterEndOfOptionsSeparatorError(
  int Position,
  int Length,
  string Option
) : SemanticError(Position, Length)
{
  public override string ToString() =>
    $"Semantic Error at position {Position}: Option '{Option}' appears after end-of-options separator '--'";
}

/// <summary>
/// Parameter after catch-all error.
/// </summary>
public record ParameterAfterCatchAllError(
  int Position,
  int Length,
  string Parameter,
  string CatchAll
) : SemanticError(Position, Length)
{
  public override string ToString() =>
    $"Semantic Error at position {Position}: Parameter '{Parameter}' appears after catch-all '{CatchAll}'";
}

/// <summary>
/// Parameter after repeated parameter error.
/// </summary>
public record ParameterAfterRepeatedError(
  int Position,
  int Length,
  string Parameter,
  string RepeatedParam
) : SemanticError(Position, Length)
{
  public override string ToString() =>
    $"Semantic Error at position {Position}: Parameter '{Parameter}' appears after repeated parameter '{RepeatedParam}'";
}
