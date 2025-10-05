#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
await TestRunner.RunTests<CatchAllOptionalConflictTests>(clearCache: true);

[TestTag("Parser")]
[ClearRunfileCache]
public class CatchAllOptionalConflictTests
{
  // NURU_S004: Optional parameters and catch-all parameters CANNOT coexist
  // The ambiguity is irresolvable: where does optional end and catch-all begin?

  public static async Task Should_reject_optional_before_catchall()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {script?} {*args}")
    );

    // Assert - NURU_S004: Cannot mix optional with catch-all
    // Example ambiguity: "run myfile.sh arg1 arg2"
    //   - Is myfile.sh the script or first catch-all arg?
    //   - Are arg1/arg2 the catch-all, or is everything catch-all?
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<MixedCatchAllWithOptionalError>();

    var error = (MixedCatchAllWithOptionalError)exception.SemanticErrors[0];
    error.CatchAllParam.ShouldBe("args");
    error.OptionalParams.ShouldContain("script");

    await Task.CompletedTask;
  }

  public static async Task Should_allow_required_before_catchall()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("execute {script} {*args}");

    // Assert - Valid: Required parameters work fine with catch-all
    // No ambiguity - script is always consumed first
    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.PositionalMatchers.Count.ShouldBe(3);

    var scriptParam = (ParameterMatcher)route.PositionalMatchers[1];
    scriptParam.Name.ShouldBe("script");
    scriptParam.IsOptional.ShouldBeFalse();
    scriptParam.IsCatchAll.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_allow_catchall_only()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("passthrough {*args}");

    // Assert - Valid: No optional parameters, no conflict
    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.CatchAllParameterName.ShouldBe("args");
    route.PositionalMatchers.Count.ShouldBe(2); // "passthrough", {*args}

    await Task.CompletedTask;
  }

  public static async Task Should_reject_multiple_params_with_optional_and_catchall()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {cmd} {arg?} {*rest}")
    );

    // Assert - NURU_S004: Even with required params, optional + catch-all = conflict
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<MixedCatchAllWithOptionalError>();

    var error = (MixedCatchAllWithOptionalError)exception.SemanticErrors[0];
    error.CatchAllParam.ShouldBe("rest");
    error.OptionalParams.ShouldContain("arg");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_typed_optional_with_catchall()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("process {id:int?} {*args}")
    );

    // Assert - Type constraints don't change the rule
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<MixedCatchAllWithOptionalError>();

    var error = (MixedCatchAllWithOptionalError)exception.SemanticErrors[0];
    error.CatchAllParam.ShouldBe("args");
    error.OptionalParams.ShouldContain("id");

    await Task.CompletedTask;
  }

  public static async Task Should_allow_multiple_required_with_catchall()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("copy {source} {dest} {*options}");

    // Assert - Valid: Multiple required parameters work fine with catch-all
    route.ShouldNotBeNull();
    route.HasCatchAll.ShouldBeTrue();
    route.PositionalMatchers.Count.ShouldBe(4);

    var sourceParam = (ParameterMatcher)route.PositionalMatchers[1];
    sourceParam.IsOptional.ShouldBeFalse();

    var destParam = (ParameterMatcher)route.PositionalMatchers[2];
    destParam.IsOptional.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_optional_catchall_syntax()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("deploy {env?} {*files}")
    );

    // Assert - Even if optional is explicitly marked, still conflicts with catch-all
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<MixedCatchAllWithOptionalError>();

    var error = (MixedCatchAllWithOptionalError)exception.SemanticErrors[0];
    error.CatchAllParam.ShouldBe("files");
    error.OptionalParams.ShouldContain("env");

    await Task.CompletedTask;
  }
}
