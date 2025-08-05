namespace TimeWarp.Nuru.Parsing;

using TimeWarp.Nuru.Parsing.Ast;

/// <summary>
/// Interface for parsing route patterns into Abstract Syntax Trees.
/// </summary>
public interface IRoutePatternParser
{
  /// <summary>
  /// Parses a route pattern string into an AST.
  /// </summary>
  /// <param name="pattern">The route pattern to parse (e.g., "git commit --amend {message}").</param>
  /// <returns>A parse result containing either the AST or parsing errors.</returns>
  ParseResult<RoutePatternAst> Parse(string pattern);
}

