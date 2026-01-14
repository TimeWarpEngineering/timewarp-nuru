#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Lexer")]
public class EdgeCasesTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EdgeCasesTests>();

  /// <summary>
  /// Test 1: Empty string should produce only EndOfInput token
  /// This validates lexer handles minimal input correctly
  /// </summary>
  public static async Task Should_tokenize_empty_string()
  {
    string pattern = "";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    tokens.Count.ShouldBe(1);
    tokens[0].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 2: Whitespace-only strings should produce only EndOfInput
  /// Tests that all whitespace variations are treated equivalently
  /// </summary>
  [Input("   ")]        // spaces only
  [Input("\t\t")]       // tabs only
  [Input(" \t \t ")]    // mixed spaces and tabs
  [Input("\n")]         // newline
  [Input(" \n\t ")]     // mixed whitespace with newline
  public static async Task Should_tokenize_only_whitespace(string pattern)
  {
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    tokens.Count.ShouldBe(1);
    tokens[0].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 3: Single character tokens should be recognized correctly
  /// Validates minimal token patterns work
  /// </summary>
  public static async Task Should_tokenize_single_character_tokens()
  {
    // Test pattern with all single-char special tokens separated by spaces
    string pattern = "{ } : ? * |";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    tokens.Count.ShouldBe(7);  // 6 tokens + EndOfInput
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[2].Type.ShouldBe(RouteTokenType.Colon);
    tokens[3].Type.ShouldBe(RouteTokenType.Question);
    tokens[4].Type.ShouldBe(RouteTokenType.Asterisk);
    tokens[5].Type.ShouldBe(RouteTokenType.Pipe);
    tokens[6].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 4: Very long identifiers should be tokenized correctly
  /// Tests lexer can handle identifiers beyond typical lengths
  /// Real-world example: AWS resource names can be 100+ chars
  /// </summary>
  public static async Task Should_tokenize_very_long_identifier()
  {
    // Create 150-character identifier (exceeds most buffer sizes)
    string longIdentifier = new('a', 150);
    string pattern = $"cmd {longIdentifier}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[0].Value.ShouldBe("cmd");
    tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[1].Value.ShouldBe(longIdentifier);
    tokens[1].Value.Length.ShouldBe(150);
    tokens[2].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 5: Adjacent special characters should tokenize correctly
  /// Validates state machine handles rapid transitions between special chars
  /// </summary>
  public static async Task Should_tokenize_adjacent_special_characters()
  {
    // Test various combinations of adjacent special characters
    string pattern = "{}{?}{*}{:}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Should produce: { } { ? } { * } { : } EndOfInput = 12 tokens
    tokens.Count.ShouldBe(12);
    tokens[0].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[1].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[2].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[3].Type.ShouldBe(RouteTokenType.Question);
    tokens[4].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[5].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[6].Type.ShouldBe(RouteTokenType.Asterisk);
    tokens[7].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[8].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[9].Type.ShouldBe(RouteTokenType.Colon);
    tokens[10].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[11].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 6: Mixed valid and invalid patterns should tokenize with Invalid tokens
  /// Validates lexer continues after encountering invalid characters
  /// </summary>
  public static async Task Should_handle_mixed_valid_invalid_patterns()
  {
    // Pattern with valid tokens followed by invalid character (@) then more valid tokens
    string pattern = "cmd {param} @ --flag";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Should tokenize: cmd, {, param, }, Invalid(@), --, flag, EndOfInput
    tokens.Count.ShouldBe(8);
    tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[0].Value.ShouldBe("cmd");
    tokens[1].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[2].Value.ShouldBe("param");
    tokens[3].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[4].Type.ShouldBe(RouteTokenType.Invalid);
    tokens[4].Value.ShouldBe("@");
    tokens[5].Type.ShouldBe(RouteTokenType.DoubleDash);
    tokens[6].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[6].Value.ShouldBe("flag");
    tokens[7].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 7: Unicode characters in identifiers
  /// Modern CLIs should support international characters in command names
  /// Examples: Chinese (部署), Greek (αβγ), French accents (café)
  /// </summary>
  public static async Task Should_tokenize_unicode_identifiers()
  {
    // Test various unicode identifier scenarios
    string pattern = "部署 {名称} αβγ {δ} café";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Should recognize unicode characters as valid identifiers
    tokens.Count.ShouldBe(10);
    tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[0].Value.ShouldBe("部署");  // Chinese: "deploy"
    tokens[1].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[2].Value.ShouldBe("名称");  // Chinese: "name"
    tokens[3].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[4].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[4].Value.ShouldBe("αβγ");   // Greek letters
    tokens[5].Type.ShouldBe(RouteTokenType.LeftBrace);
    tokens[6].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[6].Value.ShouldBe("δ");     // Greek delta
    tokens[7].Type.ShouldBe(RouteTokenType.RightBrace);
    tokens[8].Type.ShouldBe(RouteTokenType.Identifier);
    tokens[8].Value.ShouldBe("café");  // French accent
    tokens[9].Type.ShouldBe(RouteTokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
