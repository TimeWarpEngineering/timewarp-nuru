#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
return await RunTests<ConsecutiveOptionalParametersTests>(clearCache: true);

[TestTag("Parser")]
[ClearRunfileCache]
public class ConsecutiveOptionalParametersTests
{
  // NURU_S002: Only ONE optional positional parameter is allowed
  // Consecutive optional parameters create ambiguity in parsing

  public static async Task Should_allow_single_optional_at_end()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("deploy {env} {tag?}");

    // Assert - Valid pattern: only one optional parameter
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(3); // "deploy" + {env} + {tag?}

    ParameterMatcher envParam = (ParameterMatcher)route.PositionalMatchers[1];
    envParam.Name.ShouldBe("env");
    envParam.IsOptional.ShouldBeFalse();

    ParameterMatcher tagParam = (ParameterMatcher)route.PositionalMatchers[2];
    tagParam.Name.ShouldBe("tag");
    tagParam.IsOptional.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_two_consecutive_optionals()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("deploy {env?} {version?}")
    );

    // Assert - NURU_S002: Consecutive optionals create ambiguity
    // Example: "deploy v2.0" - is v2.0 the env or version?
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<ConflictingOptionalParametersError>();

    ConflictingOptionalParametersError error = (ConflictingOptionalParametersError)exception.SemanticErrors[0];
    error.ConflictingParameters.ShouldContain("env");
    error.ConflictingParameters.ShouldContain("version");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_three_consecutive_optionals()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {script?} {arg1?} {arg2?}")
    );

    // Assert - Extreme ambiguity with three optionals
    // Note: Validator reports the first conflicting PAIR (script, arg1)
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<ConflictingOptionalParametersError>();

    ConflictingOptionalParametersError error = (ConflictingOptionalParametersError)exception.SemanticErrors[0];
    error.ConflictingParameters.ShouldContain("script");
    error.ConflictingParameters.ShouldContain("arg1");
    // arg2 not included - validator stops at first conflicting pair

    await Task.CompletedTask;
  }

  public static async Task Should_allow_single_optional_only()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("status {format?}");

    // Assert - Valid: only one parameter, can be optional
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(2); // "status" + {format?}

    ParameterMatcher formatParam = (ParameterMatcher)route.PositionalMatchers[1];
    formatParam.Name.ShouldBe("format");
    formatParam.IsOptional.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_optional_before_required()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {script} {arg?} {timeout}")
    );

    // Assert - NURU_S006: Optional before required is invalid
    // This is caught by a different validation rule
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<OptionalBeforeRequiredError>();

    OptionalBeforeRequiredError optError = (OptionalBeforeRequiredError)exception.SemanticErrors[0];
    optError.OptionalParam.ShouldBe("arg");
    optError.RequiredParam.ShouldBe("timeout");

    await Task.CompletedTask;
  }

  public static async Task Should_allow_required_then_optional()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("backup {source} {dest?}");

    // Assert - Valid pattern: required parameter followed by single optional
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(3);

    ParameterMatcher sourceParam = (ParameterMatcher)route.PositionalMatchers[1];
    sourceParam.IsOptional.ShouldBeFalse();

    ParameterMatcher destParam = (ParameterMatcher)route.PositionalMatchers[2];
    destParam.IsOptional.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_typed_consecutive_optionals()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("process {id:int?} {priority:int?}")
    );

    // Assert - Type constraints don't change the rule
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<ConflictingOptionalParametersError>();

    ConflictingOptionalParametersError error = (ConflictingOptionalParametersError)exception.SemanticErrors[0];
    error.ConflictingParameters.ShouldContain("id");
    error.ConflictingParameters.ShouldContain("priority");

    await Task.CompletedTask;
  }
}
