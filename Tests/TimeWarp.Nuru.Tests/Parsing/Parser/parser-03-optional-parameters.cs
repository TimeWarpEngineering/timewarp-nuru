#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
return await RunTests<OptionalParameterParsingTests>(clearCache: true);

[TestTag("Parser")]
[ClearRunfileCache]
public class OptionalParameterParsingTests
{
  public static async Task Should_parse_single_optional_parameter()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("greet {name?}");

    // Assert - Verify route compiles successfully
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(2); // "greet" literal + {name?} parameter
    route.OptionMatchers.Count.ShouldBe(0);

    // Verify optional parameter
    route.PositionalMatchers[1].ShouldBeOfType<ParameterMatcher>();
    var param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("name");
    param.IsOptional.ShouldBeTrue();
    param.IsCatchAll.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_optional_typed_parameter()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("delay {ms:int?}");

    // Assert - Verify both optional and typed
    route.ShouldNotBeNull();
    var param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("ms");
    param.Constraint.ShouldBe("int?"); // Constraint includes the ? marker
    param.IsOptional.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_required_parameter_before_optional()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("deploy {env} {tag?}");

    // Assert - Verify required then optional ordering
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(3); // "deploy" + {env} + {tag?}

    var envParam = (ParameterMatcher)route.PositionalMatchers[1];
    envParam.Name.ShouldBe("env");
    envParam.IsOptional.ShouldBeFalse();

    var tagParam = (ParameterMatcher)route.PositionalMatchers[2];
    tagParam.Name.ShouldBe("tag");
    tagParam.IsOptional.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_consecutive_optional_parameters()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("configure {env?} {region?}")
    );

    // Assert - Consecutive optional parameters create ambiguity
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<ConflictingOptionalParametersError>();

    var error = (ConflictingOptionalParametersError)exception.SemanticErrors[0];
    error.ConflictingParameters.ShouldContain("env");
    error.ConflictingParameters.ShouldContain("region");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_optional_parameter_with_description()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("greet {name?|Person to greet}");

    // Assert - Verify optional with description
    route.ShouldNotBeNull();
    var param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("name");
    param.IsOptional.ShouldBeTrue();
    param.Description.ShouldBe("Person to greet");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_optional_typed_parameter_with_description()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("wait {ms:int?|Milliseconds to wait}");

    // Assert - Verify all three attributes
    route.ShouldNotBeNull();
    var param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("ms");
    param.Constraint.ShouldBe("int?"); // Constraint includes the ? marker
    param.IsOptional.ShouldBeTrue();
    param.Description.ShouldBe("Milliseconds to wait");

    await Task.CompletedTask;
  }
}
