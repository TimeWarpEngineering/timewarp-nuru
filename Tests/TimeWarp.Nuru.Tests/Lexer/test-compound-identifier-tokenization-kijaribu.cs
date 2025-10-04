#!/usr/bin/dotnet --

/*
 * THIS FILE IS REDUNDANT AND SCHEDULED FOR DELETION
 *
 * Reason for Deletion:
 * This Kijaribu-structured test duplicates coverage for compound identifiers and options, centralized in:
 * - lexer-01-basic-token-types.cs (plain/compound IDs like async-test, no-edit)
 * - lexer-02-valid-options.cs and lexer-05-multi-char-short-options.cs (options like --no-edit, --dry-run)
 * - lexer-03-invalid-double-dashes.cs and lexer-04-invalid-trailing-dashes.cs (invalids like test--case, test-)
 * - lexer-09-complex-patterns.cs (CLI mixes like git commit --no-edit, npm install {package} --save-dev)
 * Unique edge like -test as invalid conflicts with lexer-05 (valid multi-char short); align or integrate if needed.
 * After review, this file can be safely deleted to reduce redundancy.
 *
 * Date Reviewed: 2025-10-04
 * Reviewer: Roo (AI Assistant)
 * Recommendation: Delete – no unique value beyond numbered tests.
 */
 
 /*
  * ROO REVIEW: AGREE - Redundant with existing numbered lexer tests
  *
  * Claude's analysis is accurate. This file's coverage for compound identifiers,
  * dashed options (--no-edit, --dry-run), and invalid dash patterns (test-, test--case)
  * is fully covered in:
  * - lexer-01-basic-token-types.cs: Plain and compound identifiers (dry-run, no-edit, etc.)
  * - lexer-02-valid-options.cs: Valid long options with compounds
  * - lexer-03-invalid-double-dashes.cs: Consecutive dashes (test--case)
  * - lexer-04-invalid-trailing-dashes.cs: Trailing dashes (test-)
  * - lexer-05-multi-char-short-options.cs: Short options, which conflicts with treating -test as invalid here
  * - lexer-09-complex-patterns.cs: Real CLI mixes like git commit --no-edit
  *
  * The complex pattern tests (GitCommitNoEdit, NpmInstallSaveDevAfterParameter) duplicate
  * lexer-09's coverage without adding unique assertions or edge cases.
  *
  * Potential Issue: Treating "-test" as invalid (in TrailingDashProducesInvalidToken variant)
  * conflicts with lexer-05's valid multi-char shorts. This file's logic assumes single-char
  * shorts only, but Nuru supports multi-char shorts per design docs.
  *
  * Recommendation: CONFIRM DELETE - No unique value; resolve any short option conflicts
  * in core lexer tests first if needed. Reduces test maintenance overhead.
  *
  * Date Reviewed: 2025-10-04
  * Reviewer: Roo
  */
 
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
