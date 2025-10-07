#!/usr/bin/dotnet --

return await RunTests<ComplexPatternsTests>();

[TestTag("Lexer")]
public class ComplexPatternsTests
{
  public static async Task Should_tokenize_deploy_pattern_with_option()
  {
    // Arrange
    string pattern = "deploy {env} --dry-run";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(7);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("deploy");
    tokens[1].Type.ShouldBe(TokenType.LeftBrace);
    tokens[2].Type.ShouldBe(TokenType.Identifier);
    tokens[2].Value.ShouldBe("env");
    tokens[3].Type.ShouldBe(TokenType.RightBrace);
    tokens[4].Type.ShouldBe(TokenType.DoubleDash);
    tokens[5].Type.ShouldBe(TokenType.Identifier);
    tokens[5].Value.ShouldBe("dry-run");
    tokens[6].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_tokenize_git_commit_with_short_option()
  {
    // Arrange
    string pattern = "git commit -m {message}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(8);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("git");
    tokens[1].Type.ShouldBe(TokenType.Identifier);
    tokens[1].Value.ShouldBe("commit");
    tokens[2].Type.ShouldBe(TokenType.SingleDash);
    tokens[3].Type.ShouldBe(TokenType.Identifier);
    tokens[3].Value.ShouldBe("m");
    tokens[4].Type.ShouldBe(TokenType.LeftBrace);
    tokens[5].Type.ShouldBe(TokenType.Identifier);
    tokens[5].Value.ShouldBe("message");
    tokens[6].Type.ShouldBe(TokenType.RightBrace);
    tokens[7].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_tokenize_build_with_typed_parameter()
  {
    // Arrange
    string pattern = "build --config {mode:string}";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(9);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("build");
    tokens[1].Type.ShouldBe(TokenType.DoubleDash);
    tokens[2].Type.ShouldBe(TokenType.Identifier);
    tokens[2].Value.ShouldBe("config");
    tokens[3].Type.ShouldBe(TokenType.LeftBrace);
    tokens[4].Type.ShouldBe(TokenType.Identifier);
    tokens[4].Value.ShouldBe("mode");
    tokens[5].Type.ShouldBe(TokenType.Colon);
    tokens[6].Type.ShouldBe(TokenType.Identifier);
    tokens[6].Value.ShouldBe("string");
    tokens[7].Type.ShouldBe(TokenType.RightBrace);
    tokens[8].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_tokenize_exec_with_catchall()
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
    tokens[2].Type.ShouldBe(TokenType.LeftBrace);
    tokens[3].Type.ShouldBe(TokenType.Asterisk);
    tokens[4].Type.ShouldBe(TokenType.Identifier);
    tokens[4].Value.ShouldBe("args");
    tokens[5].Type.ShouldBe(TokenType.RightBrace);
    tokens[6].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_tokenize_pattern_with_optional_and_description()
  {
    // Arrange
    string pattern = "greet {name?} | Say hello";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.Count.ShouldBe(9);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("greet");
    tokens[1].Type.ShouldBe(TokenType.LeftBrace);
    tokens[2].Type.ShouldBe(TokenType.Identifier);
    tokens[2].Value.ShouldBe("name");
    tokens[3].Type.ShouldBe(TokenType.Question);
    tokens[4].Type.ShouldBe(TokenType.RightBrace);
    tokens[5].Type.ShouldBe(TokenType.Pipe);
    tokens[6].Type.ShouldBe(TokenType.Identifier);
    tokens[6].Value.ShouldBe("Say");
    tokens[7].Type.ShouldBe(TokenType.Identifier);
    tokens[7].Value.ShouldBe("hello");
    tokens[8].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }

  public static async Task Should_tokenize_comprehensive_complex_pattern()
  {
    // Arrange
    string pattern = "cmd {a} {b:int} --flag {c?} | description";
    Lexer lexer = CreateLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert - validates ALL features together
    tokens.Count.ShouldBe(18);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe("cmd");
    tokens[1].Type.ShouldBe(TokenType.LeftBrace);
    tokens[2].Type.ShouldBe(TokenType.Identifier);
    tokens[2].Value.ShouldBe("a");
    tokens[3].Type.ShouldBe(TokenType.RightBrace);
    tokens[4].Type.ShouldBe(TokenType.LeftBrace);
    tokens[5].Type.ShouldBe(TokenType.Identifier);
    tokens[5].Value.ShouldBe("b");
    tokens[6].Type.ShouldBe(TokenType.Colon);
    tokens[7].Type.ShouldBe(TokenType.Identifier);
    tokens[7].Value.ShouldBe("int");
    tokens[8].Type.ShouldBe(TokenType.RightBrace);
    tokens[9].Type.ShouldBe(TokenType.DoubleDash);
    tokens[10].Type.ShouldBe(TokenType.Identifier);
    tokens[10].Value.ShouldBe("flag");
    tokens[11].Type.ShouldBe(TokenType.LeftBrace);
    tokens[12].Type.ShouldBe(TokenType.Identifier);
    tokens[12].Value.ShouldBe("c");
    tokens[13].Type.ShouldBe(TokenType.Question);
    tokens[14].Type.ShouldBe(TokenType.RightBrace);
    tokens[15].Type.ShouldBe(TokenType.Pipe);
    tokens[16].Type.ShouldBe(TokenType.Identifier);
    tokens[16].Value.ShouldBe("description");
    tokens[17].Type.ShouldBe(TokenType.EndOfInput);

    await Task.CompletedTask;
  }
}
