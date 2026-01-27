#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Tests for hierarchical group structure in --capabilities output

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.CapabilitiesGroups
{

[TestTag("Capabilities")]
public class CapabilitiesGroupTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CapabilitiesGroupTests>();

  public static async Task Should_include_groups_array_when_routes_have_group_prefix()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("admin")
        .Map("status").WithHandler(() => "admin status").Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert
    terminal.OutputContains("\"groups\":").ShouldBeTrue("Should contain groups array");
    terminal.OutputContains("\"name\": \"admin\"").ShouldBeTrue("Should contain admin group");
    terminal.OutputContains("admin status").ShouldBeTrue("Should contain admin status pattern");

    await Task.CompletedTask;
  }

  public static async Task Should_nest_groups_hierarchically()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("admin")
        .WithGroupPrefix("config")
          .Map("get {key}").WithHandler((string key) => $"value-of-{key}").Done()
          .Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert - Check for nested structure
    // Should have top-level groups array with admin
    terminal.OutputContains("\"groups\":").ShouldBeTrue("Should contain groups array");
    terminal.OutputContains("\"name\": \"admin\"").ShouldBeTrue("Should contain admin group");
    terminal.OutputContains("\"name\": \"config\"").ShouldBeTrue("Should contain config nested group");
    terminal.OutputContains("admin config get {key}").ShouldBeTrue("Should contain full pattern with prefix");

    await Task.CompletedTask;
  }

  public static async Task Should_place_ungrouped_commands_at_top_level_only()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("version").WithHandler(() => "1.0.0").Done()
      .WithGroupPrefix("admin")
        .Map("status").WithHandler(() => "admin status").Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert
    // Top-level commands should contain version
    terminal.OutputContains("\"pattern\": \"version\"").ShouldBeTrue("Should contain version pattern");

    // Groups should contain admin
    terminal.OutputContains("\"name\": \"admin\"").ShouldBeTrue("Should contain admin group");
    terminal.OutputContains("\"pattern\": \"admin status\"").ShouldBeTrue("Should contain admin status in group");

    await Task.CompletedTask;
  }

  public static async Task Should_not_duplicate_grouped_commands_at_top_level()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("docker")
        .Map("run").WithHandler(() => "docker run").Done()
        .Map("ps").WithHandler(() => "docker ps").Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert - The top-level commands array should be empty
    // (all commands are in the docker group)
    string output = terminal.AllOutput;

    // Count occurrences of "docker run" - should only appear once (in the group)
    int runCount = CountOccurrences(output, "\"pattern\": \"docker run\"");
    runCount.ShouldBe(1, "docker run should appear exactly once (in group only)");

    int psCount = CountOccurrences(output, "\"pattern\": \"docker ps\"");
    psCount.ShouldBe(1, "docker ps should appear exactly once (in group only)");

    await Task.CompletedTask;
  }

  public static async Task Should_show_empty_parent_groups_in_hierarchy()
  {
    // Arrange - admin has no direct commands, only nested config group has commands
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("admin")
        .WithGroupPrefix("config")
          .Map("get").WithHandler(() => "config get").Done()
          .Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert - admin group should appear even with no direct commands
    terminal.OutputContains("\"name\": \"admin\"").ShouldBeTrue("Should contain admin group");
    terminal.OutputContains("\"name\": \"config\"").ShouldBeTrue("Should contain config group");

    await Task.CompletedTask;
  }

  public static async Task Should_support_three_level_nesting()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("admin")
        .WithGroupPrefix("config")
          .WithGroupPrefix("db")
            .Map("status").WithHandler(() => "db status").Done()
            .Done()
          .Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert
    terminal.OutputContains("\"name\": \"admin\"").ShouldBeTrue("Should contain admin group");
    terminal.OutputContains("\"name\": \"config\"").ShouldBeTrue("Should contain config group");
    terminal.OutputContains("\"name\": \"db\"").ShouldBeTrue("Should contain db group");
    terminal.OutputContains("admin config db status").ShouldBeTrue("Should contain full nested pattern");

    await Task.CompletedTask;
  }

  public static async Task Should_sort_groups_alphabetically()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("zebra")
        .Map("cmd").WithHandler(() => "zebra cmd").Done()
        .Done()
      .WithGroupPrefix("alpha")
        .Map("cmd").WithHandler(() => "alpha cmd").Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert - alpha should appear before zebra in the output
    string output = terminal.AllOutput;
    int alphaPos = output.IndexOf("\"name\": \"alpha\"", StringComparison.Ordinal);
    int zebraPos = output.IndexOf("\"name\": \"zebra\"", StringComparison.Ordinal);

    alphaPos.ShouldBeLessThan(zebraPos, "alpha group should appear before zebra group");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_mixed_grouped_and_ungrouped_commands()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("help").WithHandler(() => "help").Done()
      .Map("version").WithHandler(() => "version").Done()
      .WithGroupPrefix("config")
        .Map("get {key}").WithHandler((string key) => $"get {key}").Done()
        .Map("set {key} {value}").WithHandler((string key, string value) => $"set {key}={value}").Done()
        .Done()
      .WithGroupPrefix("admin")
        .Map("status").WithHandler(() => "status").Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert
    // Groups should exist
    terminal.OutputContains("\"groups\":").ShouldBeTrue("Should contain groups array");

    // Ungrouped commands should be at top level
    terminal.OutputContains("\"pattern\": \"help\"").ShouldBeTrue("Should contain help pattern");
    terminal.OutputContains("\"pattern\": \"version\"").ShouldBeTrue("Should contain version pattern");

    // Grouped commands should be in their groups
    terminal.OutputContains("\"name\": \"admin\"").ShouldBeTrue("Should contain admin group");
    terminal.OutputContains("\"name\": \"config\"").ShouldBeTrue("Should contain config group");

    await Task.CompletedTask;
  }

  public static async Task Should_include_command_details_in_grouped_commands()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("deploy")
        .Map("{env} --dry-run,-d").WithHandler((string env, bool dryRun) => $"deploy {env} dryRun={dryRun}")
          .WithDescription("Deploy to environment")
          .Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert - command should have full details
    terminal.OutputContains("\"name\": \"deploy\"").ShouldBeTrue("Should contain deploy group");
    terminal.OutputContains("deploy {env}").ShouldBeTrue("Should contain pattern");
    terminal.OutputContains("Deploy to environment").ShouldBeTrue("Should contain description");
    terminal.OutputContains("\"name\": \"env\"").ShouldBeTrue("Should contain parameter name");
    terminal.OutputContains("\"name\": \"dry-run\"").ShouldBeTrue("Should contain option name");
    terminal.OutputContains("\"alias\": \"d\"").ShouldBeTrue("Should contain option alias");

    await Task.CompletedTask;
  }

  private static int CountOccurrences(string text, string pattern)
  {
    int count = 0;
    int index = 0;
    while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
    {
      count++;
      index += pattern.Length;
    }

    return count;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.CapabilitiesGroups
