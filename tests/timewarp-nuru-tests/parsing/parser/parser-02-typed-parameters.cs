#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
return await RunTests<TypedParameterParsingTests>(clearCache: true);

[TestTag("Parser")]
[ClearRunfileCache]
public class TypedParameterParsingTests
{
  public static async Task Should_parse_int_typed_parameter()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("delay {ms:int}");

    // Assert - Verify route compiles successfully
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(2); // "delay" literal + {ms:int} parameter
    route.OptionMatchers.Count.ShouldBe(0);

    // Verify parameter segment with type constraint
    route.PositionalMatchers[1].ShouldBeOfType<ParameterMatcher>();
    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("ms");
    param.Constraint.ShouldBe("int");
    param.IsOptional.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_double_typed_parameter()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("calculate {value:double}");

    // Assert - Verify type constraint captured
    route.ShouldNotBeNull();
    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("value");
    param.Constraint.ShouldBe("double");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_datetime_typed_parameter()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("schedule {when:DateTime}");

    // Assert - Verify DateTime type (Pascal case)
    route.ShouldNotBeNull();
    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("when");
    param.Constraint.ShouldBe("DateTime");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multiple_typed_parameters()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("range {start:int} {end:int}");

    // Assert - Verify both parameters have types
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(3); // "range" + {start:int} + {end:int}

    ParameterMatcher startParam = (ParameterMatcher)route.PositionalMatchers[1];
    startParam.Name.ShouldBe("start");
    startParam.Constraint.ShouldBe("int");

    ParameterMatcher endParam = (ParameterMatcher)route.PositionalMatchers[2];
    endParam.Name.ShouldBe("end");
    endParam.Constraint.ShouldBe("int");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_typed_parameter_with_description()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("wait {ms:int|Milliseconds to wait}");

    // Assert - Verify both type and description captured
    route.ShouldNotBeNull();
    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("ms");
    param.Constraint.ShouldBe("int");
    param.Description.ShouldBe("Milliseconds to wait");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_mixed_typed_and_untyped_parameters()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("deploy {env} {replicas:int}");

    // Assert - Verify one has type, one doesn't
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(3);

    ParameterMatcher envParam = (ParameterMatcher)route.PositionalMatchers[1];
    envParam.Name.ShouldBe("env");
    envParam.Constraint.ShouldBeNull(); // No type constraint

    ParameterMatcher replicasParam = (ParameterMatcher)route.PositionalMatchers[2];
    replicasParam.Name.ShouldBe("replicas");
    replicasParam.Constraint.ShouldBe("int");

    await Task.CompletedTask;
  }
}
