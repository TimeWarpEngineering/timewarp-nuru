#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Tests for PSReadLine-compatible undo/redo functionality (Task 043-005)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.UndoRedo
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("UndoRedo")]
public class UndoRedoTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<UndoRedoTests>();

  // ============================================================================
  // UndoStack Unit Tests
  // ============================================================================

  public static async Task UndoStack_should_save_and_restore_state()
  {
    // Arrange
    UndoStack stack = new();

    // Act - save state then undo
    stack.SaveState("hello", 5, isCharacterInput: false);
    UndoUnit? undone = stack.Undo("hello world", 11);

    // Assert
    undone.HasValue.ShouldBeTrue("Should have state to undo");
    undone!.Value.Text.ShouldBe("hello", "Undone text should match");
    undone.Value.CursorPosition.ShouldBe(5, "Undone cursor should match");

    await Task.CompletedTask;
  }

  public static async Task UndoStack_should_preserve_order_when_trimmed_past_capacity()
  {
    // Regression for kanban 454-006: TrimToCapacity reversed the list twice, so
    // exceeding MaxCapacity re-pushed the entries inverted — Ctrl+Z then walked
    // history from the OLDEST state forward.

    // Arrange - small capacity, then push past it
    UndoStack stack = new(maxCapacity: 3);
    for (int i = 1; i <= 5; i++)
    {
      stack.SaveState($"state-{i}", i, isCharacterInput: false);
    }

    // Assert - trimmed to capacity, and undo pops NEWEST-first (5, 4, 3)
    stack.UndoCount.ShouldBe(3, "Should be trimmed to MaxCapacity");

    UndoUnit? first = stack.Undo("current", 0);
    first!.Value.Text.ShouldBe("state-5", "First undo should restore the newest saved state");

    UndoUnit? second = stack.Undo("state-5", 5);
    second!.Value.Text.ShouldBe("state-4", "Second undo should walk backward in time");

    UndoUnit? third = stack.Undo("state-4", 4);
    third!.Value.Text.ShouldBe("state-3", "Third undo should walk backward in time");

    stack.CanUndo.ShouldBeFalse("Oldest states (1, 2) should have been trimmed away");

    await Task.CompletedTask;
  }

  public static async Task UndoStack_should_support_redo()
  {
    // Arrange
    UndoStack stack = new();
    stack.SaveState("hello", 5, isCharacterInput: false);
    stack.Undo("hello world", 11);

    // Act - redo
    UndoUnit? redone = stack.Redo("hello", 5);

    // Assert
    redone.HasValue.ShouldBeTrue("Should have state to redo");
    redone!.Value.Text.ShouldBe("hello world", "Redone text should match");
    redone.Value.CursorPosition.ShouldBe(11, "Redone cursor should match");

    await Task.CompletedTask;
  }

  public static async Task UndoStack_should_clear_redo_on_new_edit()
  {
    // Arrange
    UndoStack stack = new();
    stack.SaveState("hello", 5, isCharacterInput: false);
    stack.Undo("hello world", 11);
    stack.CanRedo.ShouldBeTrue("Should be able to redo before new edit");

    // Act - make new edit (clears redo)
    stack.SaveState("hello", 5, isCharacterInput: false);

    // Assert
    stack.CanRedo.ShouldBeFalse("Redo stack should be cleared after new edit");

    await Task.CompletedTask;
  }

  public static async Task UndoStack_should_group_consecutive_characters()
  {
    // Arrange
    UndoStack stack = new();

    // Act - save multiple character inputs (should be grouped)
    stack.SaveState("", 0, isCharacterInput: true);  // First char starts group
    stack.SaveState("h", 1, isCharacterInput: true); // Grouped, skipped
    stack.SaveState("he", 2, isCharacterInput: true); // Grouped, skipped
    stack.SaveState("hel", 3, isCharacterInput: true); // Grouped, skipped

    // Assert - only one undo unit should exist
    stack.UndoCount.ShouldBe(1, "Consecutive characters should be grouped into one undo unit");

    await Task.CompletedTask;
  }

  public static async Task UndoStack_should_end_grouping_on_non_character()
  {
    // Arrange
    UndoStack stack = new();
    stack.SaveState("", 0, isCharacterInput: true);  // Start group
    stack.SaveState("hello", 5, isCharacterInput: false); // End group, new unit

    // Assert - two undo units
    stack.UndoCount.ShouldBe(2, "Non-character edit should end grouping and create new unit");

    await Task.CompletedTask;
  }

  public static async Task UndoStack_should_return_initial_state()
  {
    // Arrange
    UndoStack stack = new();
    stack.SetInitialState("initial", 7);

    // Act
    UndoUnit initial = stack.GetInitialState();

    // Assert
    initial.Text.ShouldBe("initial", "Initial text should match");
    initial.CursorPosition.ShouldBe(7, "Initial cursor should match");

    await Task.CompletedTask;
  }

  // ============================================================================
  // Integration Tests
  // ============================================================================

  public static async Task Should_undo_typed_text_with_ctrl_z()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Z, ctrl: true);  // Undo
    terminal.QueueKeys("bye");  // Type new text
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("bye")
        .WithHandler(() => "Goodbye!")
        .AsQuery()
        .Done()
      .Map("hello")
        .WithHandler(() => "Hello!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - "bye" should have executed (after undo cleared "hello")
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Undo should have cleared typed text");
  }

  public static async Task Should_undo_kill_operation()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Home);  // Go to start
    terminal.QueueKey(ConsoleKey.K, ctrl: true);  // Kill to end of line
    terminal.QueueKey(ConsoleKey.Z, ctrl: true);  // Undo kill
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello world")
        .WithHandler(() => "Full text!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Full text!").ShouldBeTrue("Undo should restore killed text");
  }

  public static async Task Should_redo_with_ctrl_shift_z()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Z, ctrl: true);  // Undo
    terminal.QueueKey(ConsoleKey.Z, ctrl: true, shift: true);  // Redo
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Hello!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Hello!").ShouldBeTrue("Redo should restore undone text");
  }

  public static async Task Should_multiple_undo_operations()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("abc");
    terminal.QueueKey(ConsoleKey.Backspace);  // Delete 'c', creates undo unit
    terminal.QueueKey(ConsoleKey.Backspace);  // Delete 'b', creates undo unit
    terminal.QueueKey(ConsoleKey.Z, ctrl: true);  // Undo (restore 'b')
    terminal.QueueKey(ConsoleKey.Z, ctrl: true);  // Undo (restore 'c')
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("abc")
        .WithHandler(() => "ABC!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("ABC!").ShouldBeTrue("Multiple undos should restore deleted chars");
  }

  public static async Task Should_revert_line_with_alt_r()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Backspace);  // Delete 'o'
    terminal.QueueKeys(" world");  // Add " world"
    terminal.QueueKey(ConsoleKey.R, alt: true);  // Revert line to initial (empty)
    terminal.QueueKeys("status");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("status")
        .WithHandler(() => "OK!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("OK!").ShouldBeTrue("RevertLine should clear all changes");
  }

  public static async Task Should_handle_empty_undo_stack()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKey(ConsoleKey.Z, ctrl: true);  // Undo with nothing to undo
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Hello!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - should not crash, typing should work
    terminal.OutputContains("Hello!").ShouldBeTrue("Empty undo should be handled gracefully");
  }

  public static async Task Should_handle_empty_redo_stack()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKey(ConsoleKey.Z, ctrl: true, shift: true);  // Redo with nothing to redo
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Hello!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - should not crash, typing should work
    terminal.OutputContains("Hello!").ShouldBeTrue("Empty redo should be handled gracefully");
  }

  public static async Task Should_clear_redo_after_new_edit()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Z, ctrl: true);  // Undo
    terminal.QueueKeys("x");  // New edit (clears redo)
    terminal.QueueKey(ConsoleKey.Z, ctrl: true, shift: true);  // Redo should do nothing now
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("x")
        .WithHandler(() => "X!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - should execute "x" (redo was cleared)
    terminal.OutputContains("X!").ShouldBeTrue("Redo should be cleared after new edit");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.UndoRedo
