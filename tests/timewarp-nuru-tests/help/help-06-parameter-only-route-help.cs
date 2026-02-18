#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#pragma warning disable RCS1163 // Unused parameter - parameters must match route pattern names for binding

#region Purpose
// Tests for Task #433: Parameter-only routes (e.g., "{searchTerm}") intercept built-in flags.
// A route with only a parameter and no literals was consuming --help as a parameter value
// instead of showing help. This tests the fix for that bug.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class ParameterOnlyRouteHelpTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ParameterOnlyRouteHelpTests>();

  /// <summary>
  /// Task #433: Parameter-only route should not intercept --help.
  /// Route pattern: "{searchTerm}" (single parameter, no literals)
  /// </summary>
  public static async Task Should_show_help_not_execute_handler_for_single_parameter_route()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("getleads")
      .Map("{searchTerm}").WithHandler((string searchTerm) => $"Searching for: {searchTerm}").AsQuery().Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Searching for: --help").ShouldBeFalse("Handler should NOT execute with --help as parameter");
    terminal.OutputContains("getleads").ShouldBeTrue("Help should show app name");
    terminal.OutputContains("searchTerm").ShouldBeTrue("Help should show parameter name");
  }

  /// <summary>
  /// Task #433: Parameter-only route with -h short form should also show help.
  /// </summary>
  public static async Task Should_show_help_with_short_form_for_parameter_route()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("getleads")
      .Map("{searchTerm}").WithHandler((string searchTerm) => $"Searching for: {searchTerm}").AsQuery().Done()
      .Build();

    int exitCode = await app.RunAsync(["-h"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Searching for: -h").ShouldBeFalse("Handler should NOT execute with -h as parameter");
    terminal.OutputContains("getleads").ShouldBeTrue("Help should show app name");
  }

  /// <summary>
  /// Task #433: Parameter-only route with --version should show version, not execute handler.
  /// </summary>
  public static async Task Should_show_version_for_parameter_route()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("getleads")
      .Map("{searchTerm}").WithHandler((string searchTerm) => $"Searching for: {searchTerm}").AsQuery().Done()
      .Build();

    int exitCode = await app.RunAsync(["--version"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Searching for: --version").ShouldBeFalse("Handler should NOT execute with --version as parameter");
  }

  /// <summary>
  /// Task #433: Parameter-only route should still work with valid arguments.
  /// </summary>
  public static async Task Should_execute_parameter_route_with_valid_arg()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("getleads")
      .Map("{searchTerm}").WithHandler((string searchTerm) => $"Searching for: {searchTerm}").AsQuery().Done()
      .Build();

    int exitCode = await app.RunAsync(["electricians"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Searching for: electricians").ShouldBeTrue("Handler should execute with valid parameter");
  }

  /// <summary>
  /// Task #433: Multiple parameters route should not intercept --help.
  /// Route pattern: "{category} {item}"
  /// </summary>
  public static async Task Should_show_help_for_multiple_parameters_route()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("catalog")
      .Map("{category} {item}").WithHandler((string category, string item) => $"Category: {category}, Item: {item}").AsQuery().Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Category: --help").ShouldBeFalse("Handler should NOT execute");
    terminal.OutputContains("catalog").ShouldBeTrue("Help should show app name");
  }

  /// <summary>
  /// Task #433: Optional parameter route should not intercept --help.
  /// Route pattern: "{searchTerm?}"
  /// </summary>
  public static async Task Should_show_help_for_optional_parameter_route()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("search")
      .Map("{searchTerm?}").WithHandler((string? searchTerm) => $"Search: {searchTerm ?? "(none)"}").AsQuery().Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Search: --help").ShouldBeFalse("Handler should NOT execute");
    terminal.OutputContains("search").ShouldBeTrue("Help should show app name");
  }

  /// <summary>
  /// Task #433: Catch-all route should not intercept --help.
  /// Route pattern: "{*args}"
  /// </summary>
  public static async Task Should_show_help_for_catch_all_route()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("passthru")
      .Map("{*args}").WithHandler((string[] args) => $"Args: {string.Join(" ", args)}").AsQuery().Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Args: --help").ShouldBeFalse("Handler should NOT execute");
    terminal.OutputContains("passthru").ShouldBeTrue("Help should show app name");
  }
}

} // namespace TimeWarp.Nuru.Tests.Help
