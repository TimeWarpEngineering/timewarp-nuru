namespace TimeWarp.Nuru;

/// <summary>
/// Lexer for tokenizing route pattern strings.
/// </summary>
internal class Lexer
{
#if !ANALYZER_BUILD
  private readonly ILogger<Lexer> Logger;
#endif
  private readonly string Input;
  private int Position;
  private readonly List<Token> Tokens = [];

  /// <summary>
  /// Initializes a new instance of the <see cref="Lexer"/> class.
  /// </summary>
  /// <param name="input">The input string to tokenize.</param>
#if !ANALYZER_BUILD
  public Lexer(string input) : this(input, null) { }

  public Lexer(string input, ILogger<Lexer>? logger = null)
  {
    this.Input = input ?? throw new ArgumentNullException(nameof(input));
    Logger = logger ?? NullLogger<Lexer>.Instance;
  }
#else
  public Lexer(string input)
  {
    this.Input = input ?? throw new ArgumentNullException(nameof(input));
  }
#endif

  /// <summary>
  /// Tokenizes the input string into a list of tokens.
  /// </summary>
  /// <returns>A list of tokens representing the input.</returns>
  public IReadOnlyList<Token> Tokenize()
  {
    Tokens.Clear();
    Position = 0;

#if !ANALYZER_BUILD
    ParsingLoggerMessages.StartingLexicalAnalysis(Logger, Input, null);
#endif

    while (!IsAtEnd())
    {
      ScanToken();
    }

    Tokens.Add(Token.EndOfInput(Position));

#if !ANALYZER_BUILD
    if (Logger.IsEnabled(LogLevel.Trace))
    {
      ParsingLoggerMessages.CompletedLexicalAnalysis(Logger, Tokens.Count, null);
      ParsingLoggerMessages.DumpingTokens(Logger, DumpTokens(Tokens), null);
    }
#endif

    return Tokens;
  }

  /// <summary>
  /// Returns a diagnostic dump of the tokens for debugging.
  /// </summary>
  /// <param name="tokens">The tokens to dump.</param>
  /// <returns>A formatted string showing all tokens.</returns>
  public static string DumpTokens(IReadOnlyList<Token> tokens)
  {
    ArgumentNullException.ThrowIfNull(tokens);

    StringBuilder sb = new();
    sb.Append("Tokens (").Append(tokens.Count).Append("): ");

    List<string> tokenStrings = [];
    foreach (Token token in tokens)
    {
      tokenStrings.Add(token.ToString());
    }

    sb.AppendJoin(" | ", tokenStrings);

    return sb.ToString();
  }

  private void ScanToken()
  {
    char c = Advance();

    switch (c)
    {
      case ' ':
      case '\t':
      case '\r':
      case '\n':
        // Skip whitespace
        break;

      case '{':
        AddToken(RouteTokenType.LeftBrace, "{");
        break;

      case '}':
        AddToken(RouteTokenType.RightBrace, "}");
        break;

      case ':':
        AddToken(RouteTokenType.Colon, ":");
        break;

      case '?':
        AddToken(RouteTokenType.Question, "?");
        break;

      case '|':
        AddToken(RouteTokenType.Pipe, "|");
        break;

      case '*':
        AddToken(RouteTokenType.Asterisk, "*");
        break;

      case '-':
        if (Match('-'))
        {
          // Check if this is standalone -- (end-of-options separator)
          // It should be followed by whitespace or end of input
          if (IsAtEnd() || Peek() == ' ')
          {
            AddToken(RouteTokenType.EndOfOptions, "--");
          }
          else
          {
            AddToken(RouteTokenType.DoubleDash, "--");
          }
        }
        else
        {
          // Single dash - valid for both single and multi-character options
          // Examples: -h, -v, -bl, -verbosity
          // Real-world tools like dotnet CLI use multi-char short options: dotnet run -bl
          AddToken(RouteTokenType.SingleDash, "-");
        }

        break;

      case ',':
        AddToken(RouteTokenType.Comma, ",");
        break;

      case '<':
        // This is likely an error - user used angle brackets instead of curly braces
        ScanInvalidParameterSyntax();
        break;

      default:
        if (IsAlphaNumeric(c))
        {
          ScanIdentifier();
        }
        else
        {
          // Invalid character
          AddToken(RouteTokenType.Invalid, c.ToString());
        }

        break;
    }
  }

  private void ScanIdentifier()
  {
    int start = Position - 1;
    StringBuilder identifier = new();
    identifier.Append(Input[start]);

    while (!IsAtEnd())
    {
      char c = Peek();
      if (IsAlphaNumeric(c))
      {
        identifier.Append(Advance());
      }
      else if (c == '-')
      {
        // Check for various dash patterns
        if (Position + 1 >= Input.Length)
        {
          // Trailing dash at end of input: "test-"
          identifier.Append(Advance());
          AddToken(RouteTokenType.Invalid, identifier.ToString(), start);
          return;
        }

        char next = Input[Position + 1];
        if (next == '-')
        {
          // Double dash within identifier: "test--case"
          // Scan the rest as invalid
          while (!IsAtEnd() && (IsAlphaNumeric(Peek()) || Peek() == '-'))
          {
            identifier.Append(Advance());
          }

          AddToken(RouteTokenType.Invalid, identifier.ToString(), start);
          return;
        }
        else if (IsAlphaNumeric(next))
        {
          // Valid compound identifier: "no-edit"
          identifier.Append(Advance());
        }
        else
        {
          // Trailing dash before space or other char: "test- " or "test-}"
          identifier.Append(Advance());
          AddToken(RouteTokenType.Invalid, identifier.ToString(), start);
          return;
        }
      }
      else
      {
        break;
      }
    }

    AddToken(RouteTokenType.Identifier, identifier.ToString(), start);
  }

  private void ScanInvalidParameterSyntax()
  {
    int start = Position - 1;
    StringBuilder invalidSyntax = new();
    invalidSyntax.Append('<');

    // Scan until we find the closing > or end of input
    while (!IsAtEnd() && Peek() != '>')
    {
      invalidSyntax.Append(Advance());
    }

    if (!IsAtEnd() && Peek() == '>')
    {
      invalidSyntax.Append(Advance()); // Include the closing >
    }

    AddToken(RouteTokenType.Invalid, invalidSyntax.ToString(), start);
  }

  private bool Match(char expected)
  {
    if (IsAtEnd() || Input[Position] != expected)
    {
      return false;
    }

    Position++;
    return true;
  }

  private char Advance()
  {
    return Input[Position++];
  }

  private char Peek()
  {
    return IsAtEnd() ? '\0' : Input[Position];
  }

  private char PeekNext()
  {
    return Position + 1 >= Input.Length ? '\0' : Input[Position + 1];
  }

  private bool IsAtEnd()
  {
    return Position >= Input.Length;
  }

  private static bool IsAlphaNumeric(char c)
  {
    return char.IsLetterOrDigit(c) || c == '_';
  }

  private void AddToken(RouteTokenType type, string value)
  {
    int tokenStart = Position - value.Length;
    Tokens.Add(new Token(type, value, tokenStart, value.Length));
  }

  private void AddToken(RouteTokenType type, string value, int start)
  {
    Tokens.Add(new Token(type, value, start, Position - start));
  }
}
