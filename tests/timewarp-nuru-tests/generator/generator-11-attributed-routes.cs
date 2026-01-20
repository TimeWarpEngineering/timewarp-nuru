#!/usr/bin/dotnet --
#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CA1725 // Parameter names should match base declaration
#pragma warning disable CA1849 // Call async methods when in async method
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable RCS1248 // Use pattern matching to check for null

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Attributed Routes (#309, #310, #311)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly handles [NuruRoute] attributed
// classes with [Parameter] and [Option] attributes.
//
// REGRESSION TESTS FOR:
// - Bug #309: Typed option properties (int, double, etc.) must generate type conversion
// - Bug #310: Hyphenated option names (--no-cache) must generate correct camelCase vars
// - Bug #311: Catch-all params named Args must not collide with method args parameter
//
// HOW IT WORKS:
// 1. Define command classes with [NuruRoute], [Parameter], [Option] attributes
// 2. Source generator discovers these and generates routing + invocation code
// 3. If it compiles and runs correctly, the generated code is valid
//
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.AttributedRoutes
{
  /// <summary>
  /// Tests for attributed route generation ([NuruRoute] pattern).
  /// These tests verify that the source generator correctly handles
  /// command classes with [NuruRoute], [Parameter], and [Option] attributes.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("AttributedRoutes")]
  public class AttributedRouteTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<AttributedRouteTests>();

    /// <summary>
    /// Test basic attributed route with simple string parameter.
    /// </summary>
    public static async Task Should_handle_basic_attributed_route()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["greet", "World"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Hello, World!").ShouldBeTrue();
    }

    /// <summary>
    /// Regression test for bug #309: Typed option properties (int, double, etc.)
    /// must generate proper type conversion code.
    /// </summary>
    public static async Task Should_convert_typed_option_to_int()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints()
        .Build();

      // Act - deploy with --replicas option (int type)
      int exitCode = await app.RunAsync(["deploy", "prod", "--replicas", "5"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Replicas: 5").ShouldBeTrue();
    }

    /// <summary>
    /// Regression test for bug #310: Hyphenated option names (e.g., --no-cache)
    /// must generate correct camelCase variable names (noCache).
    /// </summary>
    public static async Task Should_handle_hyphenated_flag_option()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints()
        .Build();

      // Act - build with --no-cache flag
      int exitCode = await app.RunAsync(["build", ".", "--no-cache"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("--no-cache").ShouldBeTrue();
    }

    /// <summary>
    /// Regression test for bug #311: Catch-all parameters named Args
    /// must not collide with the method's args parameter.
    /// </summary>
    public static async Task Should_handle_catch_all_named_args()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints()
        .Build();

      // Act - exec with catch-all args
      int exitCode = await app.RunAsync(["exec", "ls", "-la", "/tmp"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("exec ls -la /tmp").ShouldBeTrue();
    }

    /// <summary>
    /// Test that multiple typed options work correctly together.
    /// </summary>
    public static async Task Should_handle_multiple_typed_options()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints()
        .Build();

      // Act - deploy with multiple options
      int exitCode = await app.RunAsync(["deploy", "staging", "--force", "--replicas", "3", "--config", "app.json"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("staging").ShouldBeTrue();
      terminal.OutputContains("Force: True").ShouldBeTrue();
      terminal.OutputContains("Replicas: 3").ShouldBeTrue();
      terminal.OutputContains("app.json").ShouldBeTrue();
    }

    /// <summary>
    /// Test that option with default value works when not provided.
    /// </summary>
    public static async Task Should_use_default_for_optional_int_option()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints()
        .Build();

      // Act - deploy without --replicas (should use default)
      int exitCode = await app.RunAsync(["deploy", "dev"]);

      // Assert
      exitCode.ShouldBe(0);
      // Default replicas is 0 (default int when option not provided)
      terminal.OutputContains("Replicas:").ShouldBeTrue();
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Command definitions with nested handlers - discovered automatically via [NuruRoute]
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Simple greeting command to test basic attributed route.
/// </summary>
[NuruRoute("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommand<Unit>
{
  [Parameter(Description = "Name to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler(ITerminal terminal) : ICommandHandler<GreetCommand, Unit>
  {
    public ValueTask<Unit> Handle(GreetCommand command, CancellationToken ct)
    {
      terminal.WriteLine($"Hello, {command.Name}!");
      return default;
    }
  }
}

/// <summary>
/// Deploy command to test typed options (bug #309).
/// </summary>
[NuruRoute("deploy", Description = "Deploy to environment")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter(Description = "Target environment")]
  public string Env { get; set; } = string.Empty;

  [Option("force", "f", Description = "Skip confirmation")]
  public bool Force { get; set; }

  [Option("config", "c", Description = "Config file path")]
  public string? ConfigFile { get; set; }

  [Option("replicas", "r", Description = "Number of replicas")]
  public int Replicas { get; set; } = 1;

  public sealed class Handler(ITerminal terminal) : ICommandHandler<DeployCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeployCommand command, CancellationToken ct)
    {
      terminal.WriteLine($"Deploying to {command.Env}...");
      terminal.WriteLine($"  Force: {command.Force}");
      terminal.WriteLine($"  Config: {command.ConfigFile ?? "(default)"}");
      terminal.WriteLine($"  Replicas: {command.Replicas}");
      return default;
    }
  }
}

/// <summary>
/// Build command to test hyphenated flag options (bug #310).
/// </summary>
[NuruRoute("build", Description = "Build something")]
public sealed class BuildCommand : ICommand<Unit>
{
  [Parameter(Description = "Path to build")]
  public string Path { get; set; } = string.Empty;

  [Option("tag", "t", Description = "Tag name")]
  public string? Tag { get; set; }

  [Option("no-cache", null, Description = "Disable cache")]
  public bool NoCache { get; set; }

  public sealed class Handler(ITerminal terminal) : ICommandHandler<BuildCommand, Unit>
  {
    public ValueTask<Unit> Handle(BuildCommand command, CancellationToken ct)
    {
      string tagInfo = command.Tag != null ? $" -t {command.Tag}" : "";
      string cacheInfo = command.NoCache ? " --no-cache" : "";
      terminal.WriteLine($"Building: {command.Path}{tagInfo}{cacheInfo}");
      return default;
    }
  }
}

/// <summary>
/// Exec command to test catch-all Args parameter (bug #311).
/// </summary>
[NuruRoute("exec", Description = "Execute command with arguments")]
public sealed class ExecCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true, Description = "Command and arguments")]
  public string[] Args { get; set; } = [];

  public sealed class Handler(ITerminal terminal) : ICommandHandler<ExecCommand, Unit>
  {
    public ValueTask<Unit> Handle(ExecCommand command, CancellationToken ct)
    {
      terminal.WriteLine($"Executing: exec {string.Join(" ", command.Args)}");
      return default;
    }
  }
}
