namespace TimeWarp.Nuru.Parsing;

using TimeWarp.Nuru.Parsing.Ast;

/// <summary>
/// Interface for parsing route patterns into Abstract Syntax Trees.
/// </summary>
public interface IRouteParser
{
  /// <summary>
  /// Parses a route pattern string into a syntax tree.
  /// </summary>
  /// <param name="pattern">The route pattern to parse (e.g., "git commit --amend {message}").</param>
  /// <returns>A parse result containing either the syntax tree or parsing errors.</returns>
  ParseResult<RouteSyntax> Parse(string pattern);
}

