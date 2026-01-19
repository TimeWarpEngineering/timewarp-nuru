#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#pragma warning disable RCS1163 // Unused parameter - parameters must match route pattern names for binding

#region Purpose
// Tests per-route help support (Task #356).
// Validates "command --help" shows command-specific help instead of full app help.
// Covers: simple commands, grouped commands, options display, parameter sections.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class PerRouteHelpTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PerRouteHelpTests>();

  public static async Task Should_show_route_specific_help_for_simple_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy {env}").WithHandler((string env) => "Deployed to " + env).WithDescription("Deploy to an environment").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("deploy").ShouldBeTrue();
    terminal.OutputContains("env").ShouldBeTrue();
    terminal.OutputContains("Deploy to an environment").ShouldBeTrue();
  }

  public static async Task Should_show_route_specific_help_with_short_form()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("build {project}").WithHandler((string project) => "Built " + project).WithDescription("Build a project").Done()
      .Build();

    // Act - using -h instead of --help
    int exitCode = await app.RunAsync(["build", "-h"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("build").ShouldBeTrue();
    terminal.OutputContains("project").ShouldBeTrue();
    terminal.OutputContains("Build a project").ShouldBeTrue();
  }

  public static async Task Should_show_full_app_help_for_standalone_help_flag()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .WithName("myapp")
      .Map("deploy {env}").WithHandler((string env) => "Deployed").WithDescription("Deploy command").Done()
      .Map("build {project}").WithHandler((string project) => "Built").WithDescription("Build command").Done()
      .Build();

    // Act - standalone --help
    int exitCode = await app.RunAsync(["--help"]);

    // Assert - should show all commands
    exitCode.ShouldBe(0);
    terminal.OutputContains("myapp").ShouldBeTrue();
    terminal.OutputContains("deploy").ShouldBeTrue();
    terminal.OutputContains("build").ShouldBeTrue();
  }

  public static async Task Should_show_grouped_command_help()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .WithGroupPrefix("config")
        .Map("set {key} {value}").WithHandler((string key, string value) => "Set " + key + "=" + value).WithDescription("Set a config value").Done()
        .Map("get {key}").WithHandler((string key) => "Got " + key).WithDescription("Get a config value").Done()
      .Done()
      .Build();

    // Act - help for grouped command
    int exitCode = await app.RunAsync(["config", "set", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("config").ShouldBeTrue();
    terminal.OutputContains("set").ShouldBeTrue();
    terminal.OutputContains("key").ShouldBeTrue();
    terminal.OutputContains("value").ShouldBeTrue();
    terminal.OutputContains("Set a config value").ShouldBeTrue();
  }

  public static async Task Should_show_options_in_route_specific_help()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy {env} --force,-f? --dry-run,-d?").WithHandler((string env, bool force, bool dryRun) => "done").WithDescription("Deploy with options").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("deploy").ShouldBeTrue();
    terminal.OutputContains("env").ShouldBeTrue();
    terminal.OutputContains("--force").ShouldBeTrue();
    terminal.OutputContains("-f").ShouldBeTrue();
    terminal.OutputContains("--dry-run").ShouldBeTrue();
    terminal.OutputContains("-d").ShouldBeTrue();
  }

  public static async Task Should_show_parameters_section_in_help()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("copy {source} {destination}").WithHandler((string source, string destination) => "copied").WithDescription("Copy files").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["copy", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Parameters").ShouldBeTrue();
    terminal.OutputContains("source").ShouldBeTrue();
    terminal.OutputContains("destination").ShouldBeTrue();
  }

  public static async Task Should_show_options_section_in_help()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("run --verbose?").WithHandler((bool verbose) => "ran").WithDescription("Run something").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["run", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Options").ShouldBeTrue();
    terminal.OutputContains("--verbose").ShouldBeTrue();
  }

  public static async Task Should_not_execute_handler_when_help_requested()
  {
    // Arrange - Use a static handler that sets a flag
    // Reset the flag
    StaticFlags.DangerousHandlerExecuted = false;

    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("dangerous").WithHandler(StaticFlags.DangerousHandler).WithDescription("A dangerous command").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["dangerous", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    StaticFlags.DangerousHandlerExecuted.ShouldBeFalse();
  }

  [Skip("Design question: should 'deploy --help' show help for BOTH routes? Consider using {env?} or groups instead. See kanban #370")]
  public static async Task Should_show_help_for_multiple_routes_with_same_prefix()
  {
    #region Purpose
    // Design question: When two routes share a prefix (deploy, deploy {env}),
    // what should "deploy --help" show?
    // Options: (1) Both routes, (2) Use optional param {env?}, (3) Use group prefix
    // Current behavior: matches deploy {env} and shows help for that route only.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy").WithHandler(() => "simple deploy").WithDescription("Simple deploy").Done()
      .Map("deploy {env}").WithHandler((string env) => "deploy to " + env).WithDescription("Deploy to environment").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--help"]);

    // Assert - should show help for BOTH routes (design TBD)
    exitCode.ShouldBe(0);
    terminal.OutputContains("Simple deploy").ShouldBeTrue();
    terminal.OutputContains("Deploy to environment").ShouldBeTrue();
  }
}

// Static class to hold handler flags (avoiding lambda closures)
public static class StaticFlags
{
  public static bool DangerousHandlerExecuted { get; set; }

  public static string DangerousHandler()
  {
    DangerousHandlerExecuted = true;
    return "executed";
  }
}

} // namespace TimeWarp.Nuru.Tests.Help
