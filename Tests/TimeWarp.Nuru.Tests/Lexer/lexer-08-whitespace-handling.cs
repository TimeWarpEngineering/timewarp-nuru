#!/usr/bin/dotnet --

return await RunTests<WhitespaceHandlingTests>();

[TestTag("Lexer")]
[ClearRunfileCache]
public class WhitespaceHandlingTests
{
  // All whitespace variations should produce identical token sequences
  [Input("git log")]          // single space
  [Input("git   log")]         // multiple spaces
  [Input("git\tlog")]          // tab character
  [Input("  git log")]         // leading whitespace
  [Input("git log  ")]         // trailing whitespace
  public static async Task Should_produce_identical_tokens_regardless_of_whitespace(string pattern)
  {
    // Arrange
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert - whitespace quantity and type don't affect tokenization
    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("git");
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe("log");
    tokens[2].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_whitespace_around_special_characters()
  {
    // Arrange
    string pattern = "{ param }";
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert - whitespace separates special chars from identifiers
    tokens.Count.ShouldBe(4);
    tokens[0].Type.ShouldBe(TokenType.LeftBrace);
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe("param");
    tokens[2].Type.ShouldBe(TokenType.RightBrace);
    tokens[3].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_no_whitespace_between_special_characters()
  {
    // Arrange
    string pattern = "{}";
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert - special chars are self-delimiting
    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(TokenType.LeftBrace);
    tokens[1].Type.ShouldBe(TokenType.RightBrace);
    tokens[2].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
