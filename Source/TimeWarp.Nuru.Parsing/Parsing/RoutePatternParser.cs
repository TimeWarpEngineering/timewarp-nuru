namespace TimeWarp.Nuru.Parsing;

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

  public static CompiledRoute Parse(string routePattern, ILoggerFactory? loggerFactory)
  {
    ArgumentNullException.ThrowIfNull(routePattern);

    // Create parser and compiler with typed loggers if factory provided
    RouteParser parser = loggerFactory is not null
      ? new RouteParser(loggerFactory.CreateLogger<RouteParser>(), loggerFactory)
      : new RouteParser();
    RouteCompiler compiler = loggerFactory is not null
      ? new RouteCompiler(loggerFactory.CreateLogger<RouteCompiler>())
      : new RouteCompiler();
    ParseResult<RouteSyntax> result = parser.Parse(routePattern);

    if (!result.Success)
    {
      throw new RoutePatternException(routePattern, result.ParseErrors, result.SemanticErrors);
    }

    return compiler.Compile(result.Value!);
  }

  /// <summary>
  /// Tries to parse a route pattern string into a CompiledRoute object.
  /// </summary>
  /// <param name="routePattern">The route pattern to parse.</param>
  /// <param name="compiledRoute">The compiled route if successful.</param>
  /// <param name="errors">The parsing errors if unsuccessful.</param>
  /// <param name="semanticErrors">The semantic errors if unsuccessful.</param>
  /// <returns>True if parsing was successful, false otherwise.</returns>
  public static bool TryParse(string routePattern, out CompiledRoute? compiledRoute, out IReadOnlyList<ParseError>? errors, out IReadOnlyList<SemanticError>? semanticErrors)
  {
    compiledRoute = null;
    errors = null;
    semanticErrors = null;

    if (routePattern is null)
    {
      errors = [new NullPatternError(0, 0)];
      return false;
    }

    // Create parser and compiler without logger for now
    var parser = new RouteParser();
    var compiler = new RouteCompiler();
    ParseResult<RouteSyntax> result = parser.Parse(routePattern);
    errors = result.ParseErrors;
    semanticErrors = result.SemanticErrors;

    if (result.Success)
    {
      compiledRoute = compiler.Compile(result.Value!);
      return true;
    }

    return false;
  }
}
