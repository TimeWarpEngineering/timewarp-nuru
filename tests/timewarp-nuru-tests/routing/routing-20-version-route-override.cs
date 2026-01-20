#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

/// <summary>
/// Tests for consumer override of the built-in --version route.
/// Task 129: Verify behavior when consumer maps their own --version route.
/// The generator produces built-in --version handling, but user routes should override it.
/// </summary>
[TestTag("Routing")]
public class VersionRouteOverrideTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<VersionRouteOverrideTests>();

  // Task #357: User routes should override built-in --version handler
  // Currently the generator emits built-in handlers BEFORE user routes, so built-in wins.
  // The fix requires reordering generated code to emit user routes first.
  public static async Task Consumer_can_override_version_route()
  {
    // Arrange - Map custom --version (should override built-in)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("--version").WithHandler(() => "custom-version-output").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--version"]);

    // Assert - Custom handler should be used, not built-in
    exitCode.ShouldBe(0);
    terminal.OutputContains("custom-version-output").ShouldBeTrue();
  }

  public static async Task Consumer_can_override_version_route_with_short_alias()
  {
    // Arrange - Map custom --version,-v with alias (should override built-in for both forms)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("--version,-v").WithHandler(() => "custom-version-alias").AsQuery().Done()
      .Build();

    // Act - Use short form
    int exitCode = await app.RunAsync(["-v"]);

    // Assert - Custom handler should be used
    exitCode.ShouldBe(0);
    terminal.OutputContains("custom-version-alias").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
