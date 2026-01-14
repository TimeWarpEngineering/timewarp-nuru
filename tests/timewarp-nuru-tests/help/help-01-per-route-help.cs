#!/usr/bin/dotnet --
#pragma warning disable RCS1163 // Unused parameter - parameters must match route pattern names for binding

// Task #356: Per-route help support
// Tests for "command --help" showing command-specific help instead of full app help

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

  public static async Task Should_match_route_with_matching_literal_prefix()
  {
    // Arrange - two routes with same literal prefix but different parameters
    // Routes are checked in specificity order (highest first), so the more
    // specific route (with parameters) is checked first
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy").WithHandler(() => "simple deploy").WithDescription("Simple deploy").Done()
      .Map("deploy {env}").WithHandler((string env) => "deploy to " + env).WithDescription("Deploy to environment").Done()
      .Build();

    // Act - "deploy --help" matches the first route that has "deploy" as literal prefix
    // Since routes are ordered by specificity, the parameterized route is checked first
    int exitCode = await app.RunAsync(["deploy", "--help"]);

    // Assert - should show help (either one is valid, as both have "deploy" prefix)
    exitCode.ShouldBe(0);
    terminal.OutputContains("deploy").ShouldBeTrue();
    // The description shown depends on which route matched first
    // Both routes have "deploy" as literal prefix, so either description is valid
  }
}

// Static class to hold handler flags (avoiding lambda closures)
public static class StaticFlags
{
  private static bool _dangerousHandlerExecuted;

  public static bool DangerousHandlerExecuted
  {
    get => _dangerousHandlerExecuted;
    set => _dangerousHandlerExecuted = value;
  }

  public static string DangerousHandler()
  {
    _dangerousHandlerExecuted = true;
    return "executed";
  }
}

} // namespace TimeWarp.Nuru.Tests.Help
