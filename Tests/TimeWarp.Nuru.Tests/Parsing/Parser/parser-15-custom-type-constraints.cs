#!/usr/bin/dotnet --

// Test custom type constraints after parser fix for issue #62
return await RunTests<CustomTypeConstraintParsingTests>(clearCache: true);

[TestTag("Parser")]
[ClearRunfileCache]
public class CustomTypeConstraintParsingTests
{
  public static async Task Should_parse_custom_type_constraint_lowercase()
  {
    // Arrange & Act - Custom type constraint like "fileinfo"
    CompiledRoute route = PatternParser.Parse("process {file:fileinfo}");

    // Assert - Parser should accept custom type constraints
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(2); // "process" literal + {file:fileinfo} parameter
    route.OptionMatchers.Count.ShouldBe(0);

    // Verify parameter segment with custom type constraint
    route.PositionalMatchers[1].ShouldBeOfType<ParameterMatcher>();
    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("file");
    param.Constraint.ShouldBe("fileinfo");
    param.IsOptional.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_custom_type_constraint_pascalcase()
  {
    // Arrange & Act - Custom type with PascalCase naming
    CompiledRoute route = PatternParser.Parse("load {data:MyCustomType}");

    // Assert
    route.ShouldNotBeNull();
    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("data");
    param.Constraint.ShouldBe("MyCustomType");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_custom_type_constraint_with_underscores()
  {
    // Arrange & Act - Custom type with underscores
    CompiledRoute route = PatternParser.Parse("parse {input:my_custom_type}");

    // Assert
    route.ShouldNotBeNull();
    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("input");
    param.Constraint.ShouldBe("my_custom_type");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_custom_type_constraint_starting_with_underscore()
  {
    // Arrange & Act - Custom type starting with underscore (valid C# identifier)
    CompiledRoute route = PatternParser.Parse("handle {value:_internal}");

    // Assert
    route.ShouldNotBeNull();
    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("value");
    param.Constraint.ShouldBe("_internal");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_custom_type_constraint_with_numbers()
  {
    // Arrange & Act - Custom type with numbers (e.g., ipv4address)
    CompiledRoute route = PatternParser.Parse("connect {addr:ipv4address}");

    // Assert
    route.ShouldNotBeNull();
    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("addr");
    param.Constraint.ShouldBe("ipv4address");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_custom_type_starting_with_number()
  {
    // Arrange & Act - Invalid: type constraint starting with number
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("invalid {x:123type}")
    );

    // Assert - Should have parse error
    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);
    exception.ParseErrors[0].ToString().ShouldContain("123type");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_custom_type_with_special_characters()
  {
    // Arrange & Act - Invalid: type constraint with special characters (hyphen not allowed)
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("invalid {x:my-type}")
    );

    // Assert - Should have parse error
    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multiple_custom_type_constraints()
  {
    // Arrange & Act - Multiple custom types in one route
    CompiledRoute route = PatternParser.Parse("copy {source:fileinfo} {dest:directoryinfo}");

    // Assert - Both custom types should be accepted
    route.ShouldNotBeNull();
    route.PositionalMatchers.Count.ShouldBe(3); // "copy" + {source:fileinfo} + {dest:directoryinfo}

    ParameterMatcher sourceParam = (ParameterMatcher)route.PositionalMatchers[1];
    sourceParam.Name.ShouldBe("source");
    sourceParam.Constraint.ShouldBe("fileinfo");

    ParameterMatcher destParam = (ParameterMatcher)route.PositionalMatchers[2];
    destParam.Name.ShouldBe("dest");
    destParam.Constraint.ShouldBe("directoryinfo");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_custom_type_with_optional_modifier()
  {
    // Arrange & Act - Custom type with optional modifier
    CompiledRoute route = PatternParser.Parse("load {config:fileinfo?}");

    // Assert
    route.ShouldNotBeNull();
    ParameterMatcher param = (ParameterMatcher)route.PositionalMatchers[1];
    param.Name.ShouldBe("config");
    param.Constraint.ShouldBe("fileinfo?");
    param.IsOptional.ShouldBeTrue();

    await Task.CompletedTask;
  }

}
