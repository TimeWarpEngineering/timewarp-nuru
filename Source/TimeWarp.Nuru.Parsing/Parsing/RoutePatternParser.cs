namespace TimeWarp.Nuru.Parsing;

using TimeWarp.Nuru.Parsing;

/// <summary>
/// Parses route pattern strings into CompiledRoute objects.
/// </summary>
public static class RoutePatternParser
{

  /// <summary>
  /// Parses a route pattern string into a CompiledRoute object.
  /// </summary>
  /// <param name="routePattern">The route pattern to parse (e.g., "git commit --amend").</param>
  /// <returns>A compiled representation of the route.</returns>
  /// <exception cref="ArgumentException">Thrown when the route pattern is invalid.</exception>
  public static CompiledRoute Parse(string routePattern)
  {
    return Parse(routePattern, null);
  }

  public static CompiledRoute Parse(string routePattern, ILogger? logger)
  {
    ArgumentNullException.ThrowIfNull(routePattern);

    // Create parser and compiler with logger if provided
    var parser = new RouteParser(logger as ILogger<RouteParser>);
    var compiler = new RouteCompiler(logger as ILogger<RouteCompiler>);
    ParseResult<RouteSyntax> result = parser.Parse(routePattern);

    if (!result.Success)
    {
      // Format error messages for the exception
      string[] errorMessages = [.. result.Errors.Select(FormatError)];
      string combinedMessage = string.Join("\n", errorMessages);

      throw new ArgumentException($"Invalid route pattern '{routePattern}': \n{combinedMessage}");
    }

    return compiler.Compile(result.Value!);
  }

  /// <summary>
  /// Tries to parse a route pattern string into a CompiledRoute object.
  /// </summary>
  /// <param name="routePattern">The route pattern to parse.</param>
  /// <param name="compiledRoute">The compiled route if successful.</param>
  /// <param name="errors">The parsing errors if unsuccessful.</param>
  /// <returns>True if parsing was successful, false otherwise.</returns>
  public static bool TryParse(string routePattern, out CompiledRoute? compiledRoute, out IReadOnlyList<ParseError> errors)
  {
    compiledRoute = null;
    errors = [];

    if (routePattern is null)
    {
      errors = [new ParseError("Route pattern cannot be null", 0, 0)];
      return false;
    }

    // Create parser and compiler without logger for now
    var parser = new RouteParser();
    var compiler = new RouteCompiler();
    ParseResult<RouteSyntax> result = parser.Parse(routePattern);
    errors = result.Errors;

    if (result.Success)
    {
      compiledRoute = compiler.Compile(result.Value!);
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
