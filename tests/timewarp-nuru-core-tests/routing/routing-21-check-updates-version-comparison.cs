#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

/// <summary>
/// Tests for the --check-updates route registration and configuration.
/// Task 132: Verify check-updates route is properly registered.
/// </summary>
[TestTag("Routing")]
public class CheckUpdatesRouteTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CheckUpdatesRouteTests>();

  public static async Task Check_updates_route_is_registered_by_default()
  {
    // Arrange - Create builder with UseAllExtensions (registers --check-updates)
    NuruAppBuilder builder = new();
    builder.UseAllExtensions();

    // Assert - Should have --check-updates endpoint
    bool hasCheckUpdates = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.OptionMatchers.Any(opt =>
        opt.MatchPattern == "--check-updates"));

    hasCheckUpdates.ShouldBeTrue("--check-updates route should be registered by default");
    await Task.CompletedTask;
  }

  public static async Task Check_updates_route_can_be_disabled()
  {
    // Arrange - Create builder with DisableCheckUpdatesRoute = true
    NuruAppOptions options = new() { DisableCheckUpdatesRoute = true };
    NuruAppBuilder builder = new();
    builder.UseAllExtensions(options);

    // Assert - Should NOT have --check-updates endpoint
    bool hasCheckUpdates = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.OptionMatchers.Any(opt =>
        opt.MatchPattern == "--check-updates"));

    hasCheckUpdates.ShouldBeFalse("--check-updates route should NOT be registered when disabled");
    await Task.CompletedTask;
  }

  public static async Task Consumer_can_override_check_updates_route()
  {
    // Arrange - Create builder with UseAllExtensions (registers --check-updates)
    // Override with custom handler
    bool customHandlerCalled = false;
    NuruCoreApp app = new NuruAppBuilder()
      .UseAllExtensions()
      .Map("--check-updates", () => { customHandlerCalled = true; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--check-updates"]);

    // Assert - Custom handler should be used, not built-in
    exitCode.ShouldBe(0);
    customHandlerCalled.ShouldBeTrue("Consumer's --check-updates handler should override the built-in one");
  }

  public static async Task Endpoint_count_after_override()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.UseAllExtensions();
    builder.Map("--check-updates", () => { });

    // Count endpoints with --check-updates pattern
    int checkUpdatesEndpoints = builder.EndpointCollection.Count(e =>
      e.CompiledRoute.OptionMatchers.Any(opt =>
        opt.MatchPattern == "--check-updates"));

    // Assert - Should only have ONE --check-updates endpoint (the override replaced the original)
    checkUpdatesEndpoints.ShouldBe(1, "Override should replace, not add duplicate");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
