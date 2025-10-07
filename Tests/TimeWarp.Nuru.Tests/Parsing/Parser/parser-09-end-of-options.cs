#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
return await RunTests<EndOfOptionsParserTests>(clearCache: true);

[TestTag("Parser")]
[ClearRunfileCache]
public class EndOfOptionsParserTests
{
  // Section 11: End-of-options separator (--) handling
  // The -- separator marks the end of options, treating everything after as literal arguments

  public static async Task Should_parse_end_of_options_with_catchall()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("run -- {*args}");

    // Assert - End-of-options separator followed by catch-all
    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("args");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_required_param_then_end_of_options()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("execute {script} -- {*args}");

    // Assert - Parameter before end-of-options separator
    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("args");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_end_of_options_without_catchall()
  {
    // Arrange & Act & Assert
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run --")
    );

    // End-of-options must be followed by catch-all parameter (NURU_S007)
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_options_after_end_of_options()
  {
    // Arrange & Act & Assert
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run -- {*args} --verbose")
    );

    // No options allowed after -- separator (NURU_S008)
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_allow_options_before_end_of_options()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("docker run --detach -- {*cmd}");

    // Assert - Options allowed before --, not after
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher detachOption = route.OptionMatchers[0];
    detachOption.MatchPattern.ShouldBe("--detach");
    route.HasCatchAll.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_non_catchall_after_end_of_options()
  {
    // Arrange & Act & Assert
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("exec -- {param}")
    );

    // Only catch-all allowed after --, not regular parameters
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_double_end_of_options()
  {
    // Arrange & Act & Assert
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("exec -- {*args} -- {*more}")
    );

    // Only one end-of-options separator allowed
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_parse_standalone_end_of_options()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("-- {*args}");

    // Assert - Just separator and catch-all, no command before
    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("args");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_optional_param_after_end_of_options()
  {
    // Arrange & Act & Assert
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("exec -- {param?}")
    );

    // Only catch-all allowed after --, not optional parameters
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_typed_param_after_end_of_options()
  {
    // Arrange & Act & Assert
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("exec -- {param:int}")
    );

    // Only catch-all allowed after --, not typed parameters
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_multiple_params_after_end_of_options()
  {
    // Arrange & Act & Assert
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("exec -- {*args} {other}")
    );

    // Only one catch-all allowed, nothing can follow it
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_standalone_separator_without_catchall()
  {
    // Arrange & Act & Assert
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("--")
    );

    // Standalone -- with no catch-all is invalid
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }
}
