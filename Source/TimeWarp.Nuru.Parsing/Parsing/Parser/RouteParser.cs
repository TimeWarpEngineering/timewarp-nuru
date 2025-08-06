namespace TimeWarp.Nuru.Parsing;

using TimeWarp.Nuru.Parsing;

/// <summary>
/// Recursive descent parser for route patterns.
/// </summary>
public sealed class RouteParser : IRouteParser
{
  private IReadOnlyList<Token> Tokens = [];
  private int CurrentIndex;
  private readonly List<ParseError> Errors = [];

  /// <inheritdoc />
  public ParseResult<RouteSyntax> Parse(string pattern)
  {
    ArgumentNullException.ThrowIfNull(pattern);

    // Reset state
    CurrentIndex = 0;
    Errors.Clear();

    // Tokenize input
    var lexer = new RoutePatternLexer(pattern);
    Tokens = lexer.Tokenize();

    if (EnableDiagnostics)
    {
      WriteLine($"Parsing pattern: '{pattern}'");
      WriteLine(RoutePatternLexer.DumpTokens(Tokens));
    }

    // Parse tokens into AST
    try
    {
      List<SegmentSyntax> segments = ParsePattern();
      var ast = new RouteSyntax(segments);

      // Perform semantic validation on the complete AST
      ValidateSemantics(ast);

      if (EnableDiagnostics && Errors.Count == 0)
      {
        WriteLine(DumpAst(ast));
      }

      return Errors.Count == 0
        ? new ParseResult<RouteSyntax>
        {
          Value = ast,
          Success = true
        }
        : new ParseResult<RouteSyntax>
        {
          Success = false,
          Errors = Errors
        };
    }
    catch (ParseException)
    {
      // Parsing failed completely
      return new ParseResult<RouteSyntax>
      {
        Success = false,
        Errors = Errors
      };
    }
  }

  /// <summary>
  /// Returns a diagnostic dump of a syntax tree for debugging.
  /// </summary>
  /// <param name="ast">The syntax tree to dump.</param>
  /// <returns>A formatted string showing the syntax tree structure.</returns>
  public static string DumpAst(RouteSyntax ast)
  {
    ArgumentNullException.ThrowIfNull(ast);

    var sb = new StringBuilder();
    sb.Append("AST has ").Append(ast.Segments.Count).AppendLine(" segments:");

    for (int i = 0; i < ast.Segments.Count; i++)
    {
      sb.Append("  [").Append(i).Append("] ");
      sb.AppendLine(ast.Segments[i].ToString());
    }

    return sb.ToString();
  }

