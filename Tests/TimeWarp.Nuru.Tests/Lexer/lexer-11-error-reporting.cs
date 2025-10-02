#!/usr/bin/dotnet --

return await RunTests<ErrorReportingTests>();

[TestTag("Lexer")]
[ClearRunfileCache]
public class ErrorReportingTests
{
  /// <summary>
  /// Test 1: Invalid tokens should include accurate position information
  /// Position tracking is essential for providing helpful error messages to users
  /// </summary>
  public static async Task Should_include_position_in_invalid_token()
  {
    // Pattern with @ at position 4 (0-indexed: c=0, m=1, d=2, space=3, @=4)
    string pattern = "cmd @ option";
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Find the Invalid token
    Token? invalidToken = tokens.FirstOrDefault(t => t.Type == TokenType.Invalid);
    invalidToken.ShouldNotBeNull("Expected to find an Invalid token");

    // Verify position information
    invalidToken.Type.ShouldBe(TokenType.Invalid);
    invalidToken.Value.ShouldBe("@");
    invalidToken.Position.ShouldBe(4, "@ character is at position 4");
    invalidToken.Length.ShouldBe(1, "@ is a single character");
    invalidToken.EndPosition.ShouldBe(5, "EndPosition = Position + Length");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 2: Invalid token ToString() should provide diagnostic information
  /// The string representation helps developers debug tokenization issues
  /// </summary>
  public static async Task Should_have_descriptive_toString_for_invalid_token()
  {
    // Pattern with # at position 5
    string pattern = "test # value";
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    Token? invalidToken = tokens.FirstOrDefault(t => t.Type == TokenType.Invalid);
    invalidToken.ShouldNotBeNull();

    string diagnosticString = invalidToken.ToString();

    // Verify diagnostic string contains key information
    diagnosticString.ShouldContain("Invalid");  // Should identify token type
    diagnosticString.ShouldContain("#");        // Should show the invalid character
    diagnosticString.ShouldContain("5");        // Should include position

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 3: Lexer should handle multiple invalid tokens in single pattern
  /// Validates lexer continues tokenizing after encountering errors
  /// </summary>
  public static async Task Should_tokenize_multiple_invalid_tokens()
  {
    // Pattern: "cmd @ param # value"
    // Positions: c=0,m=1,d=2, =3,@=4, =5,p=6,a=7,r=8,a=9,m=10, =11,#=12
    string pattern = "cmd @ param # value";
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Should have two Invalid tokens
    Token[] invalidTokens = [.. tokens.Where(t => t.Type == TokenType.Invalid)];
    invalidTokens.Length.ShouldBe(2, "Should find two invalid tokens");

    // First invalid token: @
    invalidTokens[0].Value.ShouldBe("@");
    invalidTokens[0].Position.ShouldBe(4);

    // Second invalid token: #
    invalidTokens[1].Value.ShouldBe("#");
    invalidTokens[1].Position.ShouldBe(12);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 4: Invalid token at start of pattern should be detected
  /// Edge case: error occurs at position 0
  /// </summary>
  public static async Task Should_detect_invalid_token_at_start()
  {
    // Pattern starts with invalid character
    string pattern = "@cmd {param}";
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // First token should be the Invalid token
    tokens.Count.ShouldBeGreaterThan(0);
    tokens[0].Type.ShouldBe(TokenType.Invalid);
    tokens[0].Value.ShouldBe("@");
    tokens[0].Position.ShouldBe(0, "Invalid token at start has position 0");

    // Should still tokenize remaining valid parts
    tokens.Any(t => t.Type == TokenType.Identifier && t.Value == "cmd").ShouldBeTrue();
    tokens.Any(t => t.Type == TokenType.LeftBrace).ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 5: Invalid token at end of pattern should be detected
  /// Edge case: error occurs at the last position before EndOfInput
  /// </summary>
  public static async Task Should_detect_invalid_token_at_end()
  {
    // Pattern: "cmd {param} @"
    // Last character is invalid
    string pattern = "cmd {param} @";
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Find Invalid token
    Token? invalidToken = tokens.FirstOrDefault(t => t.Type == TokenType.Invalid);
    invalidToken.ShouldNotBeNull();
    invalidToken.Value.ShouldBe("@");
    invalidToken.Position.ShouldBe(12, "@ is at position 12");

    // Invalid token should come before EndOfInput
    // Last token should be EndOfInput
    tokens[tokens.Count - 1].Type.ShouldBe(TokenType.EndOfInput);
    // Second to last should be the invalid token
    tokens[tokens.Count - 2].ShouldBe(invalidToken);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 6: Invalid token in middle of pattern with valid tokens before and after
  /// Validates lexer properly recovers and continues tokenizing after error
  /// </summary>
  public static async Task Should_detect_invalid_token_in_middle()
  {
    // Pattern: "deploy {env} @ --force"
    // Valid tokens before and after the invalid @
    string pattern = "deploy {env} @ --force";
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Should have valid tokens before invalid
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("deploy");
    tokens[1].Type.ShouldBe(TokenType.LeftBrace);

    // Invalid token in middle
    Token? invalidToken = tokens.FirstOrDefault(t => t.Type == TokenType.Invalid);
    invalidToken.ShouldNotBeNull();
    invalidToken.Value.ShouldBe("@");
    invalidToken.Position.ShouldBe(13);

    // Should have valid tokens after invalid
    tokens.Any(t => t.Type == TokenType.DoubleDash).ShouldBeTrue("Should find -- after error");
    tokens.Any(t => t.Type == TokenType.Identifier && t.Value == "force").ShouldBeTrue("Should find 'force' after error");

    await Task.CompletedTask;
  }
}
