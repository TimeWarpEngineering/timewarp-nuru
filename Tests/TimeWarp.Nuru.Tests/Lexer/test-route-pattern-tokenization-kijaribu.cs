#!/usr/bin/dotnet --

/*
 * ANALYSIS: ✅ KEEP - Comprehensive parameterized regression test suite
 *
 * This file provides extensive parameterized testing (38 patterns in one test method + 7 specific tests).
 * Uses [Input] attributes to test many patterns efficiently.
 *
 * RoutePatternTokenizationTests.ShouldTokenizeWithoutException:
 * - Tests 38 diverse patterns from basic literals to edge cases
 * - Verifies lexer doesn't throw exceptions and always produces EndOfInput
 * - Acts as a smoke test / regression suite
 *
 * SpecificTokenSequenceTests:
 * - 7 tests with detailed token sequence verification
 * - Real-world patterns: git commit, docker, kubectl, npm
 *
 * Value:
 * - Parameterized approach makes it easy to add regression test cases
 * - Comprehensive edge case coverage (empty, whitespace, trailing dashes)
 * - Tests real CLI patterns that users will actually write
 *
 * Overlap: Significant overlap with lexer-01 through lexer-09, BUT the parameterized
 * testing approach and comprehensive pattern list provide regression safety.
 *
 * Recommendation: KEEP - Valuable regression test suite with efficient parameterized tests.
 */
 
 /*
  * ROO REVIEW: AGREE - Essential regression and smoke testing layer
  *
  * Claude correctly identifies this as a high-value file. The 38-parameterized patterns in
  * ShouldTokenizeWithoutException provide broad, low-maintenance coverage for lexer stability,
  * ensuring no regressions in basic tokenization (e.g., empty/whitespace, edge invalids like test-).
  *
  * SpecificTokenSequenceTests (7 detailed cases) add precision for real-world CLI patterns:
  * - git commit --no-edit (lexer-09 overlap, but verifies full sequence)
  * - kubectl apply -f {file} (short option + param)
  * - npm install {package} --save-dev (param before option)
  *
  * Overlap Assessment:
  * - Significant with lexer-01 to lexer-09 (basics, options, params, complexes), but this file's
  *   focus on "no exceptions + EndOfInput" acts as a safety net, catching syntax errors early.
  * - Edge cases (e.g., "--option={value}", "-test") test boundaries not deeply covered elsewhere.
  *
  * Unique Strengths:
  * - Parameterized format scales easily for new patterns (e.g., add typed optionals like --opt? {v:int?}).
  * - Real CLI examples (docker, kubectl, npm) align with user expectations, aiding confidence.
  * - Complements numbered tests by prioritizing breadth over depth.
  *
  * Potential Improvement: Expand invalids section to include more malformed syntax (e.g., {{nested}}).
  *
  * No Issues: Assertions are non-intrusive, focusing on successful parsing without token details.
  *
  * Recommendation: KEEP - Critical for regression; consider running as CI smoke test.
  *
  * Date Reviewed: 2025-10-04
  * Reviewer: Roo
  */
 
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
