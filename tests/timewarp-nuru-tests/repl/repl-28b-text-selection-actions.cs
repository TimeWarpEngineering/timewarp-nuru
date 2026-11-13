#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins selection actions, selection clearing, CopyOrCancelLine, and cut/kill-ring integration for REPL text selection; split from repl-28-text-selection.cs.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.TextSelectionActions
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("Selection")]
public class TextSelectionActionsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TextSelectionActionsTests>();

  // ============================================================================
  // Selection Actions Tests
  // ============================================================================

  public static async Task Should_delete_selection_with_backspace()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.LeftArrow, ctrl: true, shift: true);  // Select "world"
    terminal.QueueKey(ConsoleKey.Backspace);                            // Delete selection
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello ")
        .WithHandler(() => "Backspace delete worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Backspace delete worked!").ShouldBeTrue("Backspace should delete selection");
  }

  public static async Task Should_delete_selection_with_delete_key()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.LeftArrow, ctrl: true, shift: true);  // Select "world"
    terminal.QueueKey(ConsoleKey.Delete);                               // Delete selection
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello ")
        .WithHandler(() => "Delete key worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Delete key worked!").ShouldBeTrue("Delete should delete selection");
  }

  public static async Task Should_replace_selection_when_typing()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.LeftArrow, ctrl: true, shift: true);  // Select "world"
    terminal.QueueKeys("universe");                                     // Replace with "universe"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello universe")
        .WithHandler(() => "Replace worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Replace worked!").ShouldBeTrue("Typing should replace selection");
  }

  // ============================================================================
  // Selection Clearing Tests
  // ============================================================================

  public static async Task Should_clear_selection_on_cursor_movement()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.LeftArrow, shift: true);  // Start selection
    terminal.QueueKey(ConsoleKey.LeftArrow, shift: true);  // Extend selection
    terminal.QueueKey(ConsoleKey.LeftArrow);               // Move without shift (clears selection)
    terminal.QueueKey(ConsoleKey.X, ctrl: true);           // Cut should do nothing (no selection)
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello world")
        .WithHandler(() => "Selection cleared!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - cursor moved but no cut happened
    terminal.OutputContains("Selection cleared!").ShouldBeTrue("Non-shift movement should clear selection");
  }

  public static async Task Should_clear_selection_with_escape()
  {
    // Arrange - Escape clears the line entirely, so test that after Escape
    // the user can type a new command which executes correctly
    using TestTerminal terminal = new();
    terminal.QueueKeys("wrongcmd");
    terminal.QueueKey(ConsoleKey.Home);                     // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow, shift: true);  // Select 'w'
    terminal.QueueKey(ConsoleKey.RightArrow, shift: true);  // Select 'wr'
    terminal.QueueKey(ConsoleKey.Escape);                   // Clear line (and selection)
    terminal.QueueKeys("test");                             // Type new command
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test")
        .WithHandler(() => "Escape cleared!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - "test" command should execute (not "wrongcmd" or partial)
    terminal.OutputContains("Escape cleared!").ShouldBeTrue("Escape should clear line and selection");
  }

  // ============================================================================
  // CopyOrCancelLine Tests
  // ============================================================================

  public static async Task Should_cancel_line_with_ctrl_c_when_no_selection()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.C, ctrl: true);  // No selection - cancels line
    terminal.QueueKeys("test");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test")
        .WithHandler(() => "Cancel worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Cancel worked!").ShouldBeTrue("Ctrl+C with no selection should cancel line");
  }

  // ============================================================================
  // Cut Integration with Kill Ring Tests
  // ============================================================================

  public static async Task Should_add_cut_text_to_kill_ring()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.LeftArrow, ctrl: true, shift: true);  // Select "world"
    terminal.QueueKey(ConsoleKey.X, ctrl: true);                        // Cut (adds to kill ring)
    terminal.QueueKey(ConsoleKey.Y, ctrl: true);                        // Yank from kill ring
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello world")
        .WithHandler(() => "Kill ring worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Kill ring worked!").ShouldBeTrue("Cut should add to kill ring for yank");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.TextSelectionActions
