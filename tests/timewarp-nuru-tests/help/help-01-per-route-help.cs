#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
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
    NuruApp app = NuruApp.CreateBuilder()
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
    NuruApp app = NuruApp.CreateBuilder()
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
    NuruApp app = NuruApp.CreateBuilder()
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
    NuruApp app = NuruApp.CreateBuilder()
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
    NuruApp app = NuruApp.CreateBuilder()
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
    NuruApp app = NuruApp.CreateBuilder()
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

  public static async Task Should_show_parameter_descriptions_in_help()
  {
    // Issue #215 / kanban #453: Endpoint DSL [Parameter(Description)] must render
    // in per-route help (previously the Parameters table dropped the description).
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<ForkEndpoint>()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["fork", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Parameters").ShouldBeTrue();
    terminal.OutputContains("source").ShouldBeTrue();
    terminal.OutputContains("Description").ShouldBeTrue();
    terminal.OutputContains("Source repository identifier").ShouldBeTrue();
  }

  public static async Task Should_not_show_description_column_when_no_parameter_descriptions()
  {
    // Issue #215 / kanban #453: omit the Description column when no parameter has one.
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("move {source} {destination}")
        .WithHandler((string source, string destination) => "moved")
        .WithDescription("Move files")
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["move", "--help"]);

    // Assert - parameters render, but no Description column header appears
    exitCode.ShouldBe(0);
    terminal.OutputContains("Parameters").ShouldBeTrue();
    terminal.OutputContains("source").ShouldBeTrue();
    terminal.OutputContains("destination").ShouldBeTrue();
    terminal.OutputContains("Description").ShouldBeFalse();
  }

  public static async Task Should_show_options_section_in_help()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
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
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("dangerous").WithHandler(StaticFlags.DangerousHandler).WithDescription("A dangerous command").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["dangerous", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    StaticFlags.DangerousHandlerExecuted.ShouldBeFalse();
  }

  public static async Task Should_show_help_for_multiple_routes_with_same_prefix()
  {
    // Kanban #370: deploy --help lists every route whose leading literal is "deploy",
    // most specific first, in the same per-route layout.
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy")
        .WithHandler(() => "simple deploy")
        .WithDescription("Simple deploy")
        .WithExample("deploy", "Run a simple deploy")
        .Done()
      .Map("deploy {env} --force,-f?")
        .WithHandler((string env, bool force) => "deploy to " + env)
        .WithDescription("Deploy to environment")
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Simple deploy").ShouldBeTrue();
    terminal.OutputContains("Deploy to environment").ShouldBeTrue();
    terminal.OutputContains("deploy {env}").ShouldBeTrue();
    terminal.OutputContains("Parameters:").ShouldBeTrue();
    terminal.OutputContains("env").ShouldBeTrue();
    terminal.OutputContains("Options:").ShouldBeTrue();
    terminal.OutputContains("--force").ShouldBeTrue();
    terminal.OutputContains("-f").ShouldBeTrue();
    terminal.OutputContains("Examples:").ShouldBeTrue();
    terminal.OutputContains("Run a simple deploy").ShouldBeTrue();
    AssertHelpOrder(terminal.Output, "Deploy to environment", "Simple deploy");
  }

  public static async Task Should_keep_single_route_help_when_only_one_route_matches_prefix()
  {
    // A single match keeps the per-route layout and does not repeat the route.
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("archive {box}")
        .WithHandler((string box) => "archived " + box)
        .WithDescription("Archive a box")
        .Done()
      .Map("deployment")
        .WithHandler(() => "created")
        .WithDescription("Create a deployment")
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["archive", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Archive a box").ShouldBeTrue();
    terminal.OutputContains("Parameters:").ShouldBeTrue();
    terminal.OutputContains("box").ShouldBeTrue();
    terminal.OutputContains("Create a deployment").ShouldBeFalse();
    int first = terminal.Output.IndexOf("Archive a box", StringComparison.Ordinal);
    int last = terminal.Output.LastIndexOf("Archive a box", StringComparison.Ordinal);
    first.ShouldBeGreaterThanOrEqualTo(0);
    last.ShouldBe(first);
  }

  public static async Task Should_show_per_route_help_for_group_prefix_routes()
  {
    // WithGroupPrefix("deploy") plus "" and "{env}" share the literal prefix "deploy".
    // A longer subcommand is a different prefix and stays out of this invocation.
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("deploy")
        .Map("")
          .WithHandler(() => "simple")
          .WithDescription("Grouped simple deploy")
          .Done()
        .Map("{env}")
          .WithHandler((string env) => "to " + env)
          .WithDescription("Grouped deploy to environment")
          .Done()
        .Map("status")
          .WithHandler(() => "status")
          .WithDescription("Show deploy status")
          .Done()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Grouped simple deploy").ShouldBeTrue();
    terminal.OutputContains("Grouped deploy to environment").ShouldBeTrue();
    terminal.OutputContains("deploy {env}").ShouldBeTrue();
    terminal.OutputContains("Parameters:").ShouldBeTrue();
    terminal.OutputContains("env").ShouldBeTrue();
    terminal.OutputContains("deploy commands:").ShouldBeFalse();
    terminal.OutputContains("Show deploy status").ShouldBeFalse();
    AssertHelpOrder(terminal.Output, "Grouped deploy to environment", "Grouped simple deploy");
  }

  public static async Task Should_not_treat_near_prefix_or_longer_command_as_same_prefix()
  {
    // "deployment" is a different literal from "deploy".
    // "deploy status" has a longer literal prefix than "deploy".
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy").WithHandler(() => "simple").WithDescription("Simple deploy").Done()
      .Map("deploy {env}").WithHandler((string env) => "to " + env).WithDescription("Deploy to environment").Done()
      .Map("deployment").WithHandler(() => "created").WithDescription("Create a deployment").Done()
      .Map("deployment {name}").WithHandler((string name) => "named " + name).WithDescription("Name a deployment").Done()
      .Map("deploy status").WithHandler(() => "status").WithDescription("Show deploy status").Done()
      .Map("deploy status {id}").WithHandler((string id) => "status " + id).WithDescription("Show one deploy status").Done()
      .Build();

    // Act
    int deployExitCode = await app.RunAsync(["deploy", "--help"]);

    // Assert
    deployExitCode.ShouldBe(0);
    terminal.OutputContains("Simple deploy").ShouldBeTrue();
    terminal.OutputContains("Deploy to environment").ShouldBeTrue();
    terminal.OutputContains("Create a deployment").ShouldBeFalse();
    terminal.OutputContains("Name a deployment").ShouldBeFalse();
    terminal.OutputContains("Show deploy status").ShouldBeFalse();
    terminal.OutputContains("Show one deploy status").ShouldBeFalse();
    AssertHelpOrder(terminal.Output, "Deploy to environment", "Simple deploy");
  }

  public static async Task Should_show_help_for_near_prefix_on_its_own()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy").WithHandler(() => "simple").WithDescription("Simple deploy").Done()
      .Map("deploy {env}").WithHandler((string env) => "to " + env).WithDescription("Deploy to environment").Done()
      .Map("deployment").WithHandler(() => "created").WithDescription("Create a deployment").Done()
      .Map("deployment {name}").WithHandler((string name) => "named " + name).WithDescription("Name a deployment").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deployment", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Create a deployment").ShouldBeTrue();
    terminal.OutputContains("Name a deployment").ShouldBeTrue();
    terminal.OutputContains("Simple deploy").ShouldBeFalse();
    terminal.OutputContains("Deploy to environment").ShouldBeFalse();
    AssertHelpOrder(terminal.Output, "Name a deployment", "Create a deployment");
  }

  public static async Task Should_show_help_for_longer_shared_literal_prefix()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy").WithHandler(() => "simple").WithDescription("Simple deploy").Done()
      .Map("deploy status").WithHandler(() => "status").WithDescription("Show deploy status").Done()
      .Map("deploy status {id}").WithHandler((string id) => "status " + id).WithDescription("Show one deploy status").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "status", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Show deploy status").ShouldBeTrue();
    terminal.OutputContains("Show one deploy status").ShouldBeTrue();
    terminal.OutputContains("Simple deploy").ShouldBeFalse();
    AssertHelpOrder(terminal.Output, "Show one deploy status", "Show deploy status");
  }

  public static async Task Should_not_list_same_command_from_another_group()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy").WithHandler(() => "top").WithDescription("Top-level simple deploy").Done()
      .Map("deploy {env}").WithHandler((string env) => "to " + env).WithDescription("Top-level deploy to environment").Done()
      .WithGroupPrefix("git")
        .Map("deploy").WithHandler(() => "git").WithDescription("Git deploy subcommand").Done()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Top-level simple deploy").ShouldBeTrue();
    terminal.OutputContains("Top-level deploy to environment").ShouldBeTrue();
    terminal.OutputContains("Git deploy subcommand").ShouldBeFalse();
  }

  public static async Task Should_keep_grouped_command_help_on_its_own_prefix()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy").WithHandler(() => "top").WithDescription("Top-level simple deploy").Done()
      .WithGroupPrefix("git")
        .Map("deploy").WithHandler(() => "git").WithDescription("Git deploy subcommand").Done()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["git", "deploy", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Git deploy subcommand").ShouldBeTrue();
    terminal.OutputContains("Top-level simple deploy").ShouldBeFalse();
  }

  public static async Task Should_show_help_for_endpoint_routes_with_same_prefix()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<H370PackEndpoint>()
      .Map<H370PackFeedEndpoint>()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["h370-pack", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Pack locally").ShouldBeTrue();
    terminal.OutputContains("Pack for a feed").ShouldBeTrue();
    terminal.OutputContains("Parameters:").ShouldBeTrue();
    terminal.OutputContains("feed").ShouldBeTrue();
    terminal.OutputContains("Target feed").ShouldBeTrue();
    terminal.OutputContains("Options:").ShouldBeTrue();
    terminal.OutputContains("--force").ShouldBeTrue();
    terminal.OutputContains("Examples:").ShouldBeTrue();
    terminal.OutputContains("Pack the default output").ShouldBeTrue();
    AssertHelpOrder(terminal.Output, "Pack for a feed", "Pack locally");
  }

  private static void AssertHelpOrder(string output, string earlier, string later)
  {
    int earlierIndex = output.IndexOf(earlier, StringComparison.Ordinal);
    int laterIndex = output.IndexOf(later, StringComparison.Ordinal);
    earlierIndex.ShouldBeGreaterThanOrEqualTo(0);
    laterIndex.ShouldBeGreaterThan(earlierIndex);
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

// Kanban #370: two endpoint routes whose leading literal is h370-pack.
[NuruRoute("h370-pack", Description = "Pack locally")]
[NuruRouteExample("h370-pack", Description = "Pack the default output")]
public sealed class H370PackEndpoint : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<H370PackEndpoint, Unit>
  {
    public Task<Unit> Handle(H370PackEndpoint command, CancellationToken cancellationToken) => Unit.Task;
  }
}

[NuruRoute("h370-pack", Description = "Pack for a feed")]
public sealed class H370PackFeedEndpoint : ICommand<Unit>
{
  [Parameter(Description = "Target feed")]
  public required string Feed { get; set; }

  [Option("force", "f", Description = "Overwrite an existing package")]
  public bool Force { get; set; }

  public sealed class Handler : ICommandHandler<H370PackFeedEndpoint, Unit>
  {
    public Task<Unit> Handle(H370PackFeedEndpoint command, CancellationToken cancellationToken) => Unit.Task;
  }
}

// Issue #215 / kanban #453: Endpoint DSL endpoint with a described parameter.
[NuruRoute("fork", Description = "Fork a repository")]
public sealed class ForkEndpoint : ICommand<Unit>
{
  [Parameter(Description = "Source repository identifier")]
  public required string Source { get; set; }

  internal sealed class Handler(ITerminal terminal) : ICommandHandler<ForkEndpoint, Unit>
  {
    public async Task<Unit> Handle(ForkEndpoint command, CancellationToken ct)
    {
      await terminal.WriteLineAsync($"forked {command.Source}").ConfigureAwait(false);
      return default;
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Help
