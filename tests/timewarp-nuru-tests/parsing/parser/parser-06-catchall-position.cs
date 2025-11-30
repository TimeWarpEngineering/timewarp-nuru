#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
return await RunTests<CatchAllPositionTests>(clearCache: true);

[TestTag("Parser")]
[ClearRunfileCache]
public class CatchAllPositionTests
{
  // NURU_S003: Catch-all parameters must be the LAST positional parameter
  // Catch-all consumes all remaining arguments, so nothing can follow

  public static async Task Should_allow_catchall_at_end()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("docker run {*args}");

    // Assert - Valid: catch-all is last positional
    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("args");
    route.PositionalMatchers.Count.ShouldBe(3); // "docker", "run", {*args}

    await Task.CompletedTask;
  }

  public static async Task Should_allow_parameters_then_catchall()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("execute {script} {*args}");

    // Assert - Valid: regular param then catch-all
    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("args");
    route.PositionalMatchers.Count.ShouldBe(3); // "execute", {script}, {*args}

    ParameterMatcher scriptParam = (ParameterMatcher)route.PositionalMatchers[1];
    scriptParam.Name.ShouldBe("script");
    scriptParam.IsCatchAll.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_catchall_before_parameter()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {*args} {script}")
    );

    // Assert - NURU_S003: Catch-all must be last
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<CatchAllNotAtEndError>();

    CatchAllNotAtEndError error = (CatchAllNotAtEndError)exception.SemanticErrors[0];
    error.CatchAllParameter.ShouldBe("args");
    error.FollowingSegment.ShouldContain("script");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_catchall_in_middle()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {script} {*args} {timeout}")
    );

    // Assert - NURU_S003: Nothing can follow catch-all
    // Catch-all consumes all remaining args, leaving nothing for timeout
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<CatchAllNotAtEndError>();

    CatchAllNotAtEndError error = (CatchAllNotAtEndError)exception.SemanticErrors[0];
    error.CatchAllParameter.ShouldBe("args");
    error.FollowingSegment.ShouldContain("timeout");

    await Task.CompletedTask;
  }

  public static async Task Should_allow_catchall_with_options_after()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("docker run {*args} --verbose");

    // Assert - Valid: Options can appear after catch-all
    // Options are parsed by prefix (-- or -), not positionally
    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("args");
    route.OptionMatchers.Count.ShouldBe(1);

    OptionMatcher verboseOption = route.OptionMatchers[0];
    verboseOption.MatchPattern.ShouldBe("--verbose");

    await Task.CompletedTask;
  }

  public static async Task Should_allow_multiple_params_then_catchall()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("copy {source} {dest} {*options}");

    // Assert - Valid: multiple params then catch-all
    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.PositionalMatchers.Count.ShouldBe(4); // "copy", {source}, {dest}, {*options}

    ParameterMatcher sourceParam = (ParameterMatcher)route.PositionalMatchers[1];
    sourceParam.Name.ShouldBe("source");
    sourceParam.IsCatchAll.ShouldBeFalse();

    ParameterMatcher destParam = (ParameterMatcher)route.PositionalMatchers[2];
    destParam.Name.ShouldBe("dest");
    destParam.IsCatchAll.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_catchall_before_literal()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {*args} to server")
    );

    // Assert - NURU_S003: Literal segments also can't follow catch-all
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<CatchAllNotAtEndError>();

    CatchAllNotAtEndError error = (CatchAllNotAtEndError)exception.SemanticErrors[0];
    error.CatchAllParameter.ShouldBe("args");
    error.FollowingSegment.ShouldContain("to");

    await Task.CompletedTask;
  }
}
