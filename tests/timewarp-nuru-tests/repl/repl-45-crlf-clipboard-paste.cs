#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Regression tests for REPL clipboard paste (Task 470-003 / parent 470 M5).
// HandlePasteAsync used to splice raw clipboard text into UserInput and advance
// CursorPosition by clipboardText.Length. Windows Get-Clipboard returns CRLF, so
// UserInput kept \r while the multiline linear domain counts one char per break.
// CRLF splitting itself is covered in repl-31 InsertText tests; these TestTerminal
// cases exercise the paste command path (clipboard or kill-ring fallback).

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.CrlfClipboardPaste
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("Clipboard")]
public class CrlfClipboardPasteTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CrlfClipboardPasteTests>();

  public static async Task Cut_then_paste_inserts_at_cursor()
  {
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.A, ctrl: true, shift: true); // select all
    terminal.QueueKey(ConsoleKey.X, ctrl: true);              // cut → clipboard + kill ring
    terminal.QueueKeys("echo ");
    terminal.QueueKey(ConsoleKey.V, ctrl: true);              // paste
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("echo hello")
        .WithHandler(() => "PASTE-OK")
        .AsQuery()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("PASTE-OK")
      .ShouldBeTrue("Ctrl+X then type then Ctrl+V must paste the cut text at the cursor");
  }

  public static async Task Typing_after_paste_appends_at_end_of_pasted_text()
  {
    // If CursorPosition advanced by raw CRLF length, further typing would land past
    // the \n-domain end (or leave a hole). After paste of "hel", typing "lo" must
    // produce "echo hello".
    using TestTerminal terminal = new();
    terminal.QueueKeys("hel");
    terminal.QueueKey(ConsoleKey.A, ctrl: true, shift: true);
    terminal.QueueKey(ConsoleKey.X, ctrl: true);
    terminal.QueueKeys("echo ");
    terminal.QueueKey(ConsoleKey.V, ctrl: true);
    terminal.QueueKeys("lo");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("echo hello")
        .WithHandler(() => "PASTE-CURSOR-OK")
        .AsQuery()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("PASTE-CURSOR-OK")
      .ShouldBeTrue("Typing after paste must append at the end of the pasted text");
  }

  public static async Task Paste_replaces_an_active_selection()
  {
    using TestTerminal terminal = new();
    terminal.QueueKeys("ok");
    terminal.QueueKey(ConsoleKey.A, ctrl: true, shift: true);
    terminal.QueueKey(ConsoleKey.X, ctrl: true);              // cut "ok"
    terminal.QueueKeys("xxxx");
    terminal.QueueKey(ConsoleKey.A, ctrl: true, shift: true); // select "xxxx"
    terminal.QueueKey(ConsoleKey.V, ctrl: true);              // replace with "ok"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("ok")
        .WithHandler(() => "PASTE-REPLACE-OK")
        .AsQuery()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("PASTE-REPLACE-OK")
      .ShouldBeTrue("Paste over a selection must replace it, not insert beside it");
  }

  public static async Task Multiline_cut_then_paste_executes_as_one_command()
  {
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.A, ctrl: true, shift: true);
    terminal.QueueKey(ConsoleKey.X, ctrl: true);
    terminal.QueueKey(ConsoleKey.V, ctrl: true);
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("echo hello")
        .WithHandler(() => "PASTE-MULTILINE-OK")
        .AsQuery()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("PASTE-MULTILINE-OK")
      .ShouldBeTrue("Pasting Shift+Enter multiline text must execute as one command");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.CrlfClipboardPaste
