namespace TimeWarp.Nuru;

/// <summary>
/// Recursive descent parser for route patterns.
/// </summary>
internal sealed partial class Parser : IParser
{
  private readonly ILogger<Parser> Logger;
  private readonly ILoggerFactory? LoggerFactory;
  private IReadOnlyList<Token> Tokens = [];
  private int CurrentIndex;
  private List<ParseError>? ParseErrors;

  public Parser() : this(null, null) { }

  public Parser(ILogger<Parser>? logger = null, ILoggerFactory? loggerFactory = null)
  {
    Logger = logger ?? NullLogger<Parser>.Instance;
    LoggerFactory = loggerFactory;
  }

  /// <inheritdoc />
  public ParseResult<Syntax> Parse(string pattern)
  {
    ArgumentNullException.ThrowIfNull(pattern);

    // Reset state
    CurrentIndex = 0;
    ParseErrors = null;

    // Tokenize input
    Lexer lexer = LoggerFactory is not null
      ? new Lexer(pattern, LoggerFactory.CreateLogger<Lexer>())
      : new Lexer(pattern);
    Tokens = lexer.Tokenize();

    ParsingLoggerMessages.ParsingPattern(Logger, pattern, null);
    if (Logger.IsEnabled(LogLevel.Trace))
    {
      ParsingLoggerMessages.DumpingTokens(Logger, Lexer.DumpTokens(Tokens), null);
    }

    // Parse tokens into AST

    List<SegmentSyntax> segments = ParsePattern();
    Syntax ast = new(segments);

    // Perform semantic validation using the SemanticValidator
    IReadOnlyList<SemanticError>? semanticErrors = SemanticValidator.Validate(ast);

    ParseResult<Syntax> result = new()
    {
      Value = ast,
      ParseErrors = ParseErrors,
      SemanticErrors = semanticErrors
    };

    if (result.Success && Logger.IsEnabled(LogLevel.Debug))
    {
      ParsingLoggerMessages.DumpingAst(Logger, DumpAst(ast), null);
    }

    return result;
  }

  /// <summary>
  /// Returns a diagnostic dump of a syntax tree for debugging.
  /// </summary>
  /// <param name="ast">The syntax tree to dump.</param>
  /// <returns>A formatted string showing the syntax tree structure.</returns>
  public static string DumpAst(Syntax ast)
  {
    ArgumentNullException.ThrowIfNull(ast);

    StringBuilder sb = new();
    sb.Append("→ ").Append(ast.Segments.Count).Append(" segments: ");

    // Build inline segment list
    List<string> segmentList = [];
    for (int i = 0; i < ast.Segments.Count; i++)
    {
      segmentList.Add(ast.Segments[i].ToString());
    }

    sb.AppendJoin(", ", segmentList);

    return sb.ToString();
  }

