#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Test to demonstrate tab completion cycling bug
// After first cycle completion, subsequent tabs are interpreted as text input

return await RunTests<TabCyclingBugTests>();

[TestTag("REPL")]
[ClearRunfileCache]
public class TabCyclingBugTests
{
  private static TestTerminal? Terminal;
  private static NuruApp? App;

  public static async Task Setup()
  {
    Terminal = new TestTerminal();

    App = new NuruAppBuilder()
      .UseTerminal(Terminal)
      .AddRoute("git status", () => 0)
      .AddRoute("git commit -m {message}", (string _) => 0)
      .AddRoute("git log --count {n:int}", (int _) => 0)
      .AddReplSupport(options => { options.Prompt = "demo> "; })
      .Build();

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
  public static async Task BUG_Tab_cycling_should_cycle_through_all_completions()
  {
    // Demonstrates bug: After first cycle to "commit", subsequent tabs type characters
    // instead of cycling to "log" and "status"
    //
    // Expected behavior:
    // 1. "git " + Tab     → Show completions (commit, log, status)
    // 2. Tab              → Cycle to "git commit"
    // 3. Tab              → Cycle to "git log"      ← BUG: types 'e' instead
    // 4. Tab              → Cycle to "git status"   ← BUG: types 'x' instead

    // Arrange
    Terminal!.QueueKeys("git ");
    Terminal.QueueKey(ConsoleKey.Tab);   // Show completions
    Terminal.QueueKey(ConsoleKey.Tab);   // Cycle to "commit"
    Terminal.QueueKey(ConsoleKey.Tab);   // Should cycle to "log" but types 'e'
    Terminal.QueueKey(ConsoleKey.Tab);   // Should cycle to "status" but types 'x'
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert: This test DOCUMENTS THE BUG - it will fail until fixed
    WriteLine("=== BUG OUTPUT ===");
    WriteLine(Terminal.Output);
    WriteLine("=== END ===");

    // What SHOULD happen: cycling to "log"
    bool hasLog = Terminal.OutputContains("git log");

    // What ACTUALLY happens: typing characters (e, x, i, t spell "exit")
    bool hasCommitE = Terminal.OutputContains("commite");

    WriteLine($"Has 'git log': {hasLog} (expected: true, actual: {hasLog})");
    WriteLine($"Has 'commite': {hasCommitE} (expected: false, actual: {hasCommitE})");

    // This assertion will FAIL until the bug is fixed
    hasLog.ShouldBeTrue("Tab cycling should cycle to 'git log' on third tab");
    hasCommitE.ShouldBeFalse("Tab should not type characters after cycling");
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
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Available completions").ShouldBeTrue(
      "Should show completions list"
    );
    Terminal.OutputContains("commit").ShouldBeTrue(
      "Should show 'commit' in completions"
    );
  }
}
