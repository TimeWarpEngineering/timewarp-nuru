#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

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

  private static TestTerminal? Terminal;
  private static NuruCoreApp? App;

  public static async Task Setup()
  {
    Terminal = new TestTerminal();

    App = new NuruAppBuilder()
      .UseTerminal(Terminal)
      .Map("git status")
        .WithHandler(() => 0)
        .AsQuery()
        .Done()
      .Map("git commit -m {message}")
        .WithHandler((string _) => 0)
        .AsCommand()
        .Done()
      .Map("git log --count {n:int}")
        .WithHandler((int _) => 0)
        .AsQuery()
        .Done()
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
    await App!.RunReplAsync();

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

} // namespace TimeWarp.Nuru.Tests.ReplTests.TabCyclingBug
