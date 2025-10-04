namespace TimeWarp.Nuru.Tests.Lexer;

using Microsoft.Extensions.Logging;
using TimeWarp.Nuru.Parsing;

/// <summary>
/// Helper methods for lexer tests.
/// </summary>
public static class LexerTestHelper
{
  /// <summary>
  /// Creates a Lexer with optional trace logging enabled via TRACE_LEXER=1 environment variable.
  /// </summary>
  public static Lexer CreateLexer(string pattern)
  {
    ILogger<Lexer>? logger = null;

    if (Environment.GetEnvironmentVariable("TRACE_LEXER") == "1")
    {
      using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
      {
        builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddConsole();
      });
      logger = loggerFactory.CreateLogger<Lexer>();
    }

    return new Lexer(pattern, logger);
  }

  /// <summary>
  /// Creates a lexer and tokenizes the pattern in one call.
  /// </summary>
  public static IReadOnlyList<Token> Tokenize(string pattern)
  {
    Lexer lexer = CreateLexer(pattern);
    return lexer.Tokenize();
  }
}
