#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Tests for group path information in --capabilities output

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

  public static async Task Should_include_group_path_when_route_has_group_prefix()
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

    // Assert - flat model uses groupPath array instead of nested groups
    terminal.OutputContains("\"groupPath\":").ShouldBeTrue("Should contain groupPath array");
    terminal.OutputContains("\"admin\"").ShouldBeTrue("Should contain admin in groupPath");
    terminal.OutputContains("admin status").ShouldBeTrue("Should contain admin status pattern");

    await Task.CompletedTask;
  }

  public static async Task Should_include_multi_segment_group_path_for_nested_groups()
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

    // Assert - nested groups produce multi-segment groupPath
    terminal.OutputContains("\"groupPath\":").ShouldBeTrue("Should contain groupPath array");
    terminal.OutputContains("\"admin\"").ShouldBeTrue("Should contain admin in groupPath");
    terminal.OutputContains("\"config\"").ShouldBeTrue("Should contain config in groupPath");
    terminal.OutputContains("admin config get {key}").ShouldBeTrue("Should contain full pattern with prefix");

    await Task.CompletedTask;
  }

  public static async Task Should_place_ungrouped_commands_with_empty_group_path()
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

    // Assert - both patterns appear exactly once in the flat list
    terminal.OutputContains("\"pattern\": \"version\"").ShouldBeTrue("Should contain version pattern");
    terminal.OutputContains("\"pattern\": \"admin status\"").ShouldBeTrue("Should contain admin status in group");

    await Task.CompletedTask;
  }

  public static async Task Should_not_duplicate_grouped_commands()
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

    // Assert - each route appears exactly once in the flat list
    string output = terminal.AllOutput;

    int runCount = CountOccurrences(output, "\"pattern\": \"docker run\"");
    runCount.ShouldBe(1, "docker run should appear exactly once");

    int psCount = CountOccurrences(output, "\"pattern\": \"docker ps\"");
    psCount.ShouldBe(1, "docker ps should appear exactly once");

    await Task.CompletedTask;
  }

  public static async Task Should_support_three_level_group_path()
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
    terminal.OutputContains("\"admin\"").ShouldBeTrue("Should contain admin in groupPath");
    terminal.OutputContains("\"config\"").ShouldBeTrue("Should contain config in groupPath");
    terminal.OutputContains("\"db\"").ShouldBeTrue("Should contain db in groupPath");
    terminal.OutputContains("admin config db status").ShouldBeTrue("Should contain full nested pattern");

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

    // Assert - all patterns appear in the flat list
    terminal.OutputContains("\"pattern\": \"help\"").ShouldBeTrue("Should contain help pattern");
    terminal.OutputContains("\"pattern\": \"version\"").ShouldBeTrue("Should contain version pattern");
    terminal.OutputContains("admin status").ShouldBeTrue("Should contain admin status");
    terminal.OutputContains("config get {key}").ShouldBeTrue("Should contain config get");

    await Task.CompletedTask;
  }

  public static async Task Should_include_endpoint_details_in_grouped_commands()
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

    // Assert - endpoint should have full details including kind
    terminal.OutputContains("\"groupPath\":").ShouldBeTrue("Should contain groupPath");
    terminal.OutputContains("\"deploy\"").ShouldBeTrue("Should contain deploy in groupPath");
    terminal.OutputContains("deploy {env}").ShouldBeTrue("Should contain pattern");
    terminal.OutputContains("Deploy to environment").ShouldBeTrue("Should contain description");
    terminal.OutputContains("\"kind\":").ShouldBeTrue("Should contain kind field");
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
