#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
await TestRunner.RunTests<OptionModifiersTests>(clearCache: true);

[TestTag("Parser")]
[ClearRunfileCache]
public class OptionModifiersTests
{
  // Section 8: Option modifier combinations and parsing
  // Options are flags (--verbose, -v) that can optionally take parameters

  public static async Task Should_parse_boolean_flag()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --verbose");

    // Assert - Boolean flag (no parameter)
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher verboseOption = route.OptionMatchers[0];
    verboseOption.MatchPattern.ShouldBe("--verbose");
    verboseOption.ExpectsValue.ShouldBeFalse(); // Boolean flag

    await Task.CompletedTask;
  }

  public static async Task Should_parse_option_with_required_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --config {mode}");

    // Assert - Option with required (non-nullable) value
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher configOption = route.OptionMatchers[0];
    configOption.MatchPattern.ShouldBe("--config");
    configOption.ExpectsValue.ShouldBeTrue();
    configOption.ParameterName.ShouldBe("mode");
    configOption.IsOptional.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_option_with_optional_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --config {mode?}");

    // Assert - Option required, but VALUE is optional (nullable)
    // This is the correct way to make an option's value optional
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher configOption = route.OptionMatchers[0];
    configOption.MatchPattern.ShouldBe("--config");
    configOption.ExpectsValue.ShouldBeTrue();
    configOption.ParameterName.ShouldBe("mode");
    // Note: IsOptional is False - it refers to whether the option itself can be omitted

    await Task.CompletedTask;
  }

  public static async Task Should_parse_short_option_flag()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build -v");

    // Assert - Short form boolean flag
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher vOption = route.OptionMatchers[0];
    vOption.MatchPattern.ShouldBe("-v");
    vOption.ExpectsValue.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_short_option_with_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build -c {mode}");

    // Assert - Short form with value parameter
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher cOption = route.OptionMatchers[0];
    cOption.MatchPattern.ShouldBe("-c");
    cOption.ExpectsValue.ShouldBeTrue();
    cOption.ParameterName.ShouldBe("mode");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_option_with_alias()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --verbose,-v");

    // Assert - Aliases stored as MatchPattern (primary/long) + AlternateForm (short)
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher verboseOption = route.OptionMatchers[0];
    verboseOption.MatchPattern.ShouldBe("--verbose");
    verboseOption.AlternateForm.ShouldBe("-v");
    verboseOption.ExpectsValue.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_option_alias_with_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --config,-c {mode}");

    // Assert - Aliased option with parameter
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher configOption = route.OptionMatchers[0];
    configOption.MatchPattern.ShouldBe("--config");
    configOption.AlternateForm.ShouldBe("-c");
    configOption.ExpectsValue.ShouldBeTrue();
    configOption.ParameterName.ShouldBe("mode");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multiple_options()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("deploy --verbose --force --config {env}");

    // Assert - Multiple options: 2 boolean, 1 parameterized
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(3);

    OptionMatcher verboseOption = route.OptionMatchers[0];
    verboseOption.MatchPattern.ShouldBe("--verbose");
    verboseOption.ExpectsValue.ShouldBeFalse();

    OptionMatcher forceOption = route.OptionMatchers[1];
    forceOption.MatchPattern.ShouldBe("--force");
    forceOption.ExpectsValue.ShouldBeFalse();

    OptionMatcher configOption = route.OptionMatchers[2];
    configOption.MatchPattern.ShouldBe("--config");
    configOption.ExpectsValue.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_typed_option_value()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("server --port {num:int}");

    // Assert - Option with typed parameter
    // Type information is resolved during parameter binding, not stored on OptionMatcher
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher portOption = route.OptionMatchers[0];
    portOption.MatchPattern.ShouldBe("--port");
    portOption.ExpectsValue.ShouldBeTrue();
    portOption.ParameterName.ShouldBe("num");

    await Task.CompletedTask;
  }
}
