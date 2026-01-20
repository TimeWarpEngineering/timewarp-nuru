#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test to demonstrate tab completion cycling bug
// After first cycle completion, subsequent tabs are interpreted as text input

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.TabCyclingBug
{

[TestTag("REPL")]
public class TabCyclingBugTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TabCyclingBugTests>();

  private const string DebugLogPath = "/tmp/repl-19-debug.log";
  private static TestTerminal? Terminal;
  private static NuruApp? App;

  private static void Log(string message)
  {
    File.AppendAllText(DebugLogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}\n");
  }

  public static async Task Setup()
  {
    Log("Setup() started");
    Terminal = new TestTerminal();
    Log("Terminal created");

    App = NuruApp.CreateBuilder([])
      .UseTerminal(Terminal)
      .Map("git status")
        .WithHandler(() => { })
        .AsQuery()
        .Done()
      .Map("git commit -m {message}")
        .WithHandler((string message) => 0)
        .AsCommand()
        .Done()
      .Map("git log --count {n:int}")
        .WithHandler((int n) => 0)
        .AsQuery()
        .Done()
      .AddRepl(options => { options.Prompt = "demo> "; })
      .Build();

    Log($"App built: {App?.GetType().Name ?? "null"}");
    await Task.CompletedTask;
  }

  public static async Task CleanUp()
  {
    Terminal?.Dispose();
    Terminal = null;
    App = null;
    await Task.CompletedTask;
  }

  [Timeout(5000)]
  public static async Task Tab_cycling_should_cycle_through_all_completions()
  {
    // Tests that tab cycling works through all completions
    //
    // Expected behavior:
    // 1. "git " + Tab     → Show completions (commit, log, status)
    // 2. Tab              → Cycle to "git commit"
    // 3. Tab              → Cycle to "git log"
    // 4. Tab              → Cycle to "git status"

    // Arrange
    Terminal!.QueueKeys("git ");
    Terminal.QueueKey(ConsoleKey.Tab);   // Show completions
    Terminal.QueueKey(ConsoleKey.Tab);   // Cycle to "commit"
    Terminal.QueueKey(ConsoleKey.Tab);   // Cycle to "log"
    Terminal.QueueKey(ConsoleKey.Tab);   // Cycle to "status"
    Terminal.QueueLine("exit");

    // Act
    Log("About to call RunReplAsync");
    await App!.RunReplAsync();
    Log("RunReplAsync completed");

    // Assert
    WriteLine("=== TAB CYCLING OUTPUT ===");
    WriteLine(Terminal.Output);
    WriteLine("=== END ===");

    // Check that all three completions appeared in sequence
    Terminal.OutputContains("commit").ShouldBeTrue("Should cycle to 'commit'");
    Terminal.OutputContains("log").ShouldBeTrue("Should cycle to 'log'");
    Terminal.OutputContains("status").ShouldBeTrue("Should cycle to 'status'");

    // Should NOT have the old bug where it typed "commite"
    Terminal.OutputContains("commite").ShouldBeFalse("Should not type characters after cycling");
  }

  [Timeout(5000)]
  public static async Task Tab_cycling_first_completion_works()
  {
    // This part of tab completion DOES work: cycling to first completion

    // Arrange
    Terminal!.QueueKeys("git ");
    Terminal.QueueKey(ConsoleKey.Tab);   // Show completions
    Terminal.QueueKey(ConsoleKey.Tab);   // Cycle to "commit" ✓ WORKS
    Terminal.QueueLine("exit");

    // Act
    Log("About to call RunReplAsync (second test)");
    await App!.RunReplAsync();
    Log("RunReplAsync completed (second test)");

    // Assert
    Terminal.OutputContains("Available completions").ShouldBeTrue(
      "Should show completions list"
    );
    Terminal.OutputContains("commit").ShouldBeTrue(
      "Should show 'commit' in completions"
    );
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.TabCyclingBug
