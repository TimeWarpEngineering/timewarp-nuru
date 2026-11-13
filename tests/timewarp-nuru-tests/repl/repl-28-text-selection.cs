#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins PSReadLine-compatible text selection in the REPL line editor (task 043-006).
#endregion

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hel")
        .WithHandler(() => "Cut worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("llo")
        .WithHandler(() => "Forward select worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello ")
        .WithHandler(() => "Word select backward worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map(" world")
        .WithHandler(() => "Word select forward worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test")
        .WithHandler(() => "Shift+Home worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test")
        .WithHandler(() => "Shift+End worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("replaced")
        .WithHandler(() => "Select all worked!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Select all worked!").ShouldBeTrue("Ctrl+Shift+A should select all text");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.TextSelection
