#!/usr/bin/dotnet --

return await RunTests<EndOfOptionsTests>();

[TestTag("Lexer")]
[ClearRunfileCache]
public class EndOfOptionsTests
{
  public static async Task Should_tokenize_end_of_options_after_commands()
  {
    // Arrange
    string pattern = "git log --";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(4);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("git");
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe("log");
    tokens[2].Type.ShouldBe(TokenType.EndOfOptions);
    tokens[2].Value.ShouldBe("--");
    tokens[3].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_tokenize_end_of_options_in_middle_of_pattern()
  {
    // Arrange
    string pattern = "exec -- {*args}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(7);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("exec");
    tokens[1].Type.ShouldBe(TokenType.EndOfOptions);
    tokens[1].Value.ShouldBe("--");
    tokens[2].Type.ShouldBe(TokenType.LeftBrace);
    tokens[3].Type.ShouldBe(TokenType.Asterisk);
    tokens[4].Type.ShouldBe(TokenType.Identifier);
    tokens[4].Value.ShouldBe("args");
    tokens[5].Type.ShouldBe(TokenType.RightBrace);
    tokens[6].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_tokenize_end_of_options_at_start()
  {
    // Arrange
    string pattern = "-- something";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(3);
    tokens[0].Type.ShouldBe(TokenType.EndOfOptions);
    tokens[0].Value.ShouldBe("--");
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe("something");
    tokens[2].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_tokenize_multiple_end_of_options_separators()
  {
    // Arrange
    string pattern = "git log -- foo -- bar";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(7);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("git");
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe("log");
    tokens[2].Type.ShouldBe(TokenType.EndOfOptions);
    tokens[2].Value.ShouldBe("--");
    tokens[3].Type.ShouldBe(TokenType.Identifier);
    tokens[3].Value.ShouldBe("foo");
    tokens[4].Type.ShouldBe(TokenType.EndOfOptions);
    tokens[4].Value.ShouldBe("--");
    tokens[5].Type.ShouldBe(TokenType.Identifier);
    tokens[5].Value.ShouldBe("bar");
    tokens[6].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