  private List<SegmentSyntax> ParsePattern()
  {
    List<SegmentSyntax> segments = [];

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
      RouteTokenType.LeftBrace => ParseParameter(),
      RouteTokenType.DoubleDash or RouteTokenType.SingleDash => ParseOption(),
      RouteTokenType.EndOfOptions => ParseEndOfOptions(),
      RouteTokenType.Identifier => ParseLiteral(),
      RouteTokenType.Invalid => ParseInvalidToken(),
      RouteTokenType.RightBrace => HandleUnexpectedRightBrace(),
      _ => HandleUnexpectedToken()
    };
  }

  private LiteralSyntax ParseLiteral()
  {
    Token token = Consume(RouteTokenType.Identifier, "Expected identifier");
    return new LiteralSyntax(token.Value)
    {
      Position = token.Position,
      Length = token.Length
    };
  }

  private LiteralSyntax ParseEndOfOptions()
  {
    Token token = Consume(RouteTokenType.EndOfOptions, "Expected '--'");

    // Create a literal segment for the -- separator
    return new LiteralSyntax("--")
    {
      Position = token.Position,
      Length = token.Length
    };
  }

  private ParameterSyntax ParseParameter()
  {
    Token leftBrace = Consume(RouteTokenType.LeftBrace, "Expected '{'");
    int startPos = leftBrace.Position;

    // Check for catch-all marker
    bool isCatchAll = false;
    if (Match(RouteTokenType.Asterisk))
    {
      isCatchAll = true;
    }

    // Parameter name
    Token nameToken = Consume(RouteTokenType.Identifier, "Expected parameter name");
    string paramName = nameToken.Value;

    // Validate identifier starts with letter or underscore
    if (!IsValidIdentifier(paramName))
    {
      AddParseError
      (
        new InvalidIdentifierError
        (
          nameToken.Position,
          nameToken.Length,
          paramName
        )
      );
    }

    // Optional marker
    bool isOptional = Match(RouteTokenType.Question);

    // Validate that catch-all and optional are not combined
    if (isCatchAll && isOptional)
    {
      AddParseError
      (
        new InvalidModifierCombinationError
        (
          startPos,
          nameToken.EndPosition - startPos,
          paramName
        )
      );
    }

    // Type constraint
    string? typeConstraint = null;
    if (Match(RouteTokenType.Colon))
    {
      Token typeToken = Consume(RouteTokenType.Identifier, "Expected type name after ':'");
      typeConstraint = typeToken.Value;

      // Type might also be optional (e.g., int?)
      if (Match(RouteTokenType.Question))
      {
        isOptional = true;
        typeConstraint += "?";
      }

      // Validate type constraint (NURU004)
      string baseType = typeConstraint.TrimEnd('?');
      if (!IsValidTypeConstraint(baseType))
      {
        AddParseError
        (
          new InvalidTypeConstraintError
          (
            typeToken.Position,
            typeToken.Length,
            baseType
          )
        );
      }
    }

    // Description
    string? description = null;
    if (Match(RouteTokenType.Pipe))
    {
      description = ConsumeDescription(stopAtRightBrace: true);
    }

    Token rightBrace = Consume(RouteTokenType.RightBrace, "Expected '}'");

    return new ParameterSyntax(paramName, isCatchAll, isOptional, false, typeConstraint, description)
    {
      Position = startPos,
      Length = rightBrace.EndPosition - startPos
    };
  }

  private OptionSyntax ParseOption()
  {
    Token optionToken = Current();
    bool isLong = optionToken.Type == RouteTokenType.DoubleDash;
    Advance(); // Consume the dash(es)

    int startPos = optionToken.Position;
    Token optionNameToken = Consume(RouteTokenType.Identifier, "Expected option name");

    // Parse option forms (long/short)
    (string? longForm, string? shortForm) = ParseOptionForms(isLong, optionNameToken);

    // Check for optional modifier (?)
    bool isOptional = Match(RouteTokenType.Question);

    // Parse description if present
    string? description = ParseOptionDescription();

    // Parse option parameter if present
    ParameterSyntax? parameter = ParseOptionParameter();

    int endPos = Previous().EndPosition;

    return new OptionSyntax(longForm, shortForm, description, parameter, isOptional)
    {
      Position = startPos,
      Length = endPos - startPos
    };
  }

  private (string? longForm, string? shortForm) ParseOptionForms(bool isLong, Token optionNameToken)
  {
    if (isLong)
    {
      string longForm = optionNameToken.Value;
      string? shortForm = null;

      // Check for short alias
      if (Match(RouteTokenType.Comma))
      {
        Consume(RouteTokenType.SingleDash, "Expected '-' after comma");
        Token shortToken = Consume(RouteTokenType.Identifier, "Expected short option name");
        shortForm = shortToken.Value;
      }

      return (longForm, shortForm);
    }
    else
    {
      string shortForm = optionNameToken.Value;

      // Validate single dash options should be single character
      if (shortForm.Length > 1)
      {
        AddParseError
        (
          new InvalidOptionFormatError
          (
            optionNameToken.Position - 1, // Include the dash
            optionNameToken.Length + 1,
            $"-{shortForm}")
          );
      }

      return (null, shortForm);
    }
  }

  private string? ParseOptionDescription()
  {
    if (Match(RouteTokenType.Pipe))
    {
      return ConsumeDescription(stopAtRightBrace: false);
    }

    return null;
  }

  private ParameterSyntax? ParseOptionParameter()
  {
    if (!Check(RouteTokenType.LeftBrace))
    {
      return null;
    }

    ParameterSyntax parameter = ParseParameter();

    // Validate that catch-all is not used in options
    if (parameter.IsCatchAll)
    {
      // Catch-all in options is invalid parameter syntax
      AddParseError
      (
        new InvalidParameterSyntaxError
        (
          parameter.Position,
          parameter.Length,
          $"{{*{parameter.Name}}}",
          parameter.Name
        )
      );
    }

    // Check for repeated modifier (*) after the parameter
    // e.g., --tag {value}* means the option can be repeated
    if (Match(RouteTokenType.Asterisk))
    {
      parameter = parameter with { IsRepeated = true };
    }

    return parameter;
  }

  private SegmentSyntax? ParseInvalidToken()
  {
    Token token = Advance();

    // Special handling for angle bracket syntax
    if (token.Value.StartsWith('<') && token.Value.EndsWith('>'))
    {
      string paramName = token.Value[1..^1]; // Remove < and >
      string suggestion = $"{{{paramName}}}";

      AddParseError
      (
        new InvalidParameterSyntaxError
        (
          token.Position,
          token.Length,
          token.Value,
          suggestion
        )
      );
    }
    else
    {
      AddParseError
      (
        new InvalidCharacterError
        (
          token.Position,
          token.Length,
          token.Value
        )
      );
    }

    return null; // Skip invalid tokens
  }

  private SegmentSyntax? HandleUnexpectedRightBrace()
  {
    Token token = Advance(); // Consume the right brace to avoid infinite loop
    AddParseError
    (
      new UnexpectedTokenError
      (
        token.Position,
        token.Length,
        "}",
        "Unexpected '}'"
      )
    );

    return null;
  }

  private SegmentSyntax? HandleUnexpectedToken()
  {
    Token token = Advance(); // Consume the unexpected token to avoid infinite loop
    // Unexpected token - truly unexpected case
    AddParseError
    (
      new InvalidCharacterError
      (
        token.Position,
        token.Length,
        token.Value
      )
    );

    return null;
  }

  // Helper methods for parsing

  private bool Match(params RouteTokenType[] types)
  {
    foreach (RouteTokenType type in types)
    {
      if (Check(type))
      {
        Advance();
        return true;
      }
    }

    return false;
  }

  private bool Check(RouteTokenType type)
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
    return CurrentIndex >= Tokens.Count || Peek().Type == RouteTokenType.EndOfInput;
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

  private Token Consume(RouteTokenType type, string message)
  {
    if (Check(type)) return Advance();
    Token token = Peek();
    AddParseError(new UnexpectedTokenError(token.Position, token.Length, token.Value, message));
    throw new ParseException(message);
  }

  private void Synchronize()
  {
    // Skip tokens until we find a likely start of a new segment
    while (!IsAtEnd())
    {
      Token token = Peek();
      if (token.Type is RouteTokenType.LeftBrace or RouteTokenType.DoubleDash or
          RouteTokenType.SingleDash or RouteTokenType.Identifier)
      {
        break;
      }

      Advance();
    }
  }

  private string ConsumeDescription(bool stopAtRightBrace)
  {
    List<string> description = [];

    while (!IsAtEnd())
    {
      Token token = Peek();

      // Stop conditions
      if (stopAtRightBrace && token.Type == RouteTokenType.RightBrace)
      {
        break;
      }

      if (!stopAtRightBrace)
      {
        // For option descriptions, stop at new segment indicators
        if (token.Type is RouteTokenType.DoubleDash or RouteTokenType.SingleDash or RouteTokenType.LeftBrace)
        {
          break;
        }
      }

      // Add token value to description
      if (token.Type == RouteTokenType.Identifier)
      {
        description.Add(token.Value);
        Advance();
      }
      else if (token.Type == RouteTokenType.EndOfInput)
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

  private void AddParseError(ParseError error)
  {
    ParseErrors ??= [];
    ParseErrors.Add(error);
  }

  private static bool IsBuiltInType(string type)
  {
    return type switch
    {
      "string" => true,
      "int" => true,
      "byte" => true,
      "sbyte" => true,
      "short" => true,
      "ushort" => true,
      "uint" => true,
      "ulong" => true,
      "float" => true,
      "char" => true,
      "long" => true,
      "double" => true,
      "decimal" => true,
      "bool" => true,
      "DateTime" => true,
      "Guid" => true,
      "TimeSpan" => true,
      "uri" or "Uri" => true,
      "fileinfo" or "FileInfo" => true,
      "directoryinfo" or "DirectoryInfo" => true,
      "ipaddress" or "IPAddress" => true,
      "dateonly" or "DateOnly" => true,
      "timeonly" or "TimeOnly" => true,
      _ => false
    };
  }

  private static bool IsValidTypeConstraint(string type)
  {
    // Accept known built-in types
    if (IsBuiltInType(type)) return true;

    // Accept any valid identifier format for custom types
    return IsValidIdentifierFormat(type);
  }

  private static bool IsValidIdentifierFormat(string identifier)
  {
    if (string.IsNullOrWhiteSpace(identifier))
    {
      return false;
    }

    // First character must be letter or underscore
    char first = identifier[0];
    if (!char.IsLetter(first) && first != '_')
    {
      return false;
    }

    // Remaining characters must be letters, digits, or underscores
    for (int i = 1; i < identifier.Length; i++)
    {
      char c = identifier[i];
      if (!char.IsLetterOrDigit(c) && c != '_')
      {
        return false;
      }
    }

    return true;
  }

  private static bool IsValidIdentifier(string identifier)
  {
    if (string.IsNullOrEmpty(identifier))
    {
      return false;
    }

    // First character must be letter or underscore
    char first = identifier[0];
    return char.IsLetter(first) || first == '_';
  }

}
