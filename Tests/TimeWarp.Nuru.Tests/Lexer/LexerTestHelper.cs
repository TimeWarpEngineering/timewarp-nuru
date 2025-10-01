namespace TimeWarp.Nuru.Tests.Lexer;

using Microsoft.Extensions.Logging;
using TimeWarp.Nuru.Parsing;

/// <summary>
/// Helper methods for lexer tests.
/// </summary>
public static class LexerTestHelper
{
  /// <summary>
  /// Creates a RoutePatternLexer with optional trace logging enabled via TRACE_LEXER=1 environment variable.
  /// </summary>
  public static RoutePatternLexer CreateLexer(string pattern)
  {
    ILogger<RoutePatternLexer>? logger = null;

    if (Environment.GetEnvironmentVariable("TRACE_LEXER") == "1")
    {
      using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
      {
        builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddConsole();
      });
      logger = loggerFactory.CreateLogger<RoutePatternLexer>();
    }

    return new RoutePatternLexer(pattern, logger);
  }

  /// <summary>
  /// Creates a lexer and tokenizes the pattern in one call.
  /// </summary>
  public static IReadOnlyList<Token> Tokenize(string pattern)
  {
    RoutePatternLexer lexer = CreateLexer(pattern);
    return lexer.Tokenize();
  }
}
