namespace TimeWarp.Nuru.Repl.Input;

/// <summary>
/// Provides syntax highlighting for command line input based on route patterns.
/// </summary>
internal sealed class SyntaxHighlighter
{
  private readonly EndpointCollection Endpoints;
  private readonly Dictionary<string, bool> CommandCache = [];
  private readonly ILogger<SyntaxHighlighter> Logger;

  public SyntaxHighlighter(EndpointCollection endpoints, ILoggerFactory loggerFactory)
  {
    ArgumentNullException.ThrowIfNull(loggerFactory);
    ArgumentNullException.ThrowIfNull(endpoints);

    Endpoints = endpoints;
    Logger = loggerFactory.CreateLogger<SyntaxHighlighter>();
  }

  public string Highlight(string input)
  {
    if (string.IsNullOrEmpty(input))
      return input;

    ReplLoggerMessages.SyntaxHighlightingStarted(Logger, input, null);
    List<CommandLineToken> tokens = CommandLineParser.ParseWithPositions(input);
    ReplLoggerMessages.TokensGenerated(Logger, tokens.Count, null);

    StringBuilder highlighted = new();

    foreach (CommandLineToken token in tokens)
    {
      ReplLoggerMessages.TokenProcessed(Logger, token.Type.ToString(), token.Text, null);
      highlighted.Append(GetHighlightedToken(token));
    }

    string result = highlighted.ToString();
    ReplLoggerMessages.HighlightedTextGenerated(Logger, result, null);
    return result;
  }

  private string GetHighlightedToken(CommandLineToken token)
  {
    return token.Type switch
    {
      TokenType.Command => token.Text.WithStyle(SyntaxColors.CommandColor),
      TokenType.StringLiteral => token.Text.WithStyle(SyntaxColors.StringColor),
      TokenType.Number => token.Text.WithStyle(SyntaxColors.NumberColor),
      TokenType.LongOption => token.Text.WithStyle(SyntaxColors.KeywordColor),
      TokenType.ShortOption => token.Text.WithStyle(SyntaxColors.OperatorColor),
      TokenType.Argument => DetermineArgumentHighlighting(token),
      TokenType.Whitespace => token.Text, // No coloring for whitespace
      _ => token.Text
    };
  }

  private string DetermineArgumentHighlighting(CommandLineToken token)
  {
    // Check if this argument is actually a command
    if (Endpoints is not null && IsKnownCommand(token.Text))
    {
      return token.Text.WithStyle(SyntaxColors.CommandColor);
    }

    // Check if it looks like a parameter (contains special chars)
    if
    (
      token.Text.Contains('{', StringComparison.Ordinal) ||
      token.Text.Contains('}', StringComparison.Ordinal) ||
      token.Text.Contains(':', StringComparison.Ordinal)
    )
    {
      return token.Text.WithStyle(SyntaxColors.ParameterColor);
    }

    // Default argument coloring
    return token.Text.WithStyle(SyntaxColors.DefaultTokenColor);
  }

  private bool IsKnownCommand(string token)
  {
    if (CommandCache.TryGetValue(token, out bool cached))
    {
      ReplLoggerMessages.CommandRecognitionChecked(Logger, token, cached, null);
      return cached;
    }

    bool isKnown =
      Endpoints!.Any
      (
        e =>
          e.RoutePattern.StartsWith(token + " ", StringComparison.Ordinal) ||
          e.RoutePattern == token
      );

    CommandCache[token] = isKnown;
    ReplLoggerMessages.CommandRecognitionChecked(Logger, token, isKnown, null);
    return isKnown;
  }
}
