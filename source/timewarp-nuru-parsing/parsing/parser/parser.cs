namespace TimeWarp.Nuru;

/// <summary>
/// Recursive descent parser for route patterns.
/// </summary>
/// <remarks>
/// This partial class is split across multiple files:
/// <list type="bullet">
///   <item><description><c>parser.cs</c> - Main parsing logic and AST construction</description></item>
///   <item><description><c>parser.segments.cs</c> - Segment parsing methods (literals, parameters, options)</description></item>
///   <item><description><c>parser.validation.cs</c> - Type and identifier validation</description></item>
///   <item><description><c>parser.navigation.cs</c> - Token navigation helpers</description></item>
/// </list>
/// </remarks>
internal sealed partial class Parser : IParser
{
#if !ANALYZER_BUILD
  private readonly ILogger<Parser> Logger;
  private readonly ILoggerFactory? LoggerFactory;
#endif
  private IReadOnlyList<Token> Tokens = [];
  private int CurrentIndex;
  private List<ParseError>? ParseErrors;

#if !ANALYZER_BUILD
  public Parser() : this(null, null) { }

  public Parser(ILogger<Parser>? logger = null, ILoggerFactory? loggerFactory = null)
  {
    Logger = logger ?? NullLogger<Parser>.Instance;
    LoggerFactory = loggerFactory;
  }
#else
  public Parser() { }
#endif

  /// <inheritdoc />
  public ParseResult<Syntax> Parse(string pattern)
  {
    ArgumentNullException.ThrowIfNull(pattern);

    // Reset state
    CurrentIndex = 0;
    ParseErrors = null;

    // Tokenize input
#if !ANALYZER_BUILD
    Lexer lexer = LoggerFactory is not null
      ? new Lexer(pattern, LoggerFactory.CreateLogger<Lexer>())
      : new Lexer(pattern);
#else
    Lexer lexer = new(pattern);
#endif
    Tokens = lexer.Tokenize();

#if !ANALYZER_BUILD
    ParsingLoggerMessages.ParsingPattern(Logger, pattern, null);
    if (Logger.IsEnabled(LogLevel.Trace))
    {
      ParsingLoggerMessages.DumpingTokens(Logger, Lexer.DumpTokens(Tokens), null);
    }
#endif

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

#if !ANALYZER_BUILD
    if (result.Success && Logger.IsEnabled(LogLevel.Debug))
    {
      ParsingLoggerMessages.DumpingAst(Logger, DumpAst(ast), null);
    }
#endif

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
}
