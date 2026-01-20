// test-real-app.cs - Test harness for real-app.cs using Jaribu
// This file is included at build time via Directory.Build.props when NURU_TEST is set
// Usage: NURU_TEST=test-real-app.cs ./real-app.cs
//
// The ModuleInitializer sets up NuruTestContext.TestRunner before Main() runs.
// When real-app.cs calls RunAsync(), control is handed to our test runner.

using System.Runtime.CompilerServices;
using Shouldly;
using TimeWarp.Jaribu;
using TimeWarp.Nuru;
using static TimeWarp.Jaribu.TestRunner;

public static class TestHarness
{
  internal static NuruApp? App;

  [ModuleInitializer]
  public static void Initialize()
  {
    NuruTestContext.TestRunner = async (app) =>
    {
      App = app;  // Capture the real app
      return await RunTests<RealAppTests>(clearCache: false);
    };
  }
}

[TestTag("RealApp")]
public class RealAppTests
{
  public static async Task CleanUp()
  {
    // Reset terminal context after each test
    TestTerminalContext.Current = null;
    await Task.CompletedTask;
  }

  public static async Task Should_greet_with_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    TestTerminalContext.Current = terminal;

    // Act
    await TestHarness.App!.RunAsync(["greet", "World"]);

    // Assert
    terminal.OutputContains("Hello, World!").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_deploy_with_dry_run_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    TestTerminalContext.Current = terminal;

    // Act
    await TestHarness.App!.RunAsync(["deploy", "production", "--dry-run"]);

    // Assert
    terminal.OutputContains("[DRY RUN]").ShouldBeTrue();
    terminal.OutputContains("production").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_deploy_without_dry_run()
  {
    // Arrange
    using TestTerminal terminal = new();
    TestTerminalContext.Current = terminal;

    // Act
    await TestHarness.App!.RunAsync(["deploy", "staging"]);

    // Assert
    terminal.OutputContains("Deploying to staging").ShouldBeTrue();
    terminal.OutputContains("DRY RUN").ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_show_version()
  {
    // Arrange
    using TestTerminal terminal = new();
    TestTerminalContext.Current = terminal;

    // Act
    await TestHarness.App!.RunAsync(["version"]);

    // Assert
    terminal.OutputContains("RealApp v1.0.0").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_return_error_for_unknown_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    TestTerminalContext.Current = terminal;

    // Act
    int exitCode = await TestHarness.App!.RunAsync(["unknown-command"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("No matching command found").ShouldBeTrue();

    await Task.CompletedTask;
  }
}
