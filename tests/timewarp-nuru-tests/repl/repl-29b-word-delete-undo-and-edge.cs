#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins BackwardDeleteWord, word operations with undo, combined word operations, and edge cases; split from repl-29-word-operations.cs.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.WordOperationsDeleteUndoEdge
{

[TestTag("REPL")]
public class WordOperationDeleteUndoEdgeTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<WordOperationDeleteUndoEdgeTests>();

  // === BackwardDeleteWord Tests (Ctrl+Backspace) ===

  public static async Task Should_delete_word_backward_with_ctrl_backspace()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Backspace, ctrl: true); // Delete "world"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello ")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("Ctrl+Backspace should delete word backward");

    await Task.CompletedTask;
  }

  // === Word Operations with Undo ===

  public static async Task Should_undo_upcase_word()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.U, alt: true); // Upcase -> "HELLO"
    terminal.QueueKey(ConsoleKey.Z, ctrl: true); // Undo -> "hello"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("Undo should revert UpcaseWord");

    await Task.CompletedTask;
  }

  public static async Task Should_undo_swap_characters()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("teh");
    terminal.QueueKey(ConsoleKey.T, ctrl: true); // Swap -> "the"
    terminal.QueueKey(ConsoleKey.Z, ctrl: true); // Undo -> "teh"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("teh")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("Undo should revert SwapCharacters");

    await Task.CompletedTask;
  }

  // === Multiple Word Operations ===

  public static async Task Should_capitalize_multiple_words()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.C, alt: true); // Capitalize "hello" -> "Hello", cursor at space
    terminal.QueueKey(ConsoleKey.C, alt: true); // Capitalize "world" -> "World"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("Hello World")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("CapitalizeWord should work on consecutive words");

    await Task.CompletedTask;
  }

  // === Edge Cases ===

  public static async Task Should_handle_empty_input_for_upcase()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKey(ConsoleKey.U, alt: true); // Upcase on empty line - no-op
    terminal.QueueKeys("test");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("UpcaseWord on empty input should be no-op");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_single_char_for_swap()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("a");
    terminal.QueueKey(ConsoleKey.T, ctrl: true); // Swap with single char - no-op
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("a")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("SwapCharacters on single char should be no-op");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_cursor_at_end_for_case_operations()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    // Cursor is at end after typing
    terminal.QueueKey(ConsoleKey.U, alt: true); // Upcase at end - no-op
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("Case operation at end of line should be no-op");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.WordOperationsDeleteUndoEdge
