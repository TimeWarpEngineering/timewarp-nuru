#!/usr/bin/dotnet --

return await RunTests<TokenPositionTests>();

[TestTag("Lexer")]
[ClearRunfileCache]
public class TokenPositionTests
{
  /// <summary>
  /// Test 1: Token start positions should accurately reflect location in input string
  /// Validates Position property for all token types
  /// Pattern: "cmd {name} --flag" has tokens at positions 0, 4, 5, 9, 11, 13
  /// </summary>
  public static async Task Should_track_token_start_positions_correctly()
  {
    // Pattern: "cmd {name} --flag"
    // Positions: 0123456789012345678
    string pattern = "cmd {name} --flag";
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Verify each token's starting position
    tokens.Count.ShouldBe(7);

    // "cmd" starts at position 0
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("cmd");
    tokens[0].Position.ShouldBe(0);

    // "{" starts at position 4 (after "cmd ")
    tokens[1].Type.ShouldBe(TokenType.LeftBrace);
    tokens[1].Position.ShouldBe(4);

    // "name" starts at position 5
    tokens[2].Type.ShouldBe(TokenType.Identifier);
    tokens[2].Value.ShouldBe("name");
    tokens[2].Position.ShouldBe(5);

    // "}" starts at position 9
    tokens[3].Type.ShouldBe(TokenType.RightBrace);
    tokens[3].Position.ShouldBe(9);

    // "--" starts at position 11 (after "} ")
    tokens[4].Type.ShouldBe(TokenType.DoubleDash);
    tokens[4].Position.ShouldBe(11);

    // "flag" starts at position 13
    tokens[5].Type.ShouldBe(TokenType.Identifier);
    tokens[5].Value.ShouldBe("flag");
    tokens[5].Position.ShouldBe(13);

    // EndOfInput starts at position 17 (end of string)
    tokens[6].Type.ShouldBe(TokenType.EndOfInput);
    tokens[6].Position.ShouldBe(17);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 2: Token end positions should equal Position + Length
  /// Validates EndPosition computed property works correctly
  /// EndPosition = Position + Length for all tokens
  /// </summary>
  public static async Task Should_track_token_end_positions_correctly()
  {
    string pattern = "cmd {name}";
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Verify EndPosition = Position + Length for each token
    foreach (Token token in tokens)
    {
      int expectedEndPosition = token.Position + token.Length;
      token.EndPosition.ShouldBe(expectedEndPosition,
        $"Token [{token.Type}] '{token.Value}' EndPosition mismatch");
    }

    // Verify specific examples
    // "cmd" → Position 0, Length 3, EndPosition 3
    tokens[0].Position.ShouldBe(0);
    tokens[0].Length.ShouldBe(3);
    tokens[0].EndPosition.ShouldBe(3);

    // "{" → Position 4, Length 1, EndPosition 5
    tokens[1].Position.ShouldBe(4);
    tokens[1].Length.ShouldBe(1);
    tokens[1].EndPosition.ShouldBe(5);

    // "name" → Position 5, Length 4, EndPosition 9
    tokens[2].Position.ShouldBe(5);
    tokens[2].Length.ShouldBe(4);
    tokens[2].EndPosition.ShouldBe(9);

    // "}" → Position 9, Length 1, EndPosition 10
    tokens[3].Position.ShouldBe(9);
    tokens[3].Length.ShouldBe(1);
    tokens[3].EndPosition.ShouldBe(10);

    // EndOfInput → Position 10, Length 0, EndPosition 10 (special case)
    tokens[4].Type.ShouldBe(TokenType.EndOfInput);
    tokens[4].Position.ShouldBe(10);
    tokens[4].Length.ShouldBe(0);
    tokens[4].EndPosition.ShouldBe(10);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 3: Token length should match value length
  /// Validates Length property is consistent with Value property
  /// Special case: EndOfInput has Length 0 and Value ""
  /// </summary>
  public static async Task Should_track_token_length_matches_value()
  {
    string pattern = "test {parameter:int} --verbose";
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Verify Length = Value.Length for all tokens
    foreach (Token token in tokens)
    {
      token.Length.ShouldBe(token.Value.Length,
        $"Token [{token.Type}] '{token.Value}' Length mismatch");
    }

    // Verify specific examples
    // "test" → Length 4
    tokens[0].Value.ShouldBe("test");
    tokens[0].Length.ShouldBe(4);

    // "parameter" → Length 9
    tokens[2].Value.ShouldBe("parameter");
    tokens[2].Length.ShouldBe(9);

    // ":" → Length 1
    tokens[3].Value.ShouldBe(":");
    tokens[3].Length.ShouldBe(1);

    // "int" → Length 3
    tokens[4].Value.ShouldBe("int");
    tokens[4].Length.ShouldBe(3);

    // "--" → Length 2
    tokens[6].Value.ShouldBe("--");
    tokens[6].Length.ShouldBe(2);

    // "verbose" → Length 7
    tokens[7].Value.ShouldBe("verbose");
    tokens[7].Length.ShouldBe(7);

    // EndOfInput → Length 0, Value ""
    Token endToken = tokens[tokens.Count - 1];
    endToken.Type.ShouldBe(TokenType.EndOfInput);
    endToken.Value.ShouldBe("");
    endToken.Length.ShouldBe(0);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 4: Position tracking should account for whitespace
  /// Whitespace is skipped in tokenization but affects token positions
  /// Pattern with multiple spaces: "cmd   {name}  --flag"
  /// </summary>
  public static async Task Should_track_positions_across_whitespace()
  {
    // Pattern with varying amounts of whitespace
    string pattern = "cmd   {name}  --flag";
    // Positions:     012   3456789  10111213141516171819
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    tokens.Count.ShouldBe(7);

    // "cmd" at position 0
    tokens[0].Position.ShouldBe(0);
    tokens[0].Value.ShouldBe("cmd");

    // "{" at position 6 (after 3 spaces: positions 3, 4, 5)
    tokens[1].Position.ShouldBe(6);
    tokens[1].Value.ShouldBe("{");

    // "name" at position 7
    tokens[2].Position.ShouldBe(7);
    tokens[2].Value.ShouldBe("name");

    // "}" at position 11
    tokens[3].Position.ShouldBe(11);
    tokens[3].Value.ShouldBe("}");

    // "--" at position 14 (after 2 spaces: positions 12, 13)
    tokens[4].Position.ShouldBe(14);
    tokens[4].Value.ShouldBe("--");

    // "flag" at position 16
    tokens[5].Position.ShouldBe(16);
    tokens[5].Value.ShouldBe("flag");

    // EndOfInput at position 20
    tokens[6].Position.ShouldBe(20);
    tokens[6].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 5: Multi-character token position tracking
  /// Validates DoubleDash, EndOfOptions, and multi-char identifiers
  /// Pattern: "git log --" tests both DoubleDash and EndOfOptions
  /// </summary>
  public static async Task Should_track_multi_char_token_positions()
  {
    string pattern = "git log --";
    // Positions:     012 3456 78910
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    tokens.Count.ShouldBe(4);

    // "git" → Position 0, Length 3
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("git");
    tokens[0].Position.ShouldBe(0);
    tokens[0].Length.ShouldBe(3);
    tokens[0].EndPosition.ShouldBe(3);

    // "log" → Position 4, Length 3
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe("log");
    tokens[1].Position.ShouldBe(4);
    tokens[1].Length.ShouldBe(3);
    tokens[1].EndPosition.ShouldBe(7);

    // "--" (EndOfOptions) → Position 8, Length 2
    tokens[2].Type.ShouldBe(TokenType.EndOfOptions);
    tokens[2].Value.ShouldBe("--");
    tokens[2].Position.ShouldBe(8);
    tokens[2].Length.ShouldBe(2);
    tokens[2].EndPosition.ShouldBe(10);

    // EndOfInput → Position 10, Length 0
    tokens[3].Type.ShouldBe(TokenType.EndOfInput);
    tokens[3].Position.ShouldBe(10);
    tokens[3].Length.ShouldBe(0);
    tokens[3].EndPosition.ShouldBe(10);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test 6: Invalid token position tracking
  /// Validates error reporting can point to exact location of invalid characters
  /// Multiple invalid tokens at different positions
  /// </summary>
  public static async Task Should_track_invalid_token_positions()
  {
    string pattern = "cmd @ --flag # value";
    // Positions:     012 3 4 567891011 12 131415161718192021
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Find all Invalid tokens
    Token[] invalidTokens = [.. tokens.Where(t => t.Type == TokenType.Invalid)];
    invalidTokens.Length.ShouldBe(2, "Should find two invalid tokens");

    // First invalid token: @ at position 4
    invalidTokens[0].Value.ShouldBe("@");
    invalidTokens[0].Position.ShouldBe(4);
    invalidTokens[0].Length.ShouldBe(1);
    invalidTokens[0].EndPosition.ShouldBe(5);

    // Second invalid token: # at position 13
    invalidTokens[1].Value.ShouldBe("#");
    invalidTokens[1].Position.ShouldBe(13);
    invalidTokens[1].Length.ShouldBe(1);
    invalidTokens[1].EndPosition.ShouldBe(14);

    // Verify positions are accurate for error reporting
    // @ is at index 4 in "cmd @ --flag # value"
    pattern[4].ShouldBe('@');
    // # is at index 13
    pattern[13].ShouldBe('#');

    await Task.CompletedTask;
  }
}
