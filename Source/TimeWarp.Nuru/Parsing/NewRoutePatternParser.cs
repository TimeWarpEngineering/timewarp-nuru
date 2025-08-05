namespace TimeWarp.Nuru.Parsing;

using TimeWarp.Nuru.Parsing.Ast;

/// <summary>
/// Recursive descent parser for route patterns.
/// </summary>
internal sealed class NewRoutePatternParser : IRoutePatternParser
{
  private IReadOnlyList<Token> Tokens = [];
  private int CurrentIndex;
  private readonly List<ParseError> Errors = [];

  /// <inheritdoc />
  public ParseResult<RoutePatternAst> Parse(string pattern)
  {
    ArgumentNullException.ThrowIfNull(pattern);

    // Reset state
    CurrentIndex = 0;
    Errors.Clear();

    // Tokenize input
    var lexer = new RoutePatternLexer(pattern);
    Tokens = lexer.Tokenize();

    // Parse tokens into AST
    try
    {
      List<SegmentNode> segments = ParsePattern();
      var ast = new RoutePatternAst(segments);

      return Errors.Count == 0
        ? new ParseResult<RoutePatternAst>
        {
          Value = ast,
          Success = true
        }
        : new ParseResult<RoutePatternAst>
        {
          Success = false,
          Errors = Errors
        };
    }
    catch (ParseException)
    {
      // Parsing failed completely
      return new ParseResult<RoutePatternAst>
      {
        Success = false,
        Errors = Errors
      };
    }
  }

  private List<SegmentNode> ParsePattern()
  {
    var segments = new List<SegmentNode>();

    while (!IsAtEnd())
    {
      try
      {
        SegmentNode? segment = ParseSegment();
        if (segment is not null)
        {
          segments.Add(segment);
        }
      }
      catch (ParseException)
      {
        // Try to recover and continue parsing
        Synchronize();
      }
    }

    return segments;
  }

  private SegmentNode? ParseSegment()
  {
    Token token = Peek();

    return token.Type switch
    {
      TokenType.LeftBrace => ParseParameter(),
      TokenType.DoubleDash or TokenType.SingleDash => ParseOption(),
      TokenType.Identifier => ParseLiteral(),
      TokenType.Invalid => ParseInvalidToken(),
      _ => null
    };
  }

  private LiteralNode ParseLiteral()
  {
    Token token = Consume(TokenType.Identifier, "Expected identifier");
    return new LiteralNode(token.Value)
    {
      Position = token.Position,
      Length = token.Length
    };
  }

  private ParameterNode ParseParameter()
  {
    Token leftBrace = Consume(TokenType.LeftBrace, "Expected '{'");
    int startPos = leftBrace.Position;

    // Check for catch-all marker
    bool isCatchAll = false;
    if (Match(TokenType.Asterisk))
    {
      isCatchAll = true;
    }

    // Parameter name
    Token nameToken = Consume(TokenType.Identifier, "Expected parameter name");
    string paramName = nameToken.Value;

    // Optional marker
    bool isOptional = Match(TokenType.Question);

    // Type constraint
    string? typeConstraint = null;
    if (Match(TokenType.Colon))
    {
      Token typeToken = Consume(TokenType.Identifier, "Expected type name after ':'");
      typeConstraint = typeToken.Value;

      // Type might also be optional (e.g., int?)
      if (Match(TokenType.Question))
      {
        isOptional = true;
        typeConstraint += "?";
      }
    }

    // Description
    string? description = null;
    if (Match(TokenType.Pipe))
    {
      description = ConsumeDescription(stopAtRightBrace: true);
    }

    Token rightBrace = Consume(TokenType.RightBrace, "Expected '}'");

    return new ParameterNode(paramName, isCatchAll, isOptional, typeConstraint, description)
    {
      Position = startPos,
      Length = rightBrace.EndPosition - startPos
    };
  }

