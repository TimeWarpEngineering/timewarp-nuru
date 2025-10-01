#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj
#:project ../../../Source/TimeWarp.Kijaribu/TimeWarp.Kijaribu.csproj

using TimeWarp.Nuru.Parsing;
using Shouldly;
using TimeWarp.Kijaribu;

return await TestRunner.RunTests<DescriptionTokenizationTests>();

[TestTag("Lexer")]
public class DescriptionTokenizationTests
{
  public static async Task ParameterWithDescription()
  {
    // Arrange
    RoutePatternLexer lexer = new("deploy {env|Environment}");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(6);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe("deploy");
    actualTokens[1].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[1].Value.ShouldBe("{");
    actualTokens[2].Type.ShouldBe(TokenType.Identifier);
    actualTokens[2].Value.ShouldBe("env");
    actualTokens[3].Type.ShouldBe(TokenType.Pipe);
    actualTokens[3].Value.ShouldBe("|");
    actualTokens[4].Type.ShouldBe(TokenType.Identifier);
    actualTokens[4].Value.ShouldBe("Environment");
    actualTokens[5].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[5].Value.ShouldBe("}");

    await Task.CompletedTask;
  }

  public static async Task OptionWithDescription()
  {
    // Arrange
    RoutePatternLexer lexer = new("--dry-run,-d|Preview");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(7);
    actualTokens[0].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[0].Value.ShouldBe("--");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("dry-run");
    actualTokens[2].Type.ShouldBe(TokenType.Comma);
    actualTokens[2].Value.ShouldBe(",");
    actualTokens[3].Type.ShouldBe(TokenType.SingleDash);
    actualTokens[3].Value.ShouldBe("-");
    actualTokens[4].Type.ShouldBe(TokenType.Identifier);
    actualTokens[4].Value.ShouldBe("d");
    actualTokens[5].Type.ShouldBe(TokenType.Pipe);
    actualTokens[5].Value.ShouldBe("|");
    actualTokens[6].Type.ShouldBe(TokenType.Identifier);
    actualTokens[6].Value.ShouldBe("Preview");

    await Task.CompletedTask;
  }

  public static async Task ComplexPatternWithDescriptions()
  {
    // Arrange
    RoutePatternLexer lexer = new("deploy {env|Environment} --dry-run,-d|Preview");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(13);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe("deploy");
    actualTokens[1].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[1].Value.ShouldBe("{");
    actualTokens[2].Type.ShouldBe(TokenType.Identifier);
    actualTokens[2].Value.ShouldBe("env");
    actualTokens[3].Type.ShouldBe(TokenType.Pipe);
    actualTokens[3].Value.ShouldBe("|");
    actualTokens[4].Type.ShouldBe(TokenType.Identifier);
    actualTokens[4].Value.ShouldBe("Environment");
    actualTokens[5].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[5].Value.ShouldBe("}");
    actualTokens[6].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[6].Value.ShouldBe("--");
    actualTokens[7].Type.ShouldBe(TokenType.Identifier);
    actualTokens[7].Value.ShouldBe("dry-run");
    actualTokens[8].Type.ShouldBe(TokenType.Comma);
    actualTokens[8].Value.ShouldBe(",");
    actualTokens[9].Type.ShouldBe(TokenType.SingleDash);
    actualTokens[9].Value.ShouldBe("-");
    actualTokens[10].Type.ShouldBe(TokenType.Identifier);
    actualTokens[10].Value.ShouldBe("d");
    actualTokens[11].Type.ShouldBe(TokenType.Pipe);
    actualTokens[11].Value.ShouldBe("|");
    actualTokens[12].Type.ShouldBe(TokenType.Identifier);
    actualTokens[12].Value.ShouldBe("Preview");

    await Task.CompletedTask;
  }
}
