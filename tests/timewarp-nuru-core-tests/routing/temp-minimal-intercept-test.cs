#!/usr/bin/dotnet --

// V2 Generator Phase 6: Minimal Intercept Tests
// Tests the V2 source generator end-to-end with minimal test cases
// Reference: kanban/in-progress/272-v2-generator-phase-6-testing.md

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.V2.Minimal
{

/// <summary>
/// Minimal tests for V2 generator interceptor functionality.
/// These tests verify that the source generator correctly intercepts
/// RunAsync and routes are matched/executed.
/// </summary>
[TestTag("V2Generator")]
public class MinimalInterceptTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MinimalInterceptTests>();

  /// <summary>
  /// Test that a single route with no parameters is intercepted and executed.
  /// This is the most basic test case for the V2 generator.
  /// </summary>
  public static async Task Should_intercept_single_route()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("ping")
        .WithHandler(() => "pong")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["ping"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("pong").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that unknown commands return an error exit code.
  /// </summary>
  public static async Task Should_return_error_for_unknown_command()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("ping")
        .WithHandler(() => "pong")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["unknown"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that multiple routes can be registered and matched correctly.
  /// </summary>
  public static async Task Should_match_correct_route_among_multiple()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("ping")
        .WithHandler(() => "pong")
        .AsQuery()
        .Done()
      .Map("status")
        .WithHandler(() => "healthy")
        .AsQuery()
        .Done()
      .Map("version")
        .WithHandler(() => "1.0.0")
        .AsQuery()
        .Done()
      .Build();

    // Act - test second route
    int exitCode = await app.RunAsync(["status"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("healthy").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that --version flag is handled (built-in route).
  /// </summary>
  public static async Task Should_handle_version_flag()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("ping")
        .WithHandler(() => "pong")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--version"]);

    // Assert - version should be printed
    exitCode.ShouldBe(0);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Generator.V2.Minimal
