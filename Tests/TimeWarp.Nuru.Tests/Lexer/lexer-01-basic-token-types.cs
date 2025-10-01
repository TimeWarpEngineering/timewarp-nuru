#!/usr/bin/dotnet --

return await RunTests<BasicTokenTypesTests>();

[TestTag("Lexer")]
public class BasicTokenTypesTests
{

  [Input("status")]
  [Input("version")]
  [Input("help")]
  [Input("build")]

  public static async Task Should_tokenize_literal_words(string pattern)
  {
    // Arrange
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2); // token + EndOfInput
    tokens[0].Type.ShouldBe(TokenType.Identifier); // Lexer returns Identifier, not Literal
    tokens[0].Value.ShouldBe(pattern);
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

}

