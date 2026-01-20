#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

/// <summary>
/// Tests for the --check-updates route registration.
/// Task 132: Verify check-updates route is properly registered via AddCheckUpdatesRoute().
/// </summary>
[TestTag("Routing")]
public class CheckUpdatesRouteTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CheckUpdatesRouteTests>();

  public static async Task Check_updates_route_runs_when_enabled()
  {
    // Arrange - Enable --check-updates via AddCheckUpdatesRoute()
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddCheckUpdatesRoute()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--check-updates"]);

    // Assert - Should not return "Unknown command" (route exists)
    // Note: Actual update check may fail without RepositoryUrl metadata, but route should match
    exitCode.ShouldBe(0);
    terminal.OutputContains("Unknown command").ShouldBeFalse();
  }

  public static async Task Consumer_can_override_check_updates_route()
  {
    // Arrange - Enable --check-updates, then override with custom handler
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddCheckUpdatesRoute()
      .Map("--check-updates").WithHandler(() => "custom-check-updates").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--check-updates"]);

    // Assert - Custom handler should be used, not built-in
    exitCode.ShouldBe(0);
    terminal.OutputContains("custom-check-updates").ShouldBeTrue();
  }

  public static async Task Check_updates_not_available_without_opt_in()
  {
    // Arrange - Do NOT call AddCheckUpdatesRoute()
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--check-updates"]);

    // Assert - Should be "Unknown command" since --check-updates is opt-in
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
