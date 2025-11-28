#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
return await RunTests<BasicParameterParsingTests>(clearCache: true);

[TestTag("Parser")]
[ClearRunfileCache]
public class BasicParameterParsingTests
{
  public static async Task Should_parse_single_required_parameter()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("greet {name}");

    // Assert - Verify route compiles successfully
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(2); // "greet" literal + {name} parameter
    route.OptionMatchers.Count.ShouldBe(0);

    // Verify literal segment
    route.PositionalMatchers[0].ShouldBeOfType<LiteralMatcher>();
    LiteralMatcher literal = (LiteralMatcher)route.PositionalMatchers[0];
    literal.Value.ShouldBe("greet");

    // Verify parameter segment
    route.PositionalMatchers[1].ShouldBeOfType<ParameterMatcher>();
    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("name");
    param.IsOptional.ShouldBeFalse();
    param.IsCatchAll.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multiple_required_parameters()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("copy {source} {dest}");

    // Assert - Verify structure
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(3); // "copy" + {source} + {dest}
    route.OptionMatchers.Count.ShouldBe(0);

    // Verify all segments present
    ((LiteralMatcher)route.PositionalMatchers[0]).Value.ShouldBe("copy");
    ((ParameterMatcher)route.PositionalMatchers[1]).Name.ShouldBe("source");
    ((ParameterMatcher)route.PositionalMatchers[2]).Name.ShouldBe("dest");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_mixed_literals_and_parameters()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("deploy {env} to {region}");

    // Assert - Verify interleaved literals and parameters
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(4); // "deploy" + {env} + "to" + {region}

    // Verify correct ordering
    ((LiteralMatcher)route.PositionalMatchers[0]).Value.ShouldBe("deploy");
    ((ParameterMatcher)route.PositionalMatchers[1]).Name.ShouldBe("env");
    ((LiteralMatcher)route.PositionalMatchers[2]).Value.ShouldBe("to");
    ((ParameterMatcher)route.PositionalMatchers[3]).Name.ShouldBe("region");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_parameter_with_description()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("greet {name|Person to greet}");

    // Assert - Verify description is captured
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(2);

    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("name");
    param.Description.ShouldBe("Person to greet");

    await Task.CompletedTask;
  }
}
