namespace TimeWarp.Nuru.Parsing;

using System.Text;

/// <summary>
/// Lexer for tokenizing route pattern strings.
/// </summary>
public class RoutePatternLexer
{
  private readonly string Input;
  private int Position;
  private readonly List<Token> Tokens = [];

  /// <summary>
  /// Initializes a new instance of the <see cref="RoutePatternLexer"/> class.
  /// </summary>
  /// <param name="input">The input string to tokenize.</param>
  public RoutePatternLexer(string input)
  {
    this.Input = input ?? throw new ArgumentNullException(nameof(input));
  }

  /// <summary>
  /// Tokenizes the input string into a list of tokens.
  /// </summary>
  /// <returns>A list of tokens representing the input.</returns>
  public IReadOnlyList<Token> Tokenize()
  {
    Tokens.Clear();
    Position = 0;

    while (!IsAtEnd())
    {
      ScanToken();
    }

    Tokens.Add(Token.EndOfInput(Position));
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

    var sb = new StringBuilder();
    sb.Append("Tokens (").Append(tokens.Count).AppendLine("):");

    foreach (Token token in tokens)
    {
      sb.Append("  ").AppendLine(token.ToString());
    }

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
        AddToken(TokenType.LeftBrace, "{");
        break;

      case '}':
        AddToken(TokenType.RightBrace, "}");
        break;

      case ':':
        AddToken(TokenType.Colon, ":");
        break;

      case '?':
        AddToken(TokenType.Question, "?");
        break;

      case '|':
        AddToken(TokenType.Pipe, "|");
        break;

      case '*':
        AddToken(TokenType.Asterisk, "*");
        break;

      case '-':
        if (Match('-'))
        {
          AddToken(TokenType.DoubleDash, "--");
        }
        else
        {
          AddToken(TokenType.SingleDash, "-");
        }

        break;

      case ',':
        AddToken(TokenType.Comma, ",");
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
          AddToken(TokenType.Invalid, c.ToString());
        }

        break;
    }
  }

  private void ScanDescription()
  {
    throw new InvalidOperationException("ScanDescription should not be called!");
#pragma warning disable CS0162 // Unreachable code detected
    int start = Position;
    var description = new StringBuilder();

    // Check if we're inside a parameter by looking backwards for an unmatched '{'
    bool insideParameter = false;
    int braceCount = 0;
    for (int i = Position - 1; i >= 0; i--)
    {
      if (Input[i] == '}') braceCount++;
      else if (Input[i] == '{') braceCount--;
    }

    insideParameter = braceCount < 0;

    // Description continues until:
    // - Inside parameter: we hit '}'
    // - Outside parameter: we hit space followed by '-' or '{'
    while (!IsAtEnd())
    {
      char c = Peek();

      if (insideParameter && c == '}')
      {
        // Stop at closing brace when inside parameter
        break;
      }
      else if (!insideParameter && c == ' ')
      {
        // Check if next char starts a new segment
        if (Position + 1 >= Input.Length)
        {
          break;
        }

        char next = PeekNext();
        if (next == '-' || next == '{')
        {
          break;
        }
      }

      description.Append(Advance());
    }

    string descriptionText = description.ToString().Trim();
    if (descriptionText.Length > 0)
    {
      AddToken(TokenType.Description, descriptionText, start);
    }
#pragma warning restore CS0162
  }

  private void ScanIdentifier()
  {
    int start = Position - 1;
    var identifier = new StringBuilder();
    identifier.Append(Input[start]);

    while (!IsAtEnd())
    {
      char c = Peek();
      if (IsAlphaNumeric(c))
      {
        identifier.Append(Advance());
      }
      else if (c == '-' && Position + 1 < Input.Length && IsAlphaNumeric(Input[Position + 1]))
      {
        // Include dash if followed by alphanumeric (compound identifiers like "no-edit")
        identifier.Append(Advance());
      }
      else
      {
        break;
      }
    }

    AddToken(TokenType.Identifier, identifier.ToString(), start);
  }

  private void ScanInvalidParameterSyntax()
  {
    int start = Position - 1;
    var invalidSyntax = new StringBuilder();
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

    AddToken(TokenType.Invalid, invalidSyntax.ToString(), start);
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

  private void AddToken(TokenType type, string value)
  {
    int tokenStart = Position - value.Length;
    Tokens.Add(new Token(type, value, tokenStart, value.Length));
  }

  private void AddToken(TokenType type, string value, int start)
  {
    Tokens.Add(new Token(type, value, start, Position - start));
  }
}
