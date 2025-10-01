#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../../Source/TimeWarp.Kijaribu/TimeWarp.Kijaribu.csproj

using TimeWarp.Nuru.Parsing;
using Shouldly;
using TimeWarp.Kijaribu;

await TestRunner.RunTests<RoutePatternTokenizationTests>();

public class RoutePatternTokenizationTests
{
  // All patterns should tokenize successfully with EndOfInput as last token
  [Input("status")]
  [Input("git status")]
  [Input("git commit push")]
  [Input("async-test")]
  [Input("no-edit")]
  [Input("my-long-command-name")]
  [Input("{name}")]
  [Input("{name:string}")]
  [Input("{count:int}")]
  [Input("{tag?}")]
  [Input("{seconds:int?}")]
  [Input("{*args}")]
  [Input("{name|Description}")]
  [Input("{count:int|Number of items}")]
  [Input("--help")]
  [Input("-h")]
  [Input("--no-edit")]
  [Input("--max-count")]
  [Input("git commit --amend")]
  [Input("git commit --amend --no-edit")]
  [Input("git commit -m {message}")]
  [Input("git commit --message {message}")]
  [Input("git log --max-count {count:int}")]
  [Input("docker run --enhance-logs {image}")]
  [Input("kubectl apply -f {file}")]
  [Input("npm install {package} --save-dev")]
  [Input("git commit -m {message} --amend")]
  [Input("git commit --amend -m {message}")]
  [Input("git commit --amend --message {message}")]
  [Input("git commit --message {message} --amend")]
  [Input("deploy {env} --dry-run")]
  [Input("deploy {env} --version {ver}")]
  [Input("kubectl get {resource} --watch --enhanced")]
  [Input("")]
  [Input("   ")]
  [Input("--")]
  [Input("-")]
  [Input("test-")]
  [Input("-test")]
  [Input("test<input>")]
  [Input("test{param}test")]
  [Input("--option={value}")]
  public static async Task PatternShouldTokenizeWithEndOfInput(string pattern)
  {
    // Act
    var lexer = new RoutePatternLexer(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.ShouldNotBeEmpty();
    tokens[^1].Type.ShouldBe(TokenType.EndOfInput, $"Pattern '{pattern}' should end with EndOfInput token");

    await Task.CompletedTask;
  }

  public static async Task AsyncTestShouldTokenizeAsCompoundIdentifier()
  {
    // Arrange
    var lexer = new RoutePatternLexer("async-test");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();
    actualTokens.Count.ShouldBe(1);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe("async-test");

    await Task.CompletedTask;
  }

  public static async Task GitCommitWithNoEditShouldTokenizeCorrectly()
  {
    // Arrange
    var lexer = new RoutePatternLexer("git commit --no-edit");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();
    actualTokens.Count.ShouldBe(4);
    actualTokens[0].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.Identifier),
      t => t.Value.ShouldBe("git"));
    actualTokens[1].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.Identifier),
      t => t.Value.ShouldBe("commit"));
    actualTokens[2].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.DoubleDash),
      t => t.Value.ShouldBe("--"));
    actualTokens[3].ShouldSatisfyAllConditions(
      t => t.Type.ShouldBe(TokenType.Identifier),
      t => t.Value.ShouldBe("no-edit"));

    await Task.CompletedTask;
  }

  public static async Task GitCommitShortOptionShouldTokenizeCorrectly()
  {
    // Arrange
    var lexer = new RoutePatternLexer("git commit -m {message}");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();
    actualTokens.Count.ShouldBe(7);
    actualTokens[0].Value.ShouldBe("git");
    actualTokens[1].Value.ShouldBe("commit");
    actualTokens[2].Type.ShouldBe(TokenType.SingleDash);
    actualTokens[3].Value.ShouldBe("m");
    actualTokens[4].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[5].Value.ShouldBe("message");
    actualTokens[6].Type.ShouldBe(TokenType.RightBrace);

    await Task.CompletedTask;
  }

  public static async Task GitLogTypedParameterShouldTokenizeCorrectly()
  {
    // Arrange
    var lexer = new RoutePatternLexer("git log --max-count {count:int}");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();
    actualTokens.Count.ShouldBe(9);
    actualTokens[0].Value.ShouldBe("git");
    actualTokens[1].Value.ShouldBe("log");
    actualTokens[2].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[3].Value.ShouldBe("max-count");
    actualTokens[4].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[5].Value.ShouldBe("count");
    actualTokens[6].Type.ShouldBe(TokenType.Colon);
    actualTokens[7].Value.ShouldBe("int");
    actualTokens[8].Type.ShouldBe(TokenType.RightBrace);

    await Task.CompletedTask;
  }

  public static async Task DockerEnhanceLogsShouldTokenizeCorrectly()
  {
    // Arrange
    var lexer = new RoutePatternLexer("docker run --enhance-logs {image}");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();
    actualTokens.Count.ShouldBe(7);
    actualTokens[3].Value.ShouldBe("enhance-logs");

    await Task.CompletedTask;
  }

  public static async Task KubectlApplyShouldTokenizeCorrectly()
  {
    // Arrange
    var lexer = new RoutePatternLexer("kubectl apply -f {file}");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();
    actualTokens.Count.ShouldBe(7);
    actualTokens[2].Type.ShouldBe(TokenType.SingleDash);
    actualTokens[3].Value.ShouldBe("f");

    await Task.CompletedTask;
  }

  public static async Task NpmInstallSaveDevShouldTokenizeCorrectly()
  {
    // Arrange
    var lexer = new RoutePatternLexer("npm install {package} --save-dev");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();
    actualTokens.Count.ShouldBe(7);
    actualTokens[5].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[6].Value.ShouldBe("save-dev");

    await Task.CompletedTask;
  }

  public static async Task GitCommitMessageThenAmendShouldTokenizeCorrectly()
  {
    // Arrange
    var lexer = new RoutePatternLexer("git commit -m {message} --amend");

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    var actualTokens = tokens.Take(tokens.Count - 1).ToList();
    actualTokens.Count.ShouldBe(9);
    actualTokens[7].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[8].Value.ShouldBe("amend");

    await Task.CompletedTask;
  }
}
