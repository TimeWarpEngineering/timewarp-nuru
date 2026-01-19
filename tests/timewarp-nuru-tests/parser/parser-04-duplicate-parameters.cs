#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Parser
{

[TestTag("Parser")]
public class DuplicateParameterValidationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DuplicateParameterValidationTests>();

  public static async Task Should_reject_duplicate_positional_parameters()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("copy {source} {source}")
    );

    // Assert - Verify semantic error for duplicate parameter
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors.Count.ShouldBe(1);
    exception.SemanticErrors[0].ShouldBeOfType<DuplicateParameterNamesError>();

    DuplicateParameterNamesError error = (DuplicateParameterNamesError)exception.SemanticErrors[0];
    error.ParameterName.ShouldBe("source");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_duplicate_parameters_with_different_types()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("range {value:int} {value:double}")
    );

    // Assert - Still a duplicate even with different types
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<DuplicateParameterNamesError>();

    DuplicateParameterNamesError error = (DuplicateParameterNamesError)exception.SemanticErrors[0];
    error.ParameterName.ShouldBe("value");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_duplicate_option_parameters()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("build --config {cfg} --mode {cfg}")
    );

    // Assert - Duplicate in options
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<DuplicateParameterNamesError>();

    DuplicateParameterNamesError error = (DuplicateParameterNamesError)exception.SemanticErrors[0];
    error.ParameterName.ShouldBe("cfg");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_duplicate_across_positional_and_option()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("deploy {env} --config {env}")
    );

    // Assert - Duplicate across positional and option
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<DuplicateParameterNamesError>();

    DuplicateParameterNamesError error = (DuplicateParameterNamesError)exception.SemanticErrors[0];
    error.ParameterName.ShouldBe("env");

    await Task.CompletedTask;
  }

  public static async Task Should_allow_same_name_in_different_routes()
  {
    // Arrange & Act - These are different routes, so same param name is OK
    CompiledRoute route1 = PatternParser.Parse("copy {source} {dest}");
    CompiledRoute route2 = PatternParser.Parse("move {source} {dest}");

    // Assert - Both should compile successfully
    route1.ShouldNotBeNull();
    route2.ShouldNotBeNull();

    ParameterMatcher route1Param = (ParameterMatcher)route1.PositionalMatchers[1];
    ParameterMatcher route2Param = (ParameterMatcher)route2.PositionalMatchers[1];

    route1Param.Name.ShouldBe("source");
    route2Param.Name.ShouldBe("source"); // Same name is fine in different routes

    await Task.CompletedTask;
  }

  public static async Task Should_reject_duplicate_with_optional_modifier()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("deploy {env} {env?}")
    );

    // Assert - Duplicate even when one is optional
    exception.SemanticErrors.ShouldNotBeNull();
    exception.SemanticErrors[0].ShouldBeOfType<DuplicateParameterNamesError>();

    DuplicateParameterNamesError error = (DuplicateParameterNamesError)exception.SemanticErrors[0];
    error.ParameterName.ShouldBe("env");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Parser
