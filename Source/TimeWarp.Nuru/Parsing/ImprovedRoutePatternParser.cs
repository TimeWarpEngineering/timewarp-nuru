namespace TimeWarp.Nuru.Parsing;

using TimeWarp.Nuru.Parsing.Ast;

/// <summary>
/// Improved route pattern parser that uses a proper AST-based approach
/// while maintaining compatibility with the existing ParsedRoute API.
/// </summary>
internal static class ImprovedRoutePatternParser
{
  private static readonly NewRoutePatternParser Parser = new NewRoutePatternParser();
  private static readonly ParsedRouteBuilder Builder = new();

  /// <summary>
  /// Parses a route pattern string into a ParsedRoute object.
  /// </summary>
  /// <param name="routePattern">The route pattern to parse (e.g., "git commit --amend").</param>
  /// <returns>A parsed representation of the route.</returns>
  /// <exception cref="ArgumentException">Thrown when the route pattern is invalid.</exception>
  public static ParsedRoute Parse(string routePattern)
  {
    ArgumentNullException.ThrowIfNull(routePattern);

    ParseResult<RoutePatternAst> result = Parser.Parse(routePattern);

    if (!result.Success)
    {
      // Format error messages for the exception
      string[] errorMessages = [.. result.Errors.Select(FormatError)];
      string combinedMessage = string.Join(Environment.NewLine, errorMessages);

      throw new ArgumentException($"Invalid route pattern '{routePattern}': {Environment.NewLine}{combinedMessage}");
    }

    return Builder.Build(result.Value!);
  }

  /// <summary>
  /// Tries to parse a route pattern string into a ParsedRoute object.
  /// </summary>
  /// <param name="routePattern">The route pattern to parse.</param>
  /// <param name="parsedRoute">The parsed route if successful.</param>
  /// <param name="errors">The parsing errors if unsuccessful.</param>
  /// <returns>True if parsing was successful, false otherwise.</returns>
  public static bool TryParse(string routePattern, out ParsedRoute? parsedRoute, out IReadOnlyList<ParseError> errors)
  {
    parsedRoute = null;
    errors = [];

    if (routePattern is null)
    {
      errors = [new ParseError("Route pattern cannot be null", 0, 0)];
      return false;
    }

    ParseResult<RoutePatternAst> result = Parser.Parse(routePattern);
    errors = result.Errors;

    if (result.Success)
    {
      parsedRoute = Builder.Build(result.Value!);
      return true;
    }

    return false;
  }

  private static string FormatError(ParseError error)
  {
    string message = $"Error at position {error.Position}: {error.Message}";
    if (error.Suggestion is not null)
    {
      message += $" (Suggestion: {error.Suggestion})";
    }

    return message;
  }
}

