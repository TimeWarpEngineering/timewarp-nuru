#!/usr/bin/dotnet --

return await RunTests<MultiCharShortOptionsTests>();

[TestTag("Lexer")]
[ClearRunfileCache]
public class MultiCharShortOptionsTests
{
  // Multi-character short options (real-world patterns like dotnet -bl)
  [Input("-test", "test")]
  [Input("-bl", "bl")]
  [Input("-verbosity", "verbosity")]
  [Input("-abc", "abc")]
  public static async Task Should_tokenize_multi_char_short_options(string pattern, string expectedIdentifier)
  {
    // Arrange
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(3); // SingleDash + Identifier + EndOfInput
    tokens[0].Type.ShouldBe(TokenType.SingleDash);
    tokens[0].Value.ShouldBe("-");
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe(expectedIdentifier);
    tokens[2].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
