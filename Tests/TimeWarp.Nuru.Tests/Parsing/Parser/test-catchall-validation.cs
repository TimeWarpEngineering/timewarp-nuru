#!/usr/bin/dotnet --
#:project ../../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj
#:project ../../../../Source/TimeWarp.Kijaribu/TimeWarp.Kijaribu.csproj

using TimeWarp.Nuru.Parsing;
using Shouldly;
using TimeWarp.Kijaribu;

await TestRunner.RunTests<CatchAllValidationTests>();

public class CatchAllValidationTests
{
  public static async Task CatchAllInOptionParameterShouldBeRejected()
  {
    // Act & Assert
    RoutePatternException ex = Should.Throw<RoutePatternException>(() =>
    {
      CompiledRoute route = RoutePatternParser.Parse("test --exclude {*pattern}");
    });

    // Verify exception has errors
    bool hasErrors = (ex.ParseErrors?.Count > 0) || (ex.SemanticErrors?.Count > 0);
    hasErrors.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task RepeatedOptionParameterShouldSucceed()
  {
    // Act
    CompiledRoute route = RoutePatternParser.Parse("test --exclude {pattern}*");

    // Assert
    route.ShouldNotBeNull();
    await Task.CompletedTask;
  }

  public static async Task OptionalCatchAllSyntaxShouldSucceed()
  {
    // Act
    CompiledRoute route = RoutePatternParser.Parse("git add {*files?}");

    // Assert
    route.ShouldNotBeNull();
    await Task.CompletedTask;
  }
}