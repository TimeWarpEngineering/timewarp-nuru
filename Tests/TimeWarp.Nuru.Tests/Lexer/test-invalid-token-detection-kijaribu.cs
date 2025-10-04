#!/usr/bin/dotnet --

/*
 * CORRECTED ANALYSIS: ❌ DELETE - Initial reviews were INCORRECT
 *
 * **What Both Reviewers Missed:**
 * The numbered tests ALREADY use [Input] parameterization for the SAME patterns!
 * The "unique parameterized testing value" we cited does NOT exist.
 *
 * **Coverage Analysis - 100% REDUNDANT:**
 *
 * InvalidPatternsShouldProduceInvalidTokens (8 patterns):
 * - test--case, foo--bar--baz, my--option → lexer-03 uses [Input("test--case")] ✅
 * - test-, test--, foo--- → lexer-04 uses [Input("test-")] ✅
 * - test<param>, <input> → lexer-07 ✅
 *
 * ValidPatternsShouldNotProduceInvalidTokens (12 patterns):
 * - dry-run, no-edit, save-dev → lexer-01 uses [Input("dry-run")] ✅
 * - --dry-run, --no-edit, -h, -v → lexer-02 ✅
 * - exec --, git log -- → lexer-06 ✅
 *
 * **Result:** All 20 test cases already covered by numbered tests with identical
 * [Input] parameterization approach.
 *
 * **RECOMMENDATION: DELETE**
 * - No unique testing value
 * - 100% duplicate coverage with same testing approach
 * - Eliminates maintenance overhead of keeping two test sets in sync
 * - Numbered tests provide same parameterized efficiency
 *
 * Date Reviewed: 2025-10-04 (Initial - INCORRECT: Said "KEEP")
 * Date Corrected: 2025-10-05 (Corrected to: DELETE)
 * Reviewers: Claude (Roo), Grok - Both initially missed the redundancy
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
