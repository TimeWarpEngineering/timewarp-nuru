#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#pragma warning disable RCS1163 // Unused parameter - parameters must match route pattern names for binding

#region Purpose
// Tests for Bug #403: Default route [NuruRoute("")] prevents --help from working.
// Validates that --help works correctly even when a default route is registered.
// Root cause: Default routes generate `routeArgs.Length >= 0` which is always true,
// catching --help before the built-in handler runs.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class DefaultRouteHelpTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DefaultRouteHelpTests>();

  /// <summary>
  /// Bug #403: When a default route exists, --help should still show help, not run the default handler.
  /// </summary>
  public static async Task Should_show_help_when_default_route_exists()
  {
    // Arrange - app with a default route (empty pattern)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("testapp")
      .Map("").WithHandler(() => "default-handler-executed").WithDescription("Default route").AsQuery().Done()
      .Map("greet {name}").WithHandler((string name) => $"Hello {name}").WithDescription("Greet someone").AsQuery().Done()
      .Build();

    // Act - invoke with --help
    int exitCode = await app.RunAsync(["--help"]);

    // Assert - should show help, NOT execute default handler
    exitCode.ShouldBe(0);
    terminal.OutputContains("default-handler-executed").ShouldBeFalse("Default handler should NOT execute when --help is requested");
    terminal.OutputContains("testapp").ShouldBeTrue("Help should show app name");
    terminal.OutputContains("greet").ShouldBeTrue("Help should list the greet command");
  }

  /// <summary>
  /// Bug #403: Default route with options should not intercept --help.
  /// </summary>
  public static async Task Should_show_help_when_default_route_has_options()
  {
    // Arrange - default route with an optional flag
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("testapp")
      .Map("--verbose?").WithHandler((bool verbose) => verbose ? "verbose-on" : "verbose-off").WithDescription("Default with verbose option").AsQuery().Done()
      .Map("status").WithHandler(() => "status-ok").WithDescription("Show status").AsQuery().Done()
      .Build();

    // Act - invoke with --help
    int exitCode = await app.RunAsync(["--help"]);

    // Assert - should show help, NOT execute default handler
    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose-on").ShouldBeFalse("Default handler should NOT execute");
    terminal.OutputContains("verbose-off").ShouldBeFalse("Default handler should NOT execute");
    terminal.OutputContains("testapp").ShouldBeTrue("Help should show app name");
    terminal.OutputContains("status").ShouldBeTrue("Help should list the status command");
  }

  /// <summary>
  /// Verify default route still works when no args provided.
  /// </summary>
  public static async Task Should_execute_default_route_with_empty_args()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("").WithHandler(() => "default-executed").AsQuery().Done()
      .Build();

    // Act - invoke with no args
    int exitCode = await app.RunAsync([]);

    // Assert - default handler should run
    exitCode.ShouldBe(0);
    terminal.OutputContains("default-executed").ShouldBeTrue("Default handler should execute with empty args");
  }

  /// <summary>
  /// Verify -h short form also works with default route.
  /// </summary>
  public static async Task Should_show_help_with_short_form_when_default_route_exists()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("testapp")
      .Map("").WithHandler(() => "default-handler-executed").WithDescription("Default route").AsQuery().Done()
      .Build();

    // Act - invoke with -h
    int exitCode = await app.RunAsync(["-h"]);

    // Assert - should show help, NOT execute default handler
    exitCode.ShouldBe(0);
    terminal.OutputContains("default-handler-executed").ShouldBeFalse("Default handler should NOT execute when -h is requested");
    terminal.OutputContains("testapp").ShouldBeTrue("Help should show app name");
  }

  /// <summary>
  /// Verify --version also works with default route.
  /// </summary>
  public static async Task Should_show_version_when_default_route_exists()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("").WithHandler(() => "default-handler-executed").AsQuery().Done()
      .Build();

    // Act - invoke with --version
    int exitCode = await app.RunAsync(["--version"]);

    // Assert - should show version, NOT execute default handler
    exitCode.ShouldBe(0);
    terminal.OutputContains("default-handler-executed").ShouldBeFalse("Default handler should NOT execute when --version is requested");
  }

  /// <summary>
  /// Verify --capabilities also works with default route.
  /// </summary>
  public static async Task Should_show_capabilities_when_default_route_exists()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("").WithHandler(() => "default-handler-executed").AsQuery().Done()
      .Build();

    // Act - invoke with --capabilities
    int exitCode = await app.RunAsync(["--capabilities"]);

    // Assert - should show capabilities JSON, NOT execute default handler
    exitCode.ShouldBe(0);
    terminal.OutputContains("default-handler-executed").ShouldBeFalse("Default handler should NOT execute when --capabilities is requested");
    terminal.OutputContains("{").ShouldBeTrue("Capabilities should output JSON");
  }
}

} // namespace TimeWarp.Nuru.Tests.Help
