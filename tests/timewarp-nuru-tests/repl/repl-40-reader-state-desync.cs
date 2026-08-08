#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Regression tests for REPL console-reader state/screen desync bugs (Task 454-020).
// M18 yank-arg silent loss, M19 incremental-search over-advance, M20 stale-selection crash.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.ReaderStateDesync
{

[TestTag("REPL")]
[TestTag("StateDesync")]
public class ReaderStateDesyncTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ReaderStateDesyncTests>();

  private static int CountOccurrences(string haystack, string needle) =>
    haystack.Split(needle).Length - 1;

  // ============================================================================
  // M20 — Selection bounds clamp (pure unit test on the Selection state class)
  // ============================================================================

  public static async Task Selection_clamps_stale_bounds_without_throwing()
  {
    // Arrange: a selection [2,10) captured against a longer buffer...
    Selection selection = new();
    selection.StartAt(2);
    selection.ExtendTo(10);

    // ...that the buffer has since shrunk beneath (now only 5 chars).
    const string buffer = "abcde";

    // Act: clamped bounds must stay within the current buffer.
    (int start, int end) = selection.GetClampedBounds(buffer.Length);

    // Assert: End clamped from 10 down to 5; Start preserved.
    start.ShouldBe(2);
    end.ShouldBe(5);

    // The whole point: slicing with these bounds must NOT throw ArgumentOutOfRangeException.
    string afterDelete = buffer[..start] + buffer[end..];
    afterDelete.ShouldBe("ab");

    // GetSelectedText shares the same clamp and stays in-range.
    selection.GetSelectedText(buffer).ShouldBe("cde");

    await Task.CompletedTask;
  }

  public static async Task Selection_clamped_bounds_are_ordered_for_in_range_selection()
  {
    // A normal, in-range selection is returned unchanged and remains sliceable.
    Selection selection = new();
    selection.StartAt(1);
    selection.ExtendTo(4);

    const string buffer = "abcdef";
    (int start, int end) = selection.GetClampedBounds(buffer.Length);

    start.ShouldBe(1);
    end.ShouldBe(4);
    (start <= end).ShouldBeTrue("clamped bounds must stay ordered");
    (buffer[..start] + buffer[end..]).ShouldBe("aef");

    await Task.CompletedTask;
  }

  // ============================================================================
  // M18 — YankLastArg must not silently drop the yank when no older entry exists
  // ============================================================================

  public static async Task Second_alt_period_with_no_older_entry_keeps_the_yanked_arg()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("deploy prod");                    // history: one args-bearing entry
    terminal.QueueKeys("run ");                            // fresh line: "run "
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);   // Alt+. -> yank "prod" => "run prod"
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);   // Alt+. again -> no older entry
    terminal.QueueKey(ConsoleKey.Enter);                  // execute the resulting line
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"DEPLOYED:{env}")
        .AsCommand()
        .Done()
      .Map("run {arg}")
        .WithHandler((string arg) => $"RAN:{arg}")
        .AsCommand()
        .Done()
      // PersistHistory: false isolates in-session history from the shared on-disk file so
      // the "no older args-bearing entry" premise holds under multi-mode (no cross-test leak).
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert: the second Alt+. (no older args-bearing entry) must leave "run prod" intact.
    // With the bug it removed "prod" without redrawing, so the executed line was "run "
    // and never produced "RAN:prod".
    terminal.OutputContains("RAN:prod").ShouldBeTrue("2nd Alt+. must not drop the yanked argument");
  }

  // ============================================================================
  // M19 — Refining the search pattern keeps the still-matching current entry
  // ============================================================================

  public static async Task Extending_search_pattern_keeps_current_match_when_it_still_matches()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("git push");                       // history[0] (older)
    terminal.QueueLine("git status");                     // history[1] (newest)
    terminal.QueueKey(ConsoleKey.R, ctrl: true);          // enter reverse incremental search
    terminal.QueueKeys("gi");                             // matches newest "git status"
    terminal.QueueKeys("t");                              // pattern "git" — current still matches
    terminal.QueueKey(ConsoleKey.Enter);                  // accept + execute current match
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("git push")
        .WithHandler(() => "EXEC-PUSH")
        .AsCommand()
        .Done()
      .Map("git status")
        .WithHandler(() => "EXEC-STATUS")
        .AsCommand()
        .Done()
      // Isolate history so only the two seeded entries exist (see M18 note above).
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Both handlers ran once during history seeding, so count executions to tell which entry
    // the search accepted. Fixed: search re-executes "git status" (2 total). Buggy: typing
    // "t" jumped to the older "git push", so it would run again (PUSH=2, STATUS=1).
    CountOccurrences(terminal.Output, "EXEC-STATUS")
      .ShouldBe(2, "refining the pattern must keep 'git status' selected (seed + search)");
    CountOccurrences(terminal.Output, "EXEC-PUSH")
      .ShouldBe(1, "search must NOT advance to older 'git push' while 'git status' still matches");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.ReaderStateDesync
