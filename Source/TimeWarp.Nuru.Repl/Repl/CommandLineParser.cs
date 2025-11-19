namespace TimeWarp.Nuru.Repl;

using TimeWarp.Nuru.Repl.Input;

/// <summary>
/// Parses command line input strings into argument arrays, handling quotes and escapes.
/// </summary>
internal static class CommandLineParser
{
  /// <summary>
  /// Parses a command line string into an array of arguments, respecting quoted strings.
  /// </summary>
  /// <param name="input">The raw command line input string.</param>
  /// <returns>An array of parsed arguments.</returns>
  /// <example>
  /// <code>
  /// Parse("greet \"John Doe\" --loud")
  /// // Returns: ["greet", "John Doe", "--loud"]
  ///
  /// Parse("deploy 'staging' --message 'Deploy to staging'")
  /// // Returns: ["deploy", "staging", "--message", "Deploy to staging"]
  ///
  /// Parse("echo \"Hello\\\"World\"")
  /// // Returns: ["echo", "Hello\"World"]
  /// </code>
  /// </example>
  public static string[] Parse(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
    {
      return [];
    }

    List<string> arguments = [];
    StringBuilder currentArgument = new();
    bool inQuotes = false;
    char quoteChar = '\0';
    bool escape = false;
    bool hasArgument = false; // Track if we have a valid argument (including empty quoted strings)

    for (int i = 0; i < input.Length; i++)
    {
      char c = input[i];

      if (escape)
      {
        // Handle escaped character
        currentArgument.Append(c);
        escape = false;
        hasArgument = true;
        continue;
      }

      if (c == '\\' && i + 1 < input.Length)
      {
        char next = input[i + 1];
        // Only escape quotes and backslashes
        if (next == '"' || next == '\'' || next == '\\')
        {
          escape = true;
          continue;
        }
      }

      if (!inQuotes)
      {
        if (c == '"' || c == '\'')
        {
          // Start quoted section
          inQuotes = true;
          quoteChar = c;
          hasArgument = true; // Even empty quotes count as an argument
        }
        else if (char.IsWhiteSpace(c))
        {
          // End of argument
          if (hasArgument || currentArgument.Length > 0)
          {
            arguments.Add(currentArgument.ToString());
            currentArgument.Clear();
            hasArgument = false;
          }
        }
        else
        {
          currentArgument.Append(c);
          hasArgument = true;
        }
      }
      else
      {
        if (c == quoteChar)
        {
          // End quoted section
          inQuotes = false;
          quoteChar = '\0';
        }
        else
        {
          currentArgument.Append(c);
        }
      }
    }

    // Add final argument if any
    if (hasArgument || currentArgument.Length > 0)
    {
      arguments.Add(currentArgument.ToString());
    }

    return [.. arguments];
  }

  /// <summary>
  /// Parses a command line string into tokens with position information for syntax highlighting.
  /// </summary>
  /// <param name="input">The raw command line input string.</param>
  /// <returns>A list of tokens with position and type information.</returns>
  public static List<CommandLineToken> ParseWithPositions(string input)
  {
    var tokens = new List<CommandLineToken>();

    if (string.IsNullOrWhiteSpace(input))
      return tokens;

    int position = 0;

    while (position < input.Length)
    {
      // Parse whitespace
      int whitespaceStart = position;
      while (position < input.Length && char.IsWhiteSpace(input[position]))
        position++;

      if (position > whitespaceStart)
      {
        tokens.Add(new CommandLineToken(
          input[whitespaceStart..position],
          TokenType.Whitespace,
          whitespaceStart,
          position));
      }

      if (position >= input.Length) break;

      // Parse next token
      int tokenStart = position;

      // Handle quoted strings
      if (input[position] == '"' || input[position] == '\'')
      {
        char quoteChar = input[position];
        position++;

        while (position < input.Length && input[position] != quoteChar)
        {
          if (input[position] == '\\' && position + 1 < input.Length)
            position++; // Skip escaped character
          position++;
        }

        if (position < input.Length) position++; // Skip closing quote

        tokens.Add(new CommandLineToken(
          input[tokenStart..position],
          TokenType.StringLiteral,
          tokenStart,
          position));
      }
      // Handle options (--option, -o)
      else if (input[position] == '-')
      {
        while (position < input.Length && !char.IsWhiteSpace(input[position]))
          position++;

        string tokenText = input[tokenStart..position];
        TokenType type = tokenText.StartsWith("--", StringComparison.Ordinal) ? TokenType.LongOption : TokenType.ShortOption;

        tokens.Add(new CommandLineToken(tokenText, type, tokenStart, position));
      }
      // Handle regular arguments
      else
      {
        while (position < input.Length && !char.IsWhiteSpace(input[position]))
          position++;

        string tokenText = input[tokenStart..position];
        TokenType type = DetermineArgumentType(tokenText);

        tokens.Add(new CommandLineToken(tokenText, type, tokenStart, position));
      }
    }

    return tokens;
  }

  private static TokenType DetermineArgumentType(string token)
  {
    // Check if it's a numeric literal
    if (int.TryParse(token, out _) || double.TryParse(token, out _))
      return TokenType.Number;

    // Default to argument for now - command detection will be done by SyntaxHighlighter
    return TokenType.Argument;
  }
}
