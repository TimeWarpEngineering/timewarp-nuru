#!/usr/bin/dotnet --

using TimeWarp.Nuru.Parsing;
using Shouldly;
using TimeWarp.Kijaribu;

return await TestRunner.RunTests<CompoundIdentifierTokenizationTests>();

[TestTag("Lexer")]
public class CompoundIdentifierTokenizationTests
{
  // Basic Compound Identifiers (as literals)
  [Input("async-test")]
  [Input("no-edit")]
  [Input("my-long-command-name")]
  [Input("test-case-1")]
  public static async Task SimpleCompoundIdentifiersShouldBeSingleTokens(string pattern)
  {
    // Arrange
    Lexer lexer = new(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(1);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe(pattern);

    await Task.CompletedTask;
  }

  // Compound Identifiers in Options
  public static async Task NoEditOption()
  {
    // Arrange
    Lexer lexer = new("--no-edit");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(2);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[0].Value.ShouldBe("--");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("no-edit");

    await Task.CompletedTask;
  }

  public static async Task DryRunOption()
  {
    // Arrange
    Lexer lexer = new("--dry-run");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(2);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[0].Value.ShouldBe("--");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("dry-run");

    await Task.CompletedTask;
  }

  public static async Task MaxCountOption()
  {
    // Arrange
    Lexer lexer = new("--max-count");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(2);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[0].Value.ShouldBe("--");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("max-count");

    await Task.CompletedTask;
  }

  public static async Task SaveDevOption()
  {
    // Arrange
    Lexer lexer = new("--save-dev");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(2);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[0].Value.ShouldBe("--");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("save-dev");

    await Task.CompletedTask;
  }

  public static async Task EnhanceLogsOption()
  {
    // Arrange
    Lexer lexer = new("--enhance-logs");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(2);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[0].Value.ShouldBe("--");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("enhance-logs");

    await Task.CompletedTask;
  }

  // Edge Cases with Dashes
  public static async Task TrailingDashProducesInvalidToken()
  {
    // Arrange
    Lexer lexer = new("test-");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert - trailing dash is invalid
    actualTokens.Length.ShouldBe(1);
    actualTokens[0].Type.ShouldBe(TokenType.Invalid);
    actualTokens[0].Value.ShouldBe("test-");

    await Task.CompletedTask;
  }

  public static async Task JustDash()
  {
    // Arrange
    Lexer lexer = new("-");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(1);
    actualTokens[0].Type.ShouldBe(TokenType.SingleDash);
    actualTokens[0].Value.ShouldBe("-");

    await Task.CompletedTask;
  }

  public static async Task MultipleConsecutiveDashesProduceInvalidToken()
  {
    // Arrange
    Lexer lexer = new("test--case");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert - consecutive dashes are invalid
    actualTokens.Length.ShouldBe(1);
    actualTokens[0].Type.ShouldBe(TokenType.Invalid);
    actualTokens[0].Value.ShouldBe("test--case");

    await Task.CompletedTask;
  }

  // Complex Patterns with Compound Identifiers
  public static async Task GitCommitNoEdit()
  {
    // Arrange
    Lexer lexer = new("git commit --no-edit");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(4);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe("git");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("commit");
    actualTokens[2].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[2].Value.ShouldBe("--");
    actualTokens[3].Type.ShouldBe(TokenType.Identifier);
    actualTokens[3].Value.ShouldBe("no-edit");

    await Task.CompletedTask;
  }

  public static async Task DockerRunSaveDevWithParameter()
  {
    // Arrange
    Lexer lexer = new("docker run --save-dev {image}");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(7);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe("docker");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("run");
    actualTokens[2].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[2].Value.ShouldBe("--");
    actualTokens[3].Type.ShouldBe(TokenType.Identifier);
    actualTokens[3].Value.ShouldBe("save-dev");
    actualTokens[4].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[4].Value.ShouldBe("{");
    actualTokens[5].Type.ShouldBe(TokenType.Identifier);
    actualTokens[5].Value.ShouldBe("image");
    actualTokens[6].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[6].Value.ShouldBe("}");

    await Task.CompletedTask;
  }

  public static async Task GitLogMaxCountWithTypedParameter()
  {
    // Arrange
    Lexer lexer = new("git log --max-count {count:int}");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(9);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe("git");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("log");
    actualTokens[2].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[2].Value.ShouldBe("--");
    actualTokens[3].Type.ShouldBe(TokenType.Identifier);
    actualTokens[3].Value.ShouldBe("max-count");
    actualTokens[4].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[4].Value.ShouldBe("{");
    actualTokens[5].Type.ShouldBe(TokenType.Identifier);
    actualTokens[5].Value.ShouldBe("count");
    actualTokens[6].Type.ShouldBe(TokenType.Colon);
    actualTokens[6].Value.ShouldBe(":");
    actualTokens[7].Type.ShouldBe(TokenType.Identifier);
    actualTokens[7].Value.ShouldBe("int");
    actualTokens[8].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[8].Value.ShouldBe("}");

    await Task.CompletedTask;
  }

  public static async Task NpmInstallSaveDevAfterParameter()
  {
    // Arrange
    Lexer lexer = new("npm install {package} --save-dev");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(7);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe("npm");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("install");
    actualTokens[2].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[2].Value.ShouldBe("{");
    actualTokens[3].Type.ShouldBe(TokenType.Identifier);
    actualTokens[3].Value.ShouldBe("package");
    actualTokens[4].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[4].Value.ShouldBe("}");
    actualTokens[5].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[5].Value.ShouldBe("--");
    actualTokens[6].Type.ShouldBe(TokenType.Identifier);
    actualTokens[6].Value.ShouldBe("save-dev");

    await Task.CompletedTask;
  }
}