  private OptionNode ParseOption()
  {
    Token optionToken = Current();
    bool isLong = optionToken.Type == TokenType.DoubleDash;
    Advance(); // Consume the dash(es)

    int startPos = optionToken.Position;

    // Option name
    Token nameToken = Consume(TokenType.Identifier, "Expected option name");
    string longName = nameToken.Value;
    string? shortName = null;

    // Check for short alias
    if (Match(TokenType.Comma))
    {
      Consume(TokenType.SingleDash, "Expected '-' after comma");
      Token shortToken = Consume(TokenType.Identifier, "Expected short option name");
      shortName = shortToken.Value;
    }

    // Description
    string? description = null;
    if (Match(TokenType.Pipe))
    {
      description = ConsumeDescription(stopAtRightBrace: false);
    }

    // Parameter for option
    ParameterNode? parameter = null;
    if (Check(TokenType.LeftBrace))
    {
      parameter = ParseParameter();
    }

    int endPos = Previous().EndPosition;

    return new OptionNode(longName, shortName, description, parameter)
    {
      Position = startPos,
      Length = endPos - startPos
    };
  }

  private SegmentNode? ParseInvalidToken()
  {
    Token token = Advance();

    // Special handling for angle bracket syntax
    if (token.Value.StartsWith('<') && token.Value.EndsWith('>'))
    {
      string paramName = token.Value[1..^1]; // Remove < and >
      string suggestion = $"{{{paramName}}}";

      AddError($"Invalid parameter syntax '{token.Value}'. Use curly braces for parameters.",
        token.Position, token.Length, suggestion);
    }
    else
    {
      AddError($"Invalid character '{token.Value}'", token.Position, token.Length);
    }

    return null; // Skip invalid tokens
  }

  // Helper methods for parsing

  private bool Match(params TokenType[] types)
  {
    foreach (TokenType type in types)
    {
      if (Check(type))
      {
        Advance();
        return true;
      }
    }

    return false;
  }

  private bool Check(TokenType type)
  {
    return !IsAtEnd() && Peek().Type == type;
  }

  private Token Advance()
  {
    if (!IsAtEnd()) CurrentIndex++;
    return Previous();
  }

  private bool IsAtEnd()
  {
    return CurrentIndex >= Tokens.Count || Peek().Type == TokenType.EndOfInput;
  }

  private Token Peek()
  {
    return CurrentIndex < Tokens.Count ? Tokens[CurrentIndex] : Token.EndOfInput(0);
  }

  private Token Previous()
  {
    return Tokens[CurrentIndex - 1];
  }

  private Token Current()
  {
    return Tokens[CurrentIndex];
  }

  private Token Consume(TokenType type, string message)
  {
    if (Check(type))
    {
      return Advance();
    }

    Token token = Peek();
    AddError(message, token.Position, token.Length);
    throw new ParseException(message);
  }

  private void AddError(string message, int position, int length, string? suggestion = null)
  {
    Errors.Add(new ParseError(message, position, length, suggestion));
  }

  private void Synchronize()
  {
    // Skip tokens until we find a likely start of a new segment
    while (!IsAtEnd())
    {
      Token token = Peek();
      if (token.Type is TokenType.LeftBrace or TokenType.DoubleDash or
          TokenType.SingleDash or TokenType.Identifier)
      {
        break;
      }

      Advance();
    }
  }

  private string ConsumeDescription(bool stopAtRightBrace)
  {
    var description = new List<string>();

    while (!IsAtEnd())
    {
      Token token = Peek();

      // Stop conditions
      if (stopAtRightBrace && token.Type == TokenType.RightBrace)
      {
        break;
      }

      if (!stopAtRightBrace)
      {
        // For option descriptions, stop at new segment indicators
        if (token.Type is TokenType.DoubleDash or TokenType.SingleDash or TokenType.LeftBrace)
        {
          break;
        }
      }

      // Add token value to description
      if (token.Type == TokenType.Identifier)
      {
        description.Add(token.Value);
        Advance();
      }
      else if (token.Type == TokenType.EndOfInput)
      {
        break;
      }
      else
      {
        // Skip unexpected tokens in descriptions
        Advance();
      }
    }

    return string.Join(" ", description);
  }

  /// <summary>
  /// Exception thrown when parsing fails and cannot continue.
  /// </summary>
  public sealed class ParseException : Exception
  {
    public ParseException(string message) : base(message) { }
    public ParseException()
    {
    }
    public ParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
  }
}
