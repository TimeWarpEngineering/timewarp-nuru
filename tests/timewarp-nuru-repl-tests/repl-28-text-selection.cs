#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Tests for PSReadLine-compatible text selection functionality (Task 043-006)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.TextSelection
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("Selection")]
public class TextSelectionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TextSelectionTests>();

  // ============================================================================
  // Selection Unit Tests
  // ============================================================================

  public static async Task Selection_should_track_anchor_and_cursor()
  {
    // Arrange
    Selection selection = new();

    // Act
    selection.StartAt(5);
    selection.ExtendTo(10);

    // Assert
    selection.IsActive.ShouldBeTrue("Selection should be active");
    selection.Anchor.ShouldBe(5, "Anchor should be at start position");
    selection.Cursor.ShouldBe(10, "Cursor should be at extended position");
    selection.Start.ShouldBe(5, "Start should be minimum of anchor and cursor");
    selection.End.ShouldBe(10, "End should be maximum of anchor and cursor");
    selection.Length.ShouldBe(5, "Length should be end - start");

    await Task.CompletedTask;
  }

  public static async Task Selection_should_handle_backward_selection()
  {
    // Arrange
    Selection selection = new();

    // Act - select backward (cursor before anchor)
    selection.StartAt(10);
    selection.ExtendTo(5);

    // Assert
    selection.IsActive.ShouldBeTrue("Selection should be active");
    selection.Anchor.ShouldBe(10, "Anchor should remain at start position");
    selection.Cursor.ShouldBe(5, "Cursor should be at extended position");
    selection.Start.ShouldBe(5, "Start should be minimum (cursor)");
    selection.End.ShouldBe(10, "End should be maximum (anchor)");
    selection.Length.ShouldBe(5, "Length should be 5");

    await Task.CompletedTask;
  }

  public static async Task Selection_should_not_be_active_when_anchor_equals_cursor()
  {
    // Arrange
    Selection selection = new();

    // Act
    selection.StartAt(5);
    selection.ExtendTo(5);

    // Assert
    selection.IsActive.ShouldBeFalse("Selection should not be active when anchor equals cursor");
    selection.Length.ShouldBe(0, "Length should be 0");

    await Task.CompletedTask;
  }

  public static async Task Selection_should_get_selected_text()
  {
    // Arrange
    Selection selection = new();
    string text = "hello world";

    // Act
    selection.StartAt(0);
    selection.ExtendTo(5);
    string selectedText = selection.GetSelectedText(text);

    // Assert
    selectedText.ShouldBe("hello", "Should extract selected portion");

    await Task.CompletedTask;
  }

  public static async Task Selection_should_clear()
  {
    // Arrange
    Selection selection = new();
    selection.StartAt(0);
    selection.ExtendTo(5);
    selection.IsActive.ShouldBeTrue("Should be active before clear");

    // Act
    selection.Clear();

    // Assert
    selection.IsActive.ShouldBeFalse("Should not be active after clear");
    selection.Anchor.ShouldBe(-1, "Anchor should be reset");
    selection.Cursor.ShouldBe(-1, "Cursor should be reset");

    await Task.CompletedTask;
  }

  // ============================================================================
  // Character Selection Integration Tests
  // ============================================================================

  public static async Task Should_select_backward_char_with_shift_left()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.LeftArrow, shift: true);  // Select 'o'
    terminal.QueueKey(ConsoleKey.LeftArrow, shift: true);  // Select 'lo'
    terminal.QueueKey(ConsoleKey.X, ctrl: true);           // Cut selection
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hel")
        .WithHandler(() => "Cut worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Cut worked!").ShouldBeTrue("Shift+Left should select characters backward");
  }

  public static async Task Should_select_forward_char_with_shift_right()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Home);                     // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow, shift: true);  // Select 'h'
    terminal.QueueKey(ConsoleKey.RightArrow, shift: true);  // Select 'he'
    terminal.QueueKey(ConsoleKey.X, ctrl: true);            // Cut selection
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("llo")
        .WithHandler(() => "Forward select worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Forward select worked!").ShouldBeTrue("Shift+Right should select characters forward");
  }

  // ============================================================================
  // Word Selection Integration Tests
  // ============================================================================

  public static async Task Should_select_backward_word_with_ctrl_shift_left()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.LeftArrow, ctrl: true, shift: true);  // Select "world"
    terminal.QueueKey(ConsoleKey.X, ctrl: true);                        // Cut selection
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello ")
        .WithHandler(() => "Word select backward worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Word select backward worked!").ShouldBeTrue("Ctrl+Shift+Left should select word backward");
  }

  public static async Task Should_select_forward_word_with_ctrl_shift_right()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Home);                                  // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow, ctrl: true, shift: true);   // Select "hello"
    terminal.QueueKey(ConsoleKey.X, ctrl: true);                         // Cut selection
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map(" world")
        .WithHandler(() => "Word select forward worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Word select forward worked!").ShouldBeTrue("Ctrl+Shift+Right should select word forward");
  }

  // ============================================================================
  // Line Selection Integration Tests
  // ============================================================================

  public static async Task Should_select_to_line_start_with_shift_home()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Home, shift: true);  // Select all to start
    terminal.QueueKey(ConsoleKey.X, ctrl: true);      // Cut selection
    terminal.QueueKeys("test");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("test")
        .WithHandler(() => "Shift+Home worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Shift+Home worked!").ShouldBeTrue("Shift+Home should select to line start");
  }

  public static async Task Should_select_to_line_end_with_shift_end()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Home);               // Go to start
    terminal.QueueKey(ConsoleKey.End, shift: true);   // Select all to end
    terminal.QueueKey(ConsoleKey.X, ctrl: true);      // Cut selection
    terminal.QueueKeys("test");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("test")
        .WithHandler(() => "Shift+End worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Shift+End worked!").ShouldBeTrue("Shift+End should select to line end");
  }

  public static async Task Should_select_all_with_ctrl_shift_a()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.A, ctrl: true, shift: true);  // Select all
    terminal.QueueKey(ConsoleKey.X, ctrl: true);               // Cut selection
    terminal.QueueKeys("replaced");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("replaced")
        .WithHandler(() => "Select all worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Select all worked!").ShouldBeTrue("Ctrl+Shift+A should select all text");
  }

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello ")
        .WithHandler(() => "Backspace delete worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello ")
        .WithHandler(() => "Delete key worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello universe")
        .WithHandler(() => "Replace worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello world")
        .WithHandler(() => "Selection cleared!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("test")
        .WithHandler(() => "Escape cleared!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("test")
        .WithHandler(() => "Cancel worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello world")
        .WithHandler(() => "Kill ring worked!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Kill ring worked!").ShouldBeTrue("Cut should add to kill ring for yank");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.TextSelection
