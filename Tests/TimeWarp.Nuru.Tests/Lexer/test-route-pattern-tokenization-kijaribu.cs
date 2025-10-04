#!/usr/bin/dotnet --

int exitCode = 0;
exitCode |= await RunTests<RoutePatternTokenizationTests>();
exitCode |= await RunTests<SpecificTokenSequenceTests>();
return exitCode;

[TestTag("Lexer")]
public class RoutePatternTokenizationTests
{
  // Basic literals
  [Input("status")]
  [Input("git status")]
  [Input("git commit push")]
  // Compound identifiers with dashes
  [Input("async-test")]
  [Input("no-edit")]
  [Input("my-long-command-name")]
  // Parameters
  [Input("{name}")]
  [Input("{name:string}")]
  [Input("{count:int}")]
  [Input("{tag?}")]
  [Input("{seconds:int?}")]
  [Input("{*args}")]
  [Input("{name|Description}")]
  [Input("{count:int|Number of items}")]
  // Options
  [Input("--help")]
  [Input("-h")]
  [Input("--no-edit")]
  [Input("--max-count")]
  // Complex patterns
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
  // Mixed patterns
  [Input("deploy {env} --dry-run")]
  [Input("deploy {env} --version {ver}")]
  [Input("kubectl get {resource} --watch --enhanced")]
  // Edge cases
  [Input("")]
  [Input("   ")]
  [Input("--")]
  [Input("-")]
  [Input("test-")]
  [Input("-test")]
  [Input("test<input>")]
  [Input("test{param}test")]
  [Input("--option={value}")]
  public static async Task ShouldTokenizeWithoutException(string pattern)
  {
    // Arrange & Act
    Lexer lexer = new(pattern);
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    tokens.ShouldNotBeEmpty();
    tokens[tokens.Count - 1].Type.ShouldBe(TokenType.EndOfInput, "Last token should always be EndOfInput");

    await Task.CompletedTask;
  }
}

[TestTag("Lexer")]
public class SpecificTokenSequenceTests
{
  public static async Task CompoundIdentifier()
  {
    // Arrange
    Lexer lexer = new("async-test");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(1);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe("async-test");

    await Task.CompletedTask;
  }

  public static async Task CommandWithLongOptionWithDash()
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

  public static async Task ShortOptionWithParameter()
  {
    // Arrange
    Lexer lexer = new("git commit -m {message}");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(7);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe("git");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("commit");
    actualTokens[2].Type.ShouldBe(TokenType.SingleDash);
    actualTokens[2].Value.ShouldBe("-");
    actualTokens[3].Type.ShouldBe(TokenType.Identifier);
    actualTokens[3].Value.ShouldBe("m");
    actualTokens[4].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[4].Value.ShouldBe("{");
    actualTokens[5].Type.ShouldBe(TokenType.Identifier);
    actualTokens[5].Value.ShouldBe("message");
    actualTokens[6].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[6].Value.ShouldBe("}");

    await Task.CompletedTask;
  }

  public static async Task LongOptionWithTypedParameter()
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

  public static async Task DockerWithEnhancedLogsOption()
  {
    // Arrange
    Lexer lexer = new("docker run --enhance-logs {image}");
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
    actualTokens[3].Value.ShouldBe("enhance-logs");
    actualTokens[4].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[4].Value.ShouldBe("{");
    actualTokens[5].Type.ShouldBe(TokenType.Identifier);
    actualTokens[5].Value.ShouldBe("image");
    actualTokens[6].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[6].Value.ShouldBe("}");

    await Task.CompletedTask;
  }

  public static async Task KubectlApplyShortOption()
  {
    // Arrange
    Lexer lexer = new("kubectl apply -f {file}");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(7);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe("kubectl");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("apply");
    actualTokens[2].Type.ShouldBe(TokenType.SingleDash);
    actualTokens[2].Value.ShouldBe("-");
    actualTokens[3].Type.ShouldBe(TokenType.Identifier);
    actualTokens[3].Value.ShouldBe("f");
    actualTokens[4].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[4].Value.ShouldBe("{");
    actualTokens[5].Type.ShouldBe(TokenType.Identifier);
    actualTokens[5].Value.ShouldBe("file");
    actualTokens[6].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[6].Value.ShouldBe("}");

    await Task.CompletedTask;
  }

  public static async Task NpmInstallWithSaveDevOption()
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

  public static async Task GitCommitShortOptionThenBooleanOption()
  {
    // Arrange
    Lexer lexer = new("git commit -m {message} --amend");
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    Token[] actualTokens = [.. tokens.Take(tokens.Count - 1)];

    // Assert
    actualTokens.Length.ShouldBe(9);
    actualTokens[0].Type.ShouldBe(TokenType.Identifier);
    actualTokens[0].Value.ShouldBe("git");
    actualTokens[1].Type.ShouldBe(TokenType.Identifier);
    actualTokens[1].Value.ShouldBe("commit");
    actualTokens[2].Type.ShouldBe(TokenType.SingleDash);
    actualTokens[2].Value.ShouldBe("-");
    actualTokens[3].Type.ShouldBe(TokenType.Identifier);
    actualTokens[3].Value.ShouldBe("m");
    actualTokens[4].Type.ShouldBe(TokenType.LeftBrace);
    actualTokens[4].Value.ShouldBe("{");
    actualTokens[5].Type.ShouldBe(TokenType.Identifier);
    actualTokens[5].Value.ShouldBe("message");
    actualTokens[6].Type.ShouldBe(TokenType.RightBrace);
    actualTokens[6].Value.ShouldBe("}");
    actualTokens[7].Type.ShouldBe(TokenType.DoubleDash);
    actualTokens[7].Value.ShouldBe("--");
    actualTokens[8].Type.ShouldBe(TokenType.Identifier);
    actualTokens[8].Value.ShouldBe("amend");

    await Task.CompletedTask;
  }
}
