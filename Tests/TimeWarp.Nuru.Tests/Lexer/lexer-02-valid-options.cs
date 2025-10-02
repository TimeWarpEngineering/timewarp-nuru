#!/usr/bin/dotnet --

return await RunTests<ValidOptionsTests>();

[TestTag("Lexer")]
public class ValidOptionsTests
{
  // Long options (with compound identifiers)
  [Input("--dry-run", "dry-run")]
  [Input("--no-edit", "no-edit")]
  [Input("--save-dev", "save-dev")]
  [Input("--v", "v")]
  public static async Task Should_tokenize_long_options(string pattern, string expectedOption)
  {
    // Arrange
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(3); // DoubleDash + Identifier + EndOfInput
    tokens[0].Type.ShouldBe(TokenType.DoubleDash);
    tokens[0].Value.ShouldBe("--");
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe(expectedOption);
    tokens[2].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  // Short options (single character)
  [Input("-h", "h")]
  [Input("-v", "v")]
  [Input("-x", "x")]
  public static async Task Should_tokenize_short_options(string pattern, string expectedOption)
  {
    // Arrange
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(3); // SingleDash + Identifier + EndOfInput
    tokens[0].Type.ShouldBe(TokenType.SingleDash);
    tokens[0].Value.ShouldBe("-");
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe(expectedOption);
    tokens[2].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
