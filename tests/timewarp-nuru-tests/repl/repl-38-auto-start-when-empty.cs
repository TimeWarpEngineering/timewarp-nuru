#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test AutoStartWhenEmpty feature for REPL (Task 373)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.AutoStartWhenEmpty
{
  [TestTag("REPL")]
  public class AutoStartWhenEmptyTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<AutoStartWhenEmptyTests>();

    public static async Task AutoStart_enabled_no_args_starts_repl()
    {
      // Arrange
      using TestTerminal terminal = new();
      terminal.QueueLine("exit");

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddRepl(options => options.AutoStartWhenEmpty = true)
        .Build();

      // Act - run with NO arguments (should auto-start REPL)
      await app.RunAsync([]);

      // Assert - REPL should have started and shown goodbye message
      terminal.OutputContains("Goodbye!")
        .ShouldBeTrue("AutoStartWhenEmpty=true with no args should start REPL");
    }

    public static async Task AutoStart_disabled_no_args_unknown_command()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddRepl(options => options.AutoStartWhenEmpty = false)
        .Build();

      // Act - run with NO arguments (should NOT auto-start REPL)
      int exitCode = await app.RunAsync([]);

      // Assert - should show "Unknown command" since REPL didn't start
      // Note: "Unknown command" is written to stderr, so we check AllOutput
      terminal.AllOutput.Contains("Unknown command")
        .ShouldBeTrue("AutoStartWhenEmpty=false with no args should show unknown command");
      exitCode.ShouldBe(1);
    }

    public static async Task AutoStart_enabled_with_args_runs_command()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("greet {name}")
          .WithHandler((string name) => $"Hello, {name}!")
          .AsCommand()
          .Done()
        .AddRepl(options => options.AutoStartWhenEmpty = true)
        .Build();

      // Act - run WITH arguments (should run command, not REPL)
      int exitCode = await app.RunAsync(["greet", "World"]);

      // Assert - command should execute normally
      terminal.OutputContains("Hello, World!")
        .ShouldBeTrue("AutoStartWhenEmpty=true with args should run command, not start REPL");
      exitCode.ShouldBe(0);
    }

    public static async Task Interactive_flag_still_works()
    {
      // Arrange
      using TestTerminal terminal = new();
      terminal.QueueLine("exit");

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddRepl(options => options.AutoStartWhenEmpty = false)  // AutoStart disabled
        .Build();

      // Act - run with --interactive flag
      await app.RunAsync(["--interactive"]);

      // Assert - REPL should start via flag regardless of AutoStartWhenEmpty setting
      terminal.OutputContains("Goodbye!")
        .ShouldBeTrue("--interactive flag should work regardless of AutoStartWhenEmpty");
    }

    public static async Task Interactive_short_flag_still_works()
    {
      // Arrange
      using TestTerminal terminal = new();
      terminal.QueueLine("exit");

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddRepl(options => options.AutoStartWhenEmpty = false)  // AutoStart disabled
        .Build();

      // Act - run with -i flag
      await app.RunAsync(["-i"]);

      // Assert - REPL should start via flag regardless of AutoStartWhenEmpty setting
      terminal.OutputContains("Goodbye!")
        .ShouldBeTrue("-i flag should work regardless of AutoStartWhenEmpty");
    }

    public static async Task AutoStart_with_routes_no_args_starts_repl()
    {
      // Arrange - app with both routes and AutoStartWhenEmpty
      using TestTerminal terminal = new();
      terminal.QueueLine("exit");

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("cmd1").WithHandler(() => "Command 1").AsCommand().Done()
        .Map("cmd2").WithHandler(() => "Command 2").AsCommand().Done()
        .AddRepl(options => options.AutoStartWhenEmpty = true)
        .Build();

      // Act - run with NO arguments (should auto-start REPL despite having routes)
      await app.RunAsync([]);

      // Assert - REPL should start
      terminal.OutputContains("Goodbye!")
        .ShouldBeTrue("AutoStartWhenEmpty should take precedence when no args provided");
    }

    public static async Task AutoStart_default_is_false()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddRepl()  // No options lambda - should use defaults
        .Build();

      // Act - run with NO arguments (default AutoStartWhenEmpty is false)
      int exitCode = await app.RunAsync([]);

      // Assert - should NOT auto-start REPL
      // Note: "Unknown command" is written to stderr, so we check AllOutput
      terminal.AllOutput.Contains("Unknown command")
        .ShouldBeTrue("Default AutoStartWhenEmpty should be false");
      exitCode.ShouldBe(1);
    }
  }
}
