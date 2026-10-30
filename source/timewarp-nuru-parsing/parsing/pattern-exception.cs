namespace TimeWarp.Nuru;

/// <summary>
/// Exception thrown when a route pattern cannot be parsed or validated.
/// </summary>
public class PatternException : Exception
{
  /// <summary>
  /// The route pattern that caused the error.
  /// </summary>
  public string RoutePattern { get; }
  /// <summary>
  /// Parse errors encountered during parsing.
  /// </summary>
  public IReadOnlyList<ParseError>? ParseErrors { get; }
  /// <summary>
  /// Semantic errors encountered during validation.
  /// </summary>
  public IReadOnlyList<SemanticError>? SemanticErrors { get; }

  /// <summary>
  /// Creates a new <see cref="PatternException"/> with no pattern or error details.
  /// </summary>
  public PatternException()
  {
    RoutePattern = string.Empty;
  }

  /// <summary>
  /// Creates a new <see cref="PatternException"/> with the given message and no error details.
  /// </summary>
  /// <param name="message">The error message.</param>
  public PatternException(string message)
    : base(message)
  {
    RoutePattern = string.Empty;
  }

  /// <summary>
  /// Creates a new <see cref="PatternException"/> with the given message and inner exception.
  /// </summary>
  /// <param name="message">The error message.</param>
  /// <param name="innerException">The exception that caused this failure.</param>
  public PatternException(string message, Exception innerException)
    : base(message, innerException)
  {
    RoutePattern = string.Empty;
  }

  /// <summary>
  /// Creates a new <see cref="PatternException"/> with parsing errors.
  /// </summary>
  public PatternException
  (
    string routePattern,
    IReadOnlyList<ParseError>? parseErrors,
    IReadOnlyList<SemanticError>? semanticErrors
  ) : base(FormatMessage(routePattern, parseErrors, semanticErrors))
  {
    ArgumentNullException.ThrowIfNull(routePattern);
    if (parseErrors is null && semanticErrors is null)
      throw new ArgumentException("At least one of parseErrors or semanticErrors must be provided");

    RoutePattern = routePattern;
    ParseErrors = parseErrors;
    SemanticErrors = semanticErrors;
  }

  private static string FormatMessage
  (
    string routePattern,
    IReadOnlyList<ParseError>? parseErrors,
    IReadOnlyList<SemanticError>? semanticErrors
  )
  {
    List<string> allErrors = parseErrors?.Select(e => e.ToString()).ToList() ?? [];
    if (semanticErrors is not null)
      allErrors.AddRange(semanticErrors.Select(e => e.ToString()));

    return $"Invalid route pattern '{routePattern}': \n{string.Join("\n", allErrors)}";
  }
}
