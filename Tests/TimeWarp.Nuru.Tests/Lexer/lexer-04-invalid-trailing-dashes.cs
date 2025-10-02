#!/usr/bin/dotnet --

return await RunTests<InvalidTrailingDashesTests>();

[TestTag("Lexer")]
public class InvalidTrailingDashesTests
{
  // Trailing dashes indicate incomplete/malformed identifiers
  [Input("test-")]
  [Input("test--")]
  [Input("foo---")]
  [Input("my-command-")]
  public static async Task Should_reject_trailing_dashes(string pattern)
  {
    // Arrange
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2); // Invalid + EndOfInput
    tokens[0].Type.ShouldBe(TokenType.Invalid);
    tokens[0].Value.ShouldBe(pattern);
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
