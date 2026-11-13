#!/usr/bin/env -S dotnet --

#region Purpose
// Minimal end-to-end tests of the source generator interceptor (generator phase 6): option parsing and route groups.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.MinimalOptions
{
  /// <summary>
  /// Minimal tests for generator interceptor functionality.
  /// These tests verify that the source generator correctly intercepts
  /// RunAsync and routes are matched/executed.
  /// </summary>
  [TestTag("Generator")]
  public class MinimalInterceptOptionsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MinimalInterceptOptionsTests>();

    // ========================================================================
    // Commit 6.4: Options Tests
    // Note: Using unique command prefixes to avoid route conflicts between tests
    // ========================================================================

    /// <summary>
    /// Test that a boolean flag option is correctly parsed.
    /// Pattern: "command --verbose" where verbose is a flag (no value).
    /// </summary>
    public static async Task Should_parse_boolean_flag_option()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("opt-build --verbose")
          .WithHandler((bool verbose) => verbose ? "Building with verbose output" : "Building quietly")
          .AsCommand()
          .Done()
        .Build();

      // Act - with flag
      int exitCode = await app.RunAsync(["opt-build", "--verbose"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("verbose output").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that a boolean flag with alias is correctly parsed.
    /// Pattern: "command --force,-f" where both forms are valid.
    /// </summary>
    public static async Task Should_parse_flag_with_alias()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("opt-push {env} --force,-f")
          .WithHandler((string env, bool force) => force ? $"Force pushing to {env}" : $"Safe push to {env}")
          .AsCommand()
          .Done()
        .Build();

      // Act - using short form
      int exitCode = await app.RunAsync(["opt-push", "prod", "-f"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Force pushing to prod").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that an option with required value is correctly parsed.
    /// Pattern: "command --config {value}" where value must be provided.
    /// </summary>
    public static async Task Should_parse_option_with_value()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("opt-make --config {mode}")
          .WithHandler((string mode) => $"Making in {mode} mode")
          .AsCommand()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["opt-make", "--config", "release"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Making in release mode").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that an option with alias and value is correctly parsed.
    /// Pattern: "command --output,-o {file}" where value must be provided.
    /// </summary>
    public static async Task Should_parse_option_with_alias_and_value()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("opt-compile --output,-o {file}")
          .WithHandler((string file) => $"Output to {file}")
          .AsCommand()
          .Done()
        .Build();

      // Act - using short form
      int exitCode = await app.RunAsync(["opt-compile", "-o", "out.dll"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Output to out.dll").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test multiple options together.
    /// Pattern: "multi-opt {env} --verbose --force --config {mode}"
    /// </summary>
    public static async Task Should_parse_multiple_options()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("multi-opt {env} --verbose --force --config {mode}")
          .WithHandler((string env, bool verbose, bool force, string mode) =>
            $"Multi-opt to {env}: verbose={verbose}, force={force}, mode={mode}")
          .AsCommand()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["multi-opt", "prod", "--verbose", "--force", "--config", "release"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Multi-opt to prod: verbose=True, force=True, mode=release").ShouldBeTrue();

      await Task.CompletedTask;
    }

    // ========================================================================
    // Commit 6.5: Nested Group Tests (Task #276)
    // ========================================================================

    /// <summary>
    /// Test simple group prefix - routes inside a group get the prefix prepended.
    /// </summary>
    public static async Task Should_match_grouped_route()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .WithGroupPrefix("grp-admin")
          .Map("status")
            .WithHandler(() => "admin status ok")
            .AsQuery()
            .Done()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["grp-admin", "status"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("admin status ok").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test nested group prefixes - groups within groups accumulate prefixes.
    /// </summary>
    public static async Task Should_match_nested_group_route()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .WithGroupPrefix("grp-admin")
          .Map("restart")
            .WithHandler(() => "restarting...")
            .AsCommand()
            .Done()
          .WithGroupPrefix("config")
            .Map("get {key}")
              .WithHandler((string key) => $"config value: {key}")
              .AsQuery()
              .Done()
            .Done() // end config group
          .Done() // end admin group
        .Build();

      // Act - test nested route: "grp-admin config get debug"
      int exitCode = await app.RunAsync(["grp-admin", "config", "get", "debug"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("config value: debug").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that a route in the outer group still works after defining a nested group.
    /// </summary>
    public static async Task Should_match_outer_group_route_after_nested_group()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .WithGroupPrefix("grp-admin")
          .WithGroupPrefix("config")
            .Map("list")
              .WithHandler(() => "config list")
              .AsQuery()
              .Done()
            .Done() // end config group
          .Map("status")
            .WithHandler(() => "admin status")
            .AsQuery()
            .Done()
          .Done() // end admin group
        .Build();

      // Act - test outer group route
      int exitCode = await app.RunAsync(["grp-admin", "status"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("admin status").ShouldBeTrue();

      await Task.CompletedTask;
    }
  }

} // namespace TimeWarp.Nuru.Tests.Generator.MinimalOptions
