#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
return await RunTests<ErrorReportingTests>(clearCache: true);

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

  // Modifier Syntax Errors

  public static async Task Should_reject_double_question_mark_on_flag()
  {
    // Duplicate optional modifier on flag
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("--flag??")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_double_asterisk_on_option_value()
  {
    // Duplicate array modifier on option parameter
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("--env {var}**")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_question_mark_before_option()
  {
    // Modifier in wrong position
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("?--flag")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_question_mark_inside_option_name()
  {
    // Modifier in wrong position (middle of flag name)
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("--?flag")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_asterisk_before_option()
  {
    // Modifier in wrong position
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("*--env {var}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_asterisk_on_flag_name()
  {
    // Array modifier on boolean flag (not allowed)
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("--env* {var}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_question_mark_in_short_option()
  {
    // Modifier in wrong position
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("-?v")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_wrong_modifier_order_on_flag()
  {
    // Should be ?* not *? (though both are likely invalid)
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("--flag*?")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_wrong_modifier_order_on_option_value()
  {
    // Array modifier should come before optional modifier
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("--env {var}*?")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_wrong_modifier_order_in_parameter()
  {
    // Should be {env*?} not {env?*} - optional before catch-all
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("deploy {env?*}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_question_mark_before_parameter_name()
  {
    // Modifier in wrong position (before name)
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("build {?target}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_asterisk_with_question_mark_before_name()
  {
    // Multiple modifiers in wrong position
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {*?args}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_catchall_in_option_value()
  {
    // Catch-all parameter cannot be used as option value
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("--env? {*var}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_question_mark_inside_parameter()
  {
    // Question mark must be after parameter name, not inside braces
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("--flag {?param}")
    );

    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_modifier_after_closing_brace()
  {
    // Modifiers must be inside braces or on option names
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("test {param}? --flag")
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
