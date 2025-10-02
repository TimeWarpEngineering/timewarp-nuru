#!/usr/bin/dotnet --

return await RunTests<BasicTokenTypesTests>();

[TestTag("Lexer")]
public class BasicTokenTypesTests
{
  // Plain identifiers
  [Input("status")]
  [Input("version")]
  [Input("help")]
  [Input("build")]
  public static async Task Should_tokenize_plain_identifiers(string pattern)
    => await TokenizeSingleIdentifier(pattern);

  // Compound identifiers with dashes
  [Input("dry-run")]
  [Input("no-edit")]
  [Input("save-dev")]
  [Input("my-long-command-name")]
  public static async Task Should_tokenize_compound_identifiers(string pattern)
    => await TokenizeSingleIdentifier(pattern);

  // Special characters
  [Input("{")]
  public static async Task Should_tokenize_left_brace(string pattern)
  {
    // Arrange
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.LeftBrace);
    tokens[0].Value.ShouldBe("{");
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input("}")]
  public static async Task Should_tokenize_right_brace(string pattern)
  {
    // Arrange
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.RightBrace);
    tokens[0].Value.ShouldBe("}");
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  [Input(":")]
  public static async Task Should_tokenize_colon(string pattern)
  {
    // Arrange
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.Colon);
    tokens[0].Value.ShouldBe(":");
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  private static async Task TokenizeSingleIdentifier(string pattern)
  {
    // Arrange
    RoutePatternLexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2); // identifier + EndOfInput
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe(pattern);
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

}

