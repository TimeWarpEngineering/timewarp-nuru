#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
await TestRunner.RunTests<ErrorReportingTests>(clearCache: true);

[TestTag("Parser")]
[ClearRunfileCache]
public class ErrorReportingTests
{
  // Section 15: Error reporting and edge cases
  // Verifies comprehensive error coverage for both semantic and parse errors

  // Semantic Errors (NURU_S###) - Already tested in earlier sections, verified here for coverage

  public static async Task Should_report_duplicate_parameter_names()
  {
    // NURU_S001
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {arg} {arg}")
    );

    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<DuplicateParameterNamesError>();

    await Task.CompletedTask;
  }

  public static async Task Should_report_conflicting_optional_parameters()
  {
    // NURU_S002
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {arg1?} {arg2?}")
    );

    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<ConflictingOptionalParametersError>();

    await Task.CompletedTask;
  }

  public static async Task Should_report_catchall_not_last()
  {
    // NURU_S003
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {*args} {script}")
    );

    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<CatchAllNotAtEndError>();

    await Task.CompletedTask;
  }

  public static async Task Should_report_mixed_catchall_with_optional()
  {
    // NURU_S004
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {script?} {*args}")
    );

    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<MixedCatchAllWithOptionalError>();

    await Task.CompletedTask;
  }

  public static async Task Should_report_optional_before_required()
  {
    // NURU_S006
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {arg?} {script}")
    );

    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<OptionalBeforeRequiredError>();

    await Task.CompletedTask;
  }

  public static async Task Should_report_invalid_end_of_options()
  {
    // NURU_S007
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run --")
    );

    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_report_options_after_end_of_options()
  {
    // NURU_S008
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run -- {*args} --verbose")
    );

    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  // Parse Errors (NURU_P###)

  public static async Task Should_reject_invalid_parameter_syntax()
  {
    // NURU_P001 - Angle brackets not supported
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("prompt <input>")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_unbalanced_braces()
  {
    // NURU_P002
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("deploy {env")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_invalid_option_format()
  {
    // NURU_P003 - Triple dash not allowed
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("build ---config")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  // Edge Cases

  public static async Task Should_accept_empty_pattern()
  {
    // Empty pattern is valid - matches no arguments
    CompiledRoute route = PatternParser.Parse("");

    route.ShouldNotBeNull();
    route.Specificity.ShouldBe(0);
    route.PositionalMatchers.Count.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_normalize_whitespace_only_pattern()
  {
    // Whitespace-only treated as empty after normalization
    CompiledRoute route = PatternParser.Parse("   ");

    route.ShouldNotBeNull();
    route.Specificity.ShouldBe(0);
    route.PositionalMatchers.Count.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_single_literal()
  {
    // Minimal valid pattern
    CompiledRoute route = PatternParser.Parse("run");

    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(1);
    // TODO: Assert specificity once constants are extracted

    await Task.CompletedTask;
  }

  public static async Task Should_handle_single_parameter()
  {
    // Single parameter pattern
    CompiledRoute route = PatternParser.Parse("{cmd}");

    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(1);
    // TODO: Assert specificity once constants are extracted

    await Task.CompletedTask;
  }

  public static async Task Should_handle_single_catchall()
  {
    // Catch-all only pattern
    CompiledRoute route = PatternParser.Parse("{*args}");

    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.PositionalMatchers.Count.ShouldBe(1);
    // TODO: Assert specificity once constants are extracted

    await Task.CompletedTask;
  }
}
