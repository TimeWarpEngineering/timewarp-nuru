#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj
#:project ../../../Source/TimeWarp.Kijaribu/TimeWarp.Kijaribu.csproj

using TimeWarp.Nuru.Parsing;
using Shouldly;
using TimeWarp.Kijaribu;

return await TestRunner.RunTests<ModifierTokenizationTests>();

[TestTag("Lexer")]
public class ModifierTokenizationTests
{
  // Optional Flag Modifiers
  public static async Task OptionalVerboseFlag()
  {
    // Arrange
    Lexer lexer = new("--verbose?");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(3);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[0].Value.ShouldBe("--");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("verbose");
    actualTokens[2].Type.ShouldBe(TokenType.Question);
    actualTokens[2].Value.ShouldBe("?");

    await Task.CompletedTask;
  }

  public static async Task OptionalDryRunFlag()
  {
    // Arrange
    Lexer lexer = new("--dry-run?");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(3);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[0].Value.ShouldBe("--");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("dry-run");
    actualTokens[2].Type.ShouldBe(TokenType.Question);
    actualTokens[2].Value.ShouldBe("?");

    await Task.CompletedTask;
  }

  public static async Task OptionalShortFlag()
  {
    // Arrange
    Lexer lexer = new("-v?");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(3);
    actualTokens[0].Type.ShouldBe(TokenType.SingleDash);
    actualTokens[0].Value.ShouldBe("-");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("v");
    actualTokens[2].Type.ShouldBe(TokenType.Question);
    actualTokens[2].Value.ShouldBe("?");

    await Task.CompletedTask;
  }

  public static async Task OptionalFlagWithParameter()
  {
    // Arrange
    Lexer lexer = new("--config? {mode}");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(6);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("config");
    actualTokens[2].Type.ShouldBe(TokenType.Question);
    actualTokens[3].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[4].Type.ShouldBe(TokenType.Identifier);
    actualTokens[4].Value.ShouldBe("mode");
    actualTokens[5].Type.ShouldBe(TokenType.RightBrace);

    await Task.CompletedTask;
  }

  public static async Task OptionalFlagWithOptionalParameter()
  {
    // Arrange
    Lexer lexer = new("--env? {name?}");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(7);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("env");
    actualTokens[2].Type.ShouldBe(TokenType.Question);
    actualTokens[3].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[4].Type.ShouldBe(TokenType.Identifier);
    actualTokens[4].Value.ShouldBe("name");
    actualTokens[5].Type.ShouldBe(TokenType.Question);
    actualTokens[6].Type.ShouldBe(TokenType.RightBrace);

    await Task.CompletedTask;
  }

  // Repeated Parameter Modifiers
  public static async Task RepeatedParameter()
  {
    // Arrange
    Lexer lexer = new("--env {var}*");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(6);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("env");
    actualTokens[2].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[3].Type.ShouldBe(TokenType.Identifier);
    actualTokens[3].Value.ShouldBe("var");
    actualTokens[4].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[5].Type.ShouldBe(TokenType.Asterisk);

    await Task.CompletedTask;
  }

  public static async Task RepeatedTypedParameter()
  {
    // Arrange
    Lexer lexer = new("--port {p:int}*");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(8);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("port");
    actualTokens[2].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[3].Type.ShouldBe(TokenType.Identifier);
    actualTokens[3].Value.ShouldBe("p");
    actualTokens[4].Type.ShouldBe(TokenType.Colon);
    actualTokens[5].Type.ShouldBe(TokenType.Identifier);
    actualTokens[5].Value.ShouldBe("int");
    actualTokens[6].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[7].Type.ShouldBe(TokenType.Asterisk);

    await Task.CompletedTask;
  }

  public static async Task MultipleRepeatedParameters()
  {
    // Arrange
    Lexer lexer = new("--label {l}* --tag {t}*");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(12);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[1].Value.ShouldBe("label");
    actualTokens[5].Type.ShouldBe(TokenType.Asterisk);
    actualTokens[6].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[7].Value.ShouldBe("tag");
    actualTokens[11].Type.ShouldBe(TokenType.Asterisk);

    await Task.CompletedTask;
  }

  // Combined Modifiers
  public static async Task OptionalFlagWithRepeatedParameter()
  {
    // Arrange
    Lexer lexer = new("--env? {var}*");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(7);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[1].Value.ShouldBe("env");
    actualTokens[2].Type.ShouldBe(TokenType.Question);
    actualTokens[3].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[4].Value.ShouldBe("var");
    actualTokens[5].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[6].Type.ShouldBe(TokenType.Asterisk);

    await Task.CompletedTask;
  }

  public static async Task ComplexOptionalRepeatedCombination()
  {
    // Arrange
    Lexer lexer = new("--opt? {val?}*");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(8);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[1].Value.ShouldBe("opt");
    actualTokens[2].Type.ShouldBe(TokenType.Question);
    actualTokens[4].Value.ShouldBe("val");
    actualTokens[5].Type.ShouldBe(TokenType.Question);
    actualTokens[7].Type.ShouldBe(TokenType.Asterisk);

    await Task.CompletedTask;
  }

  public static async Task OptionalRepeatedFlag()
  {
    // Arrange
    Lexer lexer = new("--flag?*");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(4);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[1].Value.ShouldBe("flag");
    actualTokens[2].Type.ShouldBe(TokenType.Question);
    actualTokens[3].Type.ShouldBe(TokenType.Asterisk);

    await Task.CompletedTask;
  }

  // Complex Patterns
  public static async Task DeployWithMultipleOptionalFlags()
  {
    // Arrange
    Lexer lexer = new("deploy {env} --force? --dry-run?");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(10);
    actualTokens[0].Value.ShouldBe("deploy");
    actualTokens[2].Value.ShouldBe("env");
    actualTokens[5].Value.ShouldBe("force");
    actualTokens[6].Type.ShouldBe(TokenType.Question);
    actualTokens[8].Value.ShouldBe("dry-run");
    actualTokens[9].Type.ShouldBe(TokenType.Question);

    await Task.CompletedTask;
  }

  public static async Task DockerWithOptionalRepeatedAndCatchAll()
  {
    // Arrange
    Lexer lexer = new("docker --env? {e}* {*cmd}");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(12);
    actualTokens[0].Value.ShouldBe("docker");
    actualTokens[2].Value.ShouldBe("env");
    actualTokens[3].Type.ShouldBe(TokenType.Question);
    actualTokens[7].Type.ShouldBe(TokenType.Asterisk);
    actualTokens[9].Type.ShouldBe(TokenType.Asterisk);
    actualTokens[10].Value.ShouldBe("cmd");

    await Task.CompletedTask;
  }
}