  private List<SegmentSyntax> ParsePattern()
  {
    var segments = new List<SegmentSyntax>();

    while (!IsAtEnd())
    {
      try
      {
        SegmentSyntax? segment = ParseSegment();
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

  private SegmentSyntax? ParseSegment()
  {
    Token token = Peek();

    return token.Type switch
    {
      TokenType.LeftBrace => ParseParameter(),
      TokenType.DoubleDash or TokenType.SingleDash => ParseOption(),
      TokenType.Identifier => ParseLiteral(),
      TokenType.Invalid => ParseInvalidToken(),
      TokenType.RightBrace => HandleUnexpectedRightBrace(),
      _ => HandleUnexpectedToken()
    };
  }

  private LiteralSyntax ParseLiteral()
  {
    Token token = Consume(TokenType.Identifier, "Expected identifier");
    return new LiteralSyntax(token.Value)
    {
      Position = token.Position,
      Length = token.Length
    };
  }

  private ParameterSyntax ParseParameter()
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

      // Validate type constraint (NURU004)
      string baseType = typeConstraint.TrimEnd('?');
      if (!IsValidTypeConstraint(baseType))
      {
        AddError($"Invalid type constraint '{baseType}'. Supported types: string, int, double, bool, DateTime, Guid, long, decimal, TimeSpan",
          typeToken.Position,
          typeToken.Length,
          ParseErrorType.InvalidTypeConstraint);
      }
    }

    // Description
    string? description = null;
    if (Match(TokenType.Pipe))
    {
      description = ConsumeDescription(stopAtRightBrace: true);
    }

    Token rightBrace = Consume(TokenType.RightBrace, "Expected '}'", ParseErrorType.UnbalancedBraces);

    return new ParameterSyntax(paramName, isCatchAll, isOptional, typeConstraint, description)
    {
      Position = startPos,
      Length = rightBrace.EndPosition - startPos
    };
  }

  private OptionSyntax ParseOption()
  {
    Token optionToken = Current();

    bool isLong = optionToken.Type == TokenType.DoubleDash;
    Advance(); // Consume the dash(es)

    int startPos = optionToken.Position;

    // Option name
    Token optionNameToken = Consume(TokenType.Identifier, "Expected option name");

    string? longForm;
    string? shortForm = null;

    if (isLong)
    {
      longForm = optionNameToken.Value;
      // Check for short alias
      if (Match(TokenType.Comma))
      {
        Consume(TokenType.SingleDash, "Expected '-' after comma");
        Token shortToken = Consume(TokenType.Identifier, "Expected short option name");
        shortForm = shortToken.Value;
      }
    }
    else
    {
      longForm = null;
      shortForm = optionNameToken.Value;

      // Validate single dash options should be single character
      if (shortForm.Length > 1)
      {
        AddError($"Invalid option format '-{shortForm}'. Single dash options must be single character (use --{shortForm} for long form)",
          optionNameToken.Position - 1, // Include the dash
          optionNameToken.Length + 1,
          ParseErrorType.InvalidOptionFormat);
      }
    }

    // Description
    string? description = null;
    if (Match(TokenType.Pipe))
    {
      description = ConsumeDescription(stopAtRightBrace: false);
    }

    // Parameter for option
    ParameterSyntax? parameter = null;
    if (Check(TokenType.LeftBrace))
    {
      parameter = ParseParameter();
    }

    int endPos = Previous().EndPosition;

    return new OptionSyntax(longForm, shortForm, description, parameter)
    {
      Position = startPos,
      Length = endPos - startPos
    };
  }

  private SegmentSyntax? ParseInvalidToken()
  {
    Token token = Advance();

    // Special handling for angle bracket syntax
    if (token.Value.StartsWith('<') && token.Value.EndsWith('>'))
    {
      string paramName = token.Value[1..^1]; // Remove < and >
      string suggestion = $"{{{paramName}}}";

      AddError($"Invalid parameter syntax '{token.Value}'. Use curly braces for parameters.",
        token.Position, token.Length, ParseErrorType.InvalidParameterSyntax, suggestion);
    }
    else
    {
      AddError($"Invalid character '{token.Value}'", token.Position, token.Length, ParseErrorType.Generic);
    }

    return null; // Skip invalid tokens
  }

  private SegmentSyntax? HandleUnexpectedRightBrace()
  {
    Token token = Advance(); // Consume the right brace to avoid infinite loop
    AddError("Unexpected '}' - closing brace without matching opening brace",
      token.Position, token.Length, ParseErrorType.UnbalancedBraces);
    return null;
  }

  private SegmentSyntax? HandleUnexpectedToken()
  {
    Token token = Advance(); // Consume the unexpected token to avoid infinite loop
    AddError($"Unexpected token '{token.Value}' of type {token.Type}",
      token.Position, token.Length, ParseErrorType.Generic);
    return null;
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

  private Token Consume(TokenType type, string message, ParseErrorType errorType = ParseErrorType.Generic)
  {
    if (Check(type))
    {
      return Advance();
    }

    Token token = Peek();
    AddError(message, token.Position, token.Length, errorType);
    throw new ParseException(message);
  }

  private void AddError(string message, int position, int length, ParseErrorType errorType = ParseErrorType.Generic, string? suggestion = null)
  {
    Errors.Add(new ParseError(message, position, length, errorType, suggestion));
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

  private static bool IsValidTypeConstraint(string type)
  {
    return type switch
    {
      "string" => true,
      "int" => true,
      "long" => true,
      "double" => true,
      "decimal" => true,
      "bool" => true,
      "DateTime" => true,
      "Guid" => true,
      "TimeSpan" => true,
      _ => false
    };
  }

  private void ValidateSemantics(RouteSyntax ast)
  {
    // Track parameter names for duplicate detection
    var parameterNames = new Dictionary<string, ParameterSyntax>();

    // Track consecutive optional parameters
    ParameterSyntax? lastOptionalParam = null;

    // NURU005: Check if catch-all parameter is at the end
    for (int i = 0; i < ast.Segments.Count; i++)
    {
      if (ast.Segments[i] is ParameterSyntax param)
      {
        // NURU006: Check for duplicate parameter names
        if (parameterNames.TryGetValue(param.Name, out ParameterSyntax? existingParam))
        {
          AddError($"Duplicate parameter name '{param.Name}' found in route pattern",
            param.Position,
            param.Length,
            ParseErrorType.DuplicateParameterNames);
        }
        else
        {
          parameterNames[param.Name] = param;
        }

        // NURU007: Check for consecutive optional positional parameters
        if (param.IsOptional && !param.IsCatchAll)
        {
          if (lastOptionalParam is not null)
          {
            // Found consecutive optional parameters
            AddError($"Multiple consecutive optional positional parameters create ambiguity: {lastOptionalParam.Name}? {param.Name}?",
              param.Position,
              param.Length,
              ParseErrorType.ConflictingOptionalParameters);
          }

          lastOptionalParam = param;
        }
        else
        {
          // Reset when we hit a required parameter
          lastOptionalParam = null;
        }

        // NURU005: Check if catch-all is at the end
        if (param.IsCatchAll && i < ast.Segments.Count - 1)
        {
          AddError($"Catch-all parameter '{param.Name}' must be the last segment in the route",
            param.Position,
            param.Length,
            ParseErrorType.CatchAllNotAtEnd);
        }
      }
      else
      {
        // Reset when we hit a non-parameter segment (literal or option)
        lastOptionalParam = null;
      }
    }
  }
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
