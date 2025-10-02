#!/usr/bin/dotnet --

return await RunTests<InvalidDoubleDashesTests>();

[TestTag("Lexer")]
public class InvalidDoubleDashesTests
{
  // Double dashes embedded within identifiers are invalid
  [Input("test--case")]
  [Input("foo--bar--baz")]
  [Input("my--option")]
  [Input("a--b")]
  public static async Task Should_reject_double_dashes_within_identifiers(string pattern)
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
