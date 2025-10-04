#!/usr/bin/dotnet --

/*
 * ANALYSIS: ✅ KEEP - Clean structured tests that complement lexer-06
 *
 * This file tests end-of-options (--) separator behavior with proper test structure.
 *
 * Coverage (8 tests):
 * - Standalone -- → EndOfOptions
 * - Command followed by -- → EndOfOptions
 * - -- with catch-all: exec -- {*cmd}
 * - git log -- {*files}
 * - --help → DoubleDash (option, not separator)
 * - --env {e} → DoubleDash (option prefix)
 * - exec --env {e}* -- {*cmd} → First -- is DoubleDash, second is EndOfOptions ✅
 *
 * Value:
 * - The last test (OptionThenSeparatorShouldTokenizeCorrectly) is the critical
 *   distinction test that was missing from the original lexer-06
 * - Clean, well-structured tests using ShouldSatisfyAllConditions
 * - Good complement to lexer-06's numbered structure
 *
 * Overlap: Yes with lexer-06, but provides alternative test style
 *
 * Recommendation: KEEP - Well-structured tests with the critical distinction test.
 * Consider as companion to lexer-06 or merge if consolidating.
 */
 
 /*
  * ROO REVIEW: PARTIALLY DISAGREE - Redundant with lexer-06, but style adds minor value
  *
  * Claude's analysis notes the critical distinction test (OptionThenSeparatorShouldTokenizeCorrectly)
  * for distinguishing DoubleDash (--env) from EndOfOptions (-- separator). However, this exact test
  * already exists in lexer-06-end-of-options.cs as Should_distinguish_option_double_dash_from_separator,
  * using the same pattern "exec --env {e}* -- {*cmd}" with identical assertions.
  *
  * Coverage Overlap:
  * - Standalone --, command + --, -- with catch-all, git log -- {*files}: Directly covered in lexer-06.
  * - --help and --env as DoubleDash: Covered in lexer-02-valid-options.cs and lexer-06.
  * - The distinction test: Duplicated exactly in lexer-06.
  *
  * Value of Kijaribu Style:
  * - Uses ShouldSatisfyAllConditions for concise assertions vs. lexer-06's explicit indexing.
  * - Alternative structure could aid readability or maintenance, but duplication increases risk.
  *
  * Potential Issue: No unique edges; all 8 tests map to existing numbered tests without new scenarios.
  *
  * Recommendation: DELETE or MERGE - Consolidate into lexer-06 for single source of truth on end-of-options.
  * If keeping for style variety, ensure assertions align perfectly to avoid divergence.
  *
  * Date Reviewed: 2025-10-04
  * Reviewer: Roo
  */
 
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
