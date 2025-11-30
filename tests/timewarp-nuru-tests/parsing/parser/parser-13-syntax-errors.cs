#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
return await RunTests<SyntaxErrorTests>(clearCache: true);

[TestTag("Parser")]
[ClearRunfileCache]
public class SyntaxErrorTests
{
  // Section 13: Additional syntax validation tests
  // These tests cover edge cases in lexical and syntactic validation

  public static async Task Should_reject_identifier_starting_with_number()
  {
    // Identifiers must start with letter or underscore, not digit
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("build {123abc}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_incomplete_parameter_with_only_opening_brace()
  {
    // Just '{' without closing is incomplete
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_empty_parameter_braces()
  {
    // '{ }' has no parameter name
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("test { }")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_incomplete_option_parameter()
  {
    // Option parameter started but not completed
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("build --config {")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_unexpected_closing_brace()
  {
    // Closing brace without opening
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("test }")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_combined_catchall_and_optional_modifiers()
  {
    // Cannot combine * and ? modifiers in same parameter
    // Grammar only allows ONE modifier: either {*param} OR {param?}, not both
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("deploy {*files?}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }
}
