#!/usr/bin/dotnet --

return await RunTests<InvalidAngleBracketsTests>();

[TestTag("Lexer")]
[ClearRunfileCache]
public class InvalidAngleBracketsTests
{
  public static async Task Should_reject_angle_brackets_after_identifier()
  {
    // Arrange
    string pattern = "test<param>";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("test");
    tokens[1].Type.ShouldBe(TokenType.Invalid);
    tokens[2].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_angle_brackets_at_start()
  {
    // Arrange
    string pattern = "<param>";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.Invalid);
    tokens[1].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_accept_curly_braces()
  {
    // Arrange
    string pattern = "{param}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert - positive test: ensure valid syntax still works
    tokens.Count.ShouldBe(4);
    tokens[0].Type.ShouldBe(TokenType.LeftBrace);
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe("param");
    tokens[2].Type.ShouldBe(TokenType.RightBrace);
    tokens[3].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_mixed_bracket_syntax()
  {
    // Arrange
    string pattern = "{param>";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(4);
    tokens[0].Type.ShouldBe(TokenType.LeftBrace);
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe("param");
    tokens[2].Type.ShouldBe(TokenType.Invalid);
    tokens[3].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
