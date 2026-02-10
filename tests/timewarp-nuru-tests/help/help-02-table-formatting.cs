#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#pragma warning disable RCS1163 // Unused parameter - parameters must match route pattern names for binding

#region Purpose
// Tests for table-formatted help output (Task #400).
// Validates that --help uses terminal.WriteTable for commands, options, and per-route help.
// Covers: command tables, option tables, parameter tables, typed parameters, catch-all, optional params.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class HelpTableFormattingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<HelpTableFormattingTests>();

  public static async Task Should_format_commands_as_table()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("testapp")
      .Map("deploy {env}").WithHandler((string env) => "deployed").WithDescription("Deploy to environment").Done()
      .Map("build {project}").WithHandler((string project) => "built").WithDescription("Build a project").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--help"]);

    // Assert - Table headers should be present
    exitCode.ShouldBe(0);
    terminal.OutputContains("Commands:").ShouldBeTrue();
    // Verify commands are shown (table will render these as rows)
    terminal.OutputContains("deploy").ShouldBeTrue();
    terminal.OutputContains("build").ShouldBeTrue();
    // Verify descriptions are in table
    terminal.OutputContains("Deploy to environment").ShouldBeTrue();
    terminal.OutputContains("Build a project").ShouldBeTrue();
  }

  public static async Task Should_format_global_options_as_table()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("testapp")
      .Map("status").WithHandler(() => "ok").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Options:").ShouldBeTrue();
    terminal.OutputContains("--help").ShouldBeTrue();
    terminal.OutputContains("-h").ShouldBeTrue();
    terminal.OutputContains("Show this help message").ShouldBeTrue();
    terminal.OutputContains("--version").ShouldBeTrue();
    terminal.OutputContains("--capabilities").ShouldBeTrue();
  }

  public static async Task Should_format_per_route_parameters_as_table()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("copy {source} {destination}").WithHandler((string source, string destination) => "copied").WithDescription("Copy files").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["copy", "--help"]);

    // Assert - Parameters table should have columns: Name, Required, Type
    exitCode.ShouldBe(0);
    terminal.OutputContains("Parameters:").ShouldBeTrue();
    // Both parameters should be in the table
    terminal.OutputContains("source").ShouldBeTrue();
    terminal.OutputContains("destination").ShouldBeTrue();
    // Type column should show string as default
    terminal.OutputContains("string").ShouldBeTrue();
    // Required column should show Yes for required params
    terminal.OutputContains("Yes").ShouldBeTrue();
  }

  public static async Task Should_format_per_route_options_as_table()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env} --force,-f? --verbose,-v?").WithHandler((string env, bool force, bool verbose) => "deployed").WithDescription("Deploy with options").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--help"]);

    // Assert - Options table should have columns: Option, Description
    exitCode.ShouldBe(0);
    terminal.OutputContains("Options:").ShouldBeTrue();
    terminal.OutputContains("--force").ShouldBeTrue();
    terminal.OutputContains("-f").ShouldBeTrue();
    terminal.OutputContains("--verbose").ShouldBeTrue();
    terminal.OutputContains("-v").ShouldBeTrue();
  }

  public static async Task Should_handle_empty_description_in_table()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("testapp")
      .Map("deploy").WithHandler(() => "deployed").Done() // No description
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Commands:").ShouldBeTrue();
    terminal.OutputContains("deploy").ShouldBeTrue();
    // Should still render even without description (empty cell in table)
  }

  public static async Task Should_format_typed_parameters_with_type_column()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("delay {ms:int}").WithHandler((int ms) => "delayed").WithDescription("Add delay").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["delay", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Parameters:").ShouldBeTrue();
    terminal.OutputContains("ms").ShouldBeTrue();
    // Type column should show int
    terminal.OutputContains("int").ShouldBeTrue();
  }

  public static async Task Should_format_catch_all_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("exec {*args}").WithHandler((string[] args) => "executed").WithDescription("Execute command with args").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["exec", "--help"]);

    // Assert - Catch-all parameter name shows as "*args"
    exitCode.ShouldBe(0);
    terminal.OutputContains("Parameters:").ShouldBeTrue();
    terminal.OutputContains("*args").ShouldBeTrue();
    // Catch-all parameters are required (must be present, accept 0+ values)
    terminal.OutputContains("Yes").ShouldBeTrue(); // Required
  }

  public static async Task Should_format_optional_parameters()
  {
    // Arrange - use optional parameter with ? inside curly braces
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name?}").WithHandler((string name) => "greeted").WithDescription("Greet someone").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["greet", "--help"]);

    // Assert - Optional parameter shows "No" in Required column
    exitCode.ShouldBe(0);
    terminal.OutputContains("Parameters:").ShouldBeTrue();
    terminal.OutputContains("name").ShouldBeTrue();
    // Optional params show No in Required column
    terminal.OutputContains("No").ShouldBeTrue();
  }

  public static async Task Should_format_group_commands_as_table()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("config")
        .Map("set {key} {value}").WithHandler((string key, string value) => "set").WithDescription("Set config").Done()
        .Map("get {key}").WithHandler((string key) => "got").WithDescription("Get config").Done()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["config", "--help"]);

    // Assert - Group help should show table of commands
    exitCode.ShouldBe(0);
    terminal.OutputContains("config commands:").ShouldBeTrue();
    terminal.OutputContains("set").ShouldBeTrue();
    terminal.OutputContains("get").ShouldBeTrue();
    terminal.OutputContains("Set config").ShouldBeTrue();
    terminal.OutputContains("Get config").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Help
