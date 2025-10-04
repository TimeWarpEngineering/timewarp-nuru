#!/usr/bin/dotnet --

/*
 * ANALYSIS: ✅ KEEP - Provides unique value through parameterized testing
 *
 * This file uses [Input] attributes for parameterized tests, testing multiple patterns
 * with a single test method. This is more concise than individual test methods.
 *
 * Coverage:
 * - ValidPatternsShouldNotProduceInvalidTokens: 12 valid patterns (compound IDs, options)
 * - InvalidPatternsShouldProduceInvalidTokens: 8 invalid patterns (test--case, test-, <brackets>)
 *
 * Value: Parameterized test approach complements the numbered test suite's explicit tests.
 * The [Input] pattern makes it easy to add new test cases without new methods.
 *
 * Overlap with numbered tests:
 * - Valid patterns overlap with lexer-01, lexer-02, lexer-06
 * - Invalid patterns overlap with lexer-03, lexer-04, lexer-07
 * BUT: This file's parameterized approach provides a quick smoke test suite.
 *
 * Recommendation: KEEP - The parameterized testing pattern adds value for regression testing.
 */
 
 /*
  * ROO REVIEW: AGREE - Valuable parameterized smoke test suite
  *
  * Claude's assessment is spot-on. This file's [Input]-driven tests provide concise, maintainable
  * coverage for invalid token detection without bloating the numbered test files.
  *
  * Coverage Analysis:
  * - ValidPatternsShouldNotProduceInvalidTokens (12 cases): Overlaps with lexer-01 (IDs), lexer-02 (options),
  *   lexer-05 (short options), lexer-06 (end-of-options), but serves as quick verification that common
  *   patterns don't regress into invalids.
  * - InvalidPatternsShouldProduceInvalidTokens (8 cases): Directly aligns with lexer-03 (double dashes like test--case),
  *   lexer-04 (trailing dashes like test-), and lexer-07 (angle brackets like <input>, test<param>).
  *
  * Unique Value:
  * - Single method per category reduces boilerplate vs. individual tests in numbered files.
  * - Easy to extend for new invalids (e.g., add malformed types like {param:invalid}).
  * - Acts as a fast regression gate: Run this file independently for smoke testing lexer changes.
  *
  * Potential Enhancement: Add more edge cases like malformed options (--opt=) or invalid types ({p:foo}).
  *
  * No Conflicts: Assertions focus on presence/absence of Invalid tokens, complementing detailed token
  * sequence checks in numbered tests.
  *
  * Recommendation: KEEP - Enhances test suite with efficient, broad-coverage parameterized tests.
  * Consider integrating as a dedicated "smoke" or "regression" suite if expanding.
  *
  * Date Reviewed: 2025-10-04
  * Reviewer: Roo
  */
 
 return await TestRunner.RunTests<InvalidTokenDetectionTests>();

[TestTag("Lexer")]
public class InvalidTokenDetectionTests
{
  // Valid patterns that should NOT produce Invalid tokens
  [Input("dry-run")]
  [Input("no-edit")]
  [Input("save-dev")]
  [Input("my-long-command")]
  [Input("--dry-run")]
  [Input("--no-edit")]
  [Input("--save-dev")]
  [Input("-h")]
  [Input("-v")]
  [Input("git commit --amend")]
  [Input("deploy --dry-run")]
  [Input("exec --")]
  [Input("git log -- {*files}")]
  public static async Task ValidPatternsShouldNotProduceInvalidTokens(string pattern)
  {
    // Arrange
    var lexer = new Lexer(pattern);

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    bool hasInvalidToken = tokens.Any(t => t.Type == TokenType.Invalid);
    hasInvalidToken.ShouldBeFalse($"Pattern '{pattern}' should not produce Invalid tokens");

    await Task.CompletedTask;
  }

  // Invalid patterns that SHOULD produce Invalid tokens
  [Input("test--case")]
  [Input("foo--bar--baz")]
  [Input("my--option")]
  [Input("test-")]
  [Input("test--")]
  [Input("foo---")]
  [Input("test<param>")]
  [Input("<input>")]
  public static async Task InvalidPatternsShouldProduceInvalidTokens(string pattern)
  {
    // Arrange
    var lexer = new Lexer(pattern);

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    bool hasInvalidToken = tokens.Any(t => t.Type == TokenType.Invalid);
    hasInvalidToken.ShouldBeTrue($"Pattern '{pattern}' should produce Invalid tokens");

    await Task.CompletedTask;
  }
}
