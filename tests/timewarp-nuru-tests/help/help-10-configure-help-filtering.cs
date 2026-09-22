#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#pragma warning disable RCS1163 // Unused parameter - parameters must match route pattern names for binding

#region Purpose
// Kanban 470-001: ConfigureHelp filters are extracted into HelpModel and applied by HelpEmitter.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class ConfigureHelpFilteringTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ConfigureHelpFilteringTests>();

  public static async Task Should_exclude_routes_matching_wildcard_patterns()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .ConfigureHelp(options =>
      {
        options.ExcludePatterns = ["h10-*-debug"];
      })
      .Map("h10-keep").WithHandler(() => "ok").WithDescription("Keep this").Done()
      .Map("h10-secret-debug").WithHandler(() => "dbg").WithDescription("Hide this").Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("h10-keep").ShouldBeTrue();
    terminal.OutputContains("Keep this").ShouldBeTrue();
    terminal.OutputContains("h10-secret-debug").ShouldBeFalse();
    terminal.OutputContains("Hide this").ShouldBeFalse();
  }

  public static async Task Should_hide_per_command_help_rows_by_default()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("h10-status").WithHandler(() => "ok").WithDescription("Show status").Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("h10-status").ShouldBeTrue();
    terminal.OutputContains("h10-status --help").ShouldBeFalse();
  }

  public static async Task Should_list_per_command_help_rows_when_enabled()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .ConfigureHelp(options => options.ShowPerCommandHelpRoutes = true)
      .Map("h10-listed").WithHandler(() => "ok").WithDescription("Listed command").Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("h10-listed --help").ShouldBeTrue();
    terminal.OutputContains("Show help for this command").ShouldBeTrue();
  }

  public static async Task Should_list_repl_commands_in_cli_help_when_enabled()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .ConfigureHelp(options => options.ShowReplCommandsInCli = true)
      .Map("h10-repl-cmd").WithHandler(() => "ok").Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("REPL:").ShouldBeTrue();
    terminal.OutputContains("clear-history").ShouldBeTrue();
  }

  public static async Task Should_list_completion_routes_when_enabled()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .EnableCompletion()
      .ConfigureHelp(options => options.ShowCompletionRoutes = true)
      .Map("h10-comp-cmd").WithHandler(() => "ok").Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("--generate-completion").ShouldBeTrue();
    terminal.OutputContains("--install-completion").ShouldBeTrue();
    terminal.OutputContains("__complete").ShouldBeTrue();
  }

  public static async Task Should_hide_completion_routes_by_default()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .EnableCompletion()
      .Map("h10-comp-hidden").WithHandler(() => "ok").Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("__complete").ShouldBeFalse();
    terminal.OutputContains("--generate-completion").ShouldBeFalse();
    terminal.OutputContains("--install-completion").ShouldBeFalse();
  }
}

}
