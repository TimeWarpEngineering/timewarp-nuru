#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Regression tests for HandleCharacter with a stale selection after a non-selection
// buffer mutation (Task 470-002 / parent 470 M4).
// Shift+Left then Ctrl+K (or select-all then Up to a shorter history entry) left
// SelectionState.End past UserInput.Length; typing then threw ArgumentOutOfRangeException.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.StaleSelectionCharacterInsert
{

[TestTag("REPL")]
[TestTag("Selection")]
public class StaleSelectionCharacterInsertTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<StaleSelectionCharacterInsertTests>();

  public static async Task Typing_after_shift_left_then_kill_line_does_not_throw()
  {
    // Repro: select a suffix (Shift+Left), Ctrl+K (End now past UserInput.Length), type.
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.LeftArrow, shift: true);  // select 'o'; cursor at 4
    terminal.QueueKey(ConsoleKey.K, ctrl: true);           // kill from cursor → "hell"; selection stale
    terminal.QueueKeys("x");                               // would throw ArgumentOutOfRangeException
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hellx")
        .WithHandler(() => "KILL-REPRO-OK")
        .AsQuery()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("KILL-REPRO-OK")
      .ShouldBeTrue("Shift+Left, Ctrl+K, then type must insert at the kill point, not throw");
  }

  public static async Task Typing_after_select_all_then_shorter_history_does_not_throw()
  {
    // Repro: select-all, Up-arrow to a shorter history entry, type.
    using TestTerminal terminal = new();
    terminal.QueueLine("hi");                             // seed shorter history
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.A, ctrl: true, shift: true);  // select all (End=11)
    terminal.QueueKey(ConsoleKey.UpArrow);                     // replace with "hi"; selection stale
    terminal.QueueKeys("x");                                   // would throw ArgumentOutOfRangeException
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hi")
        .WithHandler(() => "SEED-HI")
        .AsQuery()
        .Done()
      .Map("hix")
        .WithHandler(() => "HISTORY-REPRO-OK")
        .AsQuery()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("HISTORY-REPRO-OK")
      .ShouldBeTrue("Select-all, Up to shorter history, then type must append at the history cursor, not throw");
  }

  public static async Task Typing_after_select_all_then_undo_to_shorter_buffer_does_not_throw()
  {
    // Undo restores a shorter buffer without going through selection handlers.
    using TestTerminal terminal = new();
    terminal.QueueKeys("ab");
    terminal.QueueKey(ConsoleKey.LeftArrow);              // break consecutive-char undo grouping
    terminal.QueueKey(ConsoleKey.RightArrow);
    terminal.QueueKeys("cdef");                           // buffer "abcdef"
    terminal.QueueKey(ConsoleKey.A, ctrl: true, shift: true);  // select all (End=6)
    terminal.QueueKey(ConsoleKey.Z, ctrl: true);               // undo → "ab"; selection stale
    terminal.QueueKeys("x");                                   // would throw ArgumentOutOfRangeException
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("abx")
        .WithHandler(() => "UNDO-REPRO-OK")
        .AsQuery()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("UNDO-REPRO-OK")
      .ShouldBeTrue("Select-all, undo to a shorter buffer, then type must insert at the restored cursor, not throw");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.StaleSelectionCharacterInsert
