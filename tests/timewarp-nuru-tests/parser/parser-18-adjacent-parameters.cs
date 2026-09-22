#!/usr/bin/env -S dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Parser
{

[TestTag("Parser")]
public class AdjacentParametersTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AdjacentParametersTests>();

  public static async Task Should_error_on_adjacent_parameters_no_whitespace()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {a}{b}")
    );

    // Assert
    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.Count.ShouldBeGreaterThan(0);
    exception.ParseErrors.ShouldContain(e => e is AdjacentParametersError);

    AdjacentParametersError adjacent = exception.ParseErrors.OfType<AdjacentParametersError>().Single();
    adjacent.Position.ShouldBe(7);
    adjacent.Length.ShouldBe(3);

    await Task.CompletedTask;
  }

  public static async Task Should_allow_parameters_separated_by_space()
  {
    // Arrange & Act & Assert
    Should.NotThrow(() => PatternParser.Parse("run {a} {b}"));

    await Task.CompletedTask;
  }

  public static async Task Should_allow_parameters_separated_by_tab()
  {
    // Arrange & Act & Assert
    Should.NotThrow(() => PatternParser.Parse("run {a}\t{b}"));

    await Task.CompletedTask;
  }

  public static async Task Should_error_on_three_adjacent_parameters_reports_each_adjacency()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run {a}{b}{c}")
    );

    // Assert - each subsequent parameter after the first is adjacent to its predecessor
    exception.ParseErrors.ShouldNotBeNull();
    int adjacentErrorCount = exception.ParseErrors.Count(e => e is AdjacentParametersError);
    adjacentErrorCount.ShouldBeGreaterThanOrEqualTo(2);

    AdjacentParametersError[] adjacentErrors = exception.ParseErrors.OfType<AdjacentParametersError>().ToArray();
    adjacentErrors.Length.ShouldBe(2);
    adjacentErrors[0].Position.ShouldBe(7);
    adjacentErrors[0].Length.ShouldBe(3);
    adjacentErrors[1].Position.ShouldBe(10);
    adjacentErrors[1].Length.ShouldBe(3);

    await Task.CompletedTask;
  }

  public static async Task Should_error_on_adjacent_option_parameter_then_top_parameter()
  {
    // Arrange & Act
    PatternException exception = Should.Throw<PatternException>(() =>
      PatternParser.Parse("run --opt {a}{b}")
    );

    // Assert
    exception.ParseErrors.ShouldNotBeNull();
    exception.ParseErrors.ShouldContain(e => e is AdjacentParametersError);

    AdjacentParametersError adjacent = exception.ParseErrors.OfType<AdjacentParametersError>().Single();
    adjacent.Position.ShouldBe(13);
    adjacent.Length.ShouldBe(3);

    await Task.CompletedTask;
  }

  public static async Task Should_not_error_on_option_with_parameter_followed_by_spaced_parameter()
  {
    // Arrange & Act & Assert
    Should.NotThrow(() => PatternParser.Parse("run --opt {a} {b}"));

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Parser
