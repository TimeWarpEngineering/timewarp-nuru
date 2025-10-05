#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
return await RunTests<CatchAllValidationTests>(clearCache: true);

[TestTag("Parser")]
public class CatchAllValidationTests
{
  public static async Task CatchAllInOptionParameterShouldBeRejected()
  {
    // Act & Assert
    PatternException ex = Should.Throw<PatternException>(() =>
    {
      CompiledRoute route = PatternParser.Parse("test --exclude {*pattern}");
    });

    // Verify exception has errors
    bool hasErrors = (ex.ParseErrors?.Count > 0) || (ex.SemanticErrors?.Count > 0);
    hasErrors.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task RepeatedOptionParameterShouldSucceed()
  {
    // Act
    CompiledRoute route = PatternParser.Parse("test --exclude {pattern}*");

    // Assert
    route.ShouldNotBeNull();
    await Task.CompletedTask;
  }

  public static async Task OptionalCatchAllSyntaxShouldSucceed()
  {
    // Act
    CompiledRoute route = PatternParser.Parse("git add {*files?}");

    // Assert
    route.ShouldNotBeNull();
    await Task.CompletedTask;
  }
}