#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Tests per-route help for commands that share a leading literal prefix (Kanban #370).
// Validates "deploy --help" lists every route with that prefix, most specific first, and excludes near-prefix and other-group routes.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.HelpSharedPrefix
{

[TestTag("Help")]
public class PerRouteHelpSharedPrefixTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PerRouteHelpSharedPrefixTests>();

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

// Kanban #370: two endpoint routes whose leading literal is h370-pack.
[NuruRoute("h370-pack", Description = "Pack locally")]
[NuruRouteExample("h370-pack", Description = "Pack the default output")]
public sealed class H370PackEndpoint : ICommand<Unit>
{
  public sealed class Handler : IRequestHandler<H370PackEndpoint, Unit>
  {
    public ValueTask<Unit> Handle(H370PackEndpoint command, CancellationToken cancellationToken) => Unit.ValueTask;
  }
}

[NuruRoute("h370-pack", Description = "Pack for a feed")]
public sealed class H370PackFeedEndpoint : ICommand<Unit>
{
  [Parameter(Description = "Target feed")]
  public required string Feed { get; set; }

  [Option("force", "f", Description = "Overwrite an existing package")]
  public bool Force { get; set; }

  public sealed class Handler : IRequestHandler<H370PackFeedEndpoint, Unit>
  {
    public ValueTask<Unit> Handle(H370PackFeedEndpoint command, CancellationToken cancellationToken) => Unit.ValueTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.HelpSharedPrefix
