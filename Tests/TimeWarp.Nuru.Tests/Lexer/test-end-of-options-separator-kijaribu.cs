#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj
#:project ../../../Source/TimeWarp.Kijaribu/TimeWarp.Kijaribu.csproj

using TimeWarp.Nuru.Parsing;
using Shouldly;
using TimeWarp.Kijaribu;

return await TestRunner.RunTests<EndOfOptionsSeparatorTests>();

[TestTag("Lexer")]
public class EndOfOptionsSeparatorTests
{
  public static async Task StandaloneDoubleDashShouldBeEndOfOptions()
  {
    // Arrange
    var lexer = new Lexer("--");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();

    // Assert
    actualTokens.Count.ShouldBe(1);
    actualTokens[0].Type.ShouldBe(TokenType.EndOfOptions);
    actualTokens[0].Value.ShouldBe("--");

    await Task.CompletedTask;
  }

  public static async Task CommandFollowedByDoubleDashShouldBeEndOfOptions()
  {
    // Arrange
    var lexer = new Lexer("exec --");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();

    // Assert
    actualTokens.Count.ShouldBe(2);
    actualTokens[0].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.Identifier),
      t => t.Value.ShouldBe("exec"));
    actualTokens[1].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.EndOfOptions),
      t => t.Value.ShouldBe("--"));

    await Task.CompletedTask;
  }

  public static async Task DoubleDashSeparatorWithCatchAllShouldTokenize()
  {
    // Arrange
    var lexer = new Lexer("exec -- {*cmd}");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();

    // Assert
    actualTokens.Count.ShouldBe(6);
    actualTokens[0].Value.ShouldBe("exec");
    actualTokens[1].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.EndOfOptions),
      t => t.Value.ShouldBe("--"));
    actualTokens[2].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[3].Type.ShouldBe(TokenType.Asterisk);
    actualTokens[4].Value.ShouldBe("cmd");
    actualTokens[5].Type.ShouldBe(TokenType.RightBrace);

    await Task.CompletedTask;
  }

  public static async Task GitLogWithDoubleDashSeparatorShouldTokenize()
  {
    // Arrange
    var lexer = new Lexer("git log -- {*files}");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();

    // Assert
    actualTokens.Count.ShouldBe(7);
    actualTokens[0].Value.ShouldBe("git");
    actualTokens[1].Value.ShouldBe("log");
    actualTokens[2].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.EndOfOptions),
      t => t.Value.ShouldBe("--"));

    await Task.CompletedTask;
  }

  public static async Task DoubleDashHelpShouldRemainAsOption()
  {
    // Arrange
    var lexer = new Lexer("--help");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();

    // Assert
    actualTokens.Count.ShouldBe(2);
    actualTokens[0].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.DoubleDash),
      t => t.Value.ShouldBe("--"));
    actualTokens[1].Value.ShouldBe("help");

    await Task.CompletedTask;
  }

  public static async Task DoubleDashEnvShouldRemainAsOption()
  {
    // Arrange
    var lexer = new Lexer("exec --env {e}");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();

    // Assert
    actualTokens.Count.ShouldBe(6);
    actualTokens[0].Value.ShouldBe("exec");
    actualTokens[1].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.DoubleDash),
      t => t.Value.ShouldBe("--"));
    actualTokens[2].Value.ShouldBe("env");

    await Task.CompletedTask;
  }

  public static async Task OptionThenSeparatorShouldTokenizeCorrectly()
  {
    // Arrange
    var lexer = new Lexer("exec --env {e}* -- {*cmd}");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();

    // Assert
    actualTokens.Count.ShouldBe(12);
    actualTokens[0].Value.ShouldBe("exec");

    // First -- is DoubleDash (part of --env option)
    actualTokens[1].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.DoubleDash),
      t => t.Value.ShouldBe("--"));
    actualTokens[2].Value.ShouldBe("env");

    // Second -- is EndOfOptions separator
    actualTokens[7].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.EndOfOptions),
      t => t.Value.ShouldBe("--"));

    await Task.CompletedTask;
  }
}
