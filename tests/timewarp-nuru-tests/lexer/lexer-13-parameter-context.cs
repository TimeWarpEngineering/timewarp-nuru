#!/usr/bin/dotnet --

return await RunTests<ParameterContextTests>();

[TestTag("Lexer")]
[ClearRunfileCache]
public class ParameterContextTests
{
  /// <summary>
  /// Test 1: Simple parameter inside braces
  /// Baseline test validating basic parameter tokenization
  /// Pattern: {name}
  /// </summary>
  public static async Task Should_tokenize_simple_parameter()
  {
    string pattern = "{name}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    tokens.Count.ShouldBe(4);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[0].Value.ShouldBe("{");
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe("name");
    tokens[2].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[2].Value.ShouldBe("}");
    tokens[3].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 2: Typed parameter with colon separator
  /// Pattern: {count:int}
  /// Validates type constraint syntax is tokenized correctly
  /// </summary>
  public static async Task Should_tokenize_typed_parameter()
  {
    string pattern = "{count:int}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    tokens.Count.ShouldBe(6);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe("count");
    tokens[2].Type.ShouldBe(RouteTokenType.Colon);
    tokens[2].Value.ShouldBe(":");
    tokens[3].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[3].Value.ShouldBe("int");
    tokens[4].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[5].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 3: Optional parameter with question mark modifier
  /// Pattern: {value?}
  /// Validates optional marker is tokenized as Question token
  /// </summary>
  public static async Task Should_tokenize_optional_parameter()
  {
    string pattern = "{value?}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    tokens.Count.ShouldBe(5);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe("value");
    tokens[2].Type.ShouldBe(RouteTokenType.Question);
    tokens[2].Value.ShouldBe("?");
    tokens[3].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[4].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 4: Catchall parameter with asterisk prefix
  /// Pattern: {*args}
  /// Validates catchall syntax with asterisk token
  /// </summary>
  public static async Task Should_tokenize_catchall_parameter()
  {
    string pattern = "{*args}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    tokens.Count.ShouldBe(5);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.Asterisk);
    tokens[1].Value.ShouldBe("*");
    tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[2].Value.ShouldBe("args");
    tokens[3].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[4].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 5: Invalid double-dash inside parameter name
  /// Pattern: {invalid--name}
  /// Double-dash inside identifier makes entire identifier invalid
  /// Lexer produces single Invalid token for the whole malformed identifier
  /// </summary>
  public static async Task Should_detect_invalid_double_dash_in_parameter()
  {
    string pattern = "{invalid--name}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Should tokenize: { [Invalid]invalid--name }
    // The entire identifier is invalid due to embedded --
    tokens.Count.ShouldBe(4);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.Invalid);
    tokens[1].Value.ShouldBe("invalid--name");  // Entire thing is invalid
    tokens[2].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[3].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 6: Combined type constraint and optional modifier
  /// Pattern: {seconds:int?}
  /// Validates multiple modifiers can be tokenized together
  /// Real-world usage: optional typed parameter
  /// </summary>
  public static async Task Should_tokenize_combined_type_and_optional()
  {
    string pattern = "{seconds:int?}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Should tokenize: { seconds : int ? }
    tokens.Count.ShouldBe(7);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe("seconds");
    tokens[2].Type.ShouldBe(RouteTokenType.Colon);
    tokens[3].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[3].Value.ShouldBe("int");
    tokens[4].Type.ShouldBe(RouteTokenType.Question);  // Optional marker
    tokens[5].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[6].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 7: Enum-like values with pipe separators
  /// Pattern: {mode:dev|staging|prod}
  /// Lexer tokenizes pipes normally - parser interprets as enum values
  /// Fascinating test: pipes inside parameter definition!
  /// </summary>
  public static async Task Should_tokenize_enum_values_with_pipes()
  {
    string pattern = "{mode:dev|staging|prod}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Lexer doesn't know about enum syntax - just tokenizes pipes normally
    // Parser will interpret this as: mode parameter with type "dev|staging|prod"
    tokens.Count.ShouldBe(10);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe("mode");
    tokens[2].Type.ShouldBe(RouteTokenType.Colon);
    tokens[3].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[3].Value.ShouldBe("dev");
    tokens[4].Type.ShouldBe(RouteTokenType.Pipe);  // First pipe
    tokens[5].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[5].Value.ShouldBe("staging");
    tokens[6].Type.ShouldBe(RouteTokenType.Pipe);  // Second pipe
    tokens[7].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[7].Value.ShouldBe("prod");
    tokens[8].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[9].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 8: Nested/doubled braces (malformed)
  /// Pattern: {{name}}
  /// Each brace is tokenized individually
  /// Parser will catch nesting error - lexer just produces tokens
  /// </summary>
  public static async Task Should_detect_nested_braces()
  {
    string pattern = "{{name}}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Lexer tokenizes each brace separately
    tokens.Count.ShouldBe(6);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);   // First {
    tokens[1].Type.ShouldBe(RouteTokenType.LeftBrace);   // Second {
    tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[2].Value.ShouldBe("name");
    tokens[3].Type.ShouldBe(RouteTokenType.RightBrace);  // First }
    tokens[4].Type.ShouldBe(RouteTokenType.RightBrace);  // Second }
    tokens[5].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 9: Unclosed brace (missing closing brace)
  /// Pattern: {name
  /// Lexer doesn't require balanced braces - parser catches this
  /// Validates lexer continues tokenizing even with malformed input
  /// </summary>
  public static async Task Should_detect_unclosed_brace()
  {
    string pattern = "{name";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Lexer produces tokens, missing closing brace
    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe("name");
    tokens[2].Type.ShouldBe(RouteTokenType.EndOfInput);  // No closing brace

    // Verify no closing brace token exists
    tokens.Any(t => t.Type == RouteTokenType.RightBrace).ShouldBeFalse("Should not have closing brace");

    await Task.CompletedTask;
  }
}
