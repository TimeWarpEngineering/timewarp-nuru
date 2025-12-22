#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

/// <summary>
/// Tests for consumer override of the built-in --version route.
/// Task 129: Verify behavior when consumer maps their own --version route.
/// </summary>
[TestTag("Routing")]
public class VersionRouteOverrideTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<VersionRouteOverrideTests>();

  public static async Task Consumer_can_override_version_route()
  {
    // Arrange - Create builder with UseAllExtensions (registers --version,-v)
    // Override with SAME pattern to replace built-in
    bool customHandlerCalled = false;
    NuruCoreApp app = new NuruAppBuilder()
      .UseAllExtensions()
      .Map("--version,-v").WithHandler(() => { customHandlerCalled = true; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--version"]);

    // Assert - Custom handler should be used, not built-in
    exitCode.ShouldBe(0);
    customHandlerCalled.ShouldBeTrue("Consumer's --version handler should override the built-in one");
  }

  public static async Task Consumer_can_override_version_route_with_alias()
  {
    // Arrange - Override --version,-v with custom handler (same pattern as built-in)
    bool customHandlerCalled = false;
    NuruCoreApp app = new NuruAppBuilder()
      .UseAllExtensions()
      .Map("--version,-v").WithHandler(() => { customHandlerCalled = true; }).AsQuery().Done()
      .Build();

    // Act - Use short form
    int exitCode = await app.RunAsync(["-v"]);

    // Assert - Custom handler should be used
    exitCode.ShouldBe(0);
    customHandlerCalled.ShouldBeTrue("Consumer's -v handler should override the built-in one");
  }

  public static async Task DisableVersionRoute_then_map_custom()
  {
    // Arrange - Disable built-in, then add custom (no duplicate)
    bool customHandlerCalled = false;
    NuruAppOptions options = new() { DisableVersionRoute = true };
    NuruCoreApp app = new NuruAppBuilder()
      .UseAllExtensions(options)
      .Map("--version,-v").WithHandler(() => { customHandlerCalled = true; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--version"]);

    // Assert
    exitCode.ShouldBe(0);
    customHandlerCalled.ShouldBeTrue("Custom --version handler should work when built-in is disabled");
  }

  public static async Task Endpoint_count_after_override()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.UseAllExtensions();
    builder.Map("--version,-v").WithHandler(() => { }).AsQuery().Done();

    // Count endpoints with --version pattern
    int versionEndpoints = builder.EndpointCollection.Count(e =>
      e.CompiledRoute.OptionMatchers.Any(opt =>
        opt.MatchPattern == "--version"));

    // Assert - Should only have ONE --version endpoint (the override replaced the original)
    versionEndpoints.ShouldBe(1, "Override should replace, not add duplicate");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
