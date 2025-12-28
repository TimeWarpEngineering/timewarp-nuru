#!/usr/bin/dotnet --

// Generator Phase 6: Minimal Intercept Tests
// Tests the source generator end-to-end with minimal test cases
// Reference: kanban/in-progress/272-v2-generator-phase-6-testing.md

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Minimal
{
  /// <summary>
  /// Minimal tests for generator interceptor functionality.
  /// These tests verify that the source generator correctly intercepts
  /// RunAsync and routes are matched/executed.
  /// </summary>
  [TestTag("Generator")]
  public class MinimalInterceptTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MinimalInterceptTests>();

    /// <summary>
    /// Test that a single route with no parameters is intercepted and executed.
    /// This is the most basic test case for the generator.
    /// </summary>
    public static async Task Should_intercept_single_route()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("ping")
          .WithHandler(() => "pong")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["ping"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("pong").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that unknown commands return an error exit code.
    /// </summary>
    public static async Task Should_return_error_for_unknown_command()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("ping")
          .WithHandler(() => "pong")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["unknown"]);

      // Assert
      exitCode.ShouldBe(1);

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that multiple routes can be registered and matched correctly.
    /// </summary>
    public static async Task Should_match_correct_route_among_multiple()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("ping")
          .WithHandler(() => "pong")
          .AsQuery()
          .Done()
        .Map("status")
          .WithHandler(() => "healthy")
          .AsQuery()
          .Done()
        .Map("version")
          .WithHandler(() => "1.0.0")
          .AsQuery()
          .Done()
        .Build();

      // Act - test second route
      int exitCode = await app.RunAsync(["status"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("healthy").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that --version flag is handled (built-in route).
    /// </summary>
    public static async Task Should_handle_version_flag()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("ping")
          .WithHandler(() => "pong")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["--version"]);

      // Assert - version should be printed
      exitCode.ShouldBe(0);

      await Task.CompletedTask;
    }

    // ========================================================================
    // Commit 6.2: Parameter Tests
    // ========================================================================

    /// <summary>
    /// Test that a string parameter is correctly bound from the route.
    /// </summary>
    public static async Task Should_bind_string_parameter()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("greet {name}")
          .WithHandler((string name) => $"Hello, {name}!")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["greet", "World"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Hello, World!").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that an int parameter is correctly parsed and bound.
    /// </summary>
    public static async Task Should_bind_typed_int_parameter()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("repeat {count:int}")
          .WithHandler((int count) => $"Count: {count}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["repeat", "42"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Count: 42").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that multiple parameters are correctly bound.
    /// </summary>
    public static async Task Should_bind_multiple_parameters()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("add {a:int} {b:int}")
          .WithHandler((int a, int b) => $"Sum: {a + b}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["add", "10", "32"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Sum: 42").ShouldBeTrue();

      await Task.CompletedTask;
    }

    // ========================================================================
    // Commit 6.3: Multiple Routes and Groups Tests
    // ========================================================================

    /// <summary>
    /// Test route specificity - more specific routes should match first.
    /// </summary>
    public static async Task Should_match_more_specific_route_first()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env}")
          .WithHandler((string env) => $"Deploying to {env}")
          .AsCommand()
          .Done()
        .Map("deploy prod")
          .WithHandler(() => "Deploying to PRODUCTION with extra safety checks")
          .AsCommand()
          .Done()
        .Build();

      // Act - "deploy prod" should match the literal route, not the parameterized one
      int exitCode = await app.RunAsync(["deploy", "prod"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("PRODUCTION").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that a literal route vs parameterized route both work.
    /// </summary>
    public static async Task Should_fallback_to_parameterized_route()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy prod")
          .WithHandler(() => "Deploying to PRODUCTION")
          .AsCommand()
          .Done()
        .Map("deploy {env}")
          .WithHandler((string env) => $"Deploying to {env}")
          .AsCommand()
          .Done()
        .Build();

      // Act - "deploy staging" should match the parameterized route
      int exitCode = await app.RunAsync(["deploy", "staging"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Deploying to staging").ShouldBeTrue();

      await Task.CompletedTask;
    }

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

      NuruCoreApp app = NuruApp.CreateBuilder([])
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

      NuruCoreApp app = NuruApp.CreateBuilder([])
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

      NuruCoreApp app = NuruApp.CreateBuilder([])
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

      NuruCoreApp app = NuruApp.CreateBuilder([])
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

      NuruCoreApp app = NuruApp.CreateBuilder([])
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

      NuruCoreApp app = NuruApp.CreateBuilder([])
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

      NuruCoreApp app = NuruApp.CreateBuilder([])
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

      NuruCoreApp app = NuruApp.CreateBuilder([])
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

} // namespace TimeWarp.Nuru.Tests.Generator.Minimal
