#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Test PSReadLine word operations (case conversion, character transposition)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.WordOperations
{

[TestTag("REPL")]
public class WordOperationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<WordOperationTests>();

  // === UpcaseWord Tests (Alt+U) ===

  public static async Task Should_upcase_word_from_cursor_to_end()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.U, alt: true); // Upcase "hello" -> "HELLO"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("HELLO world")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("UpcaseWord should convert word to uppercase");

    await Task.CompletedTask;
  }

  public static async Task Should_upcase_from_middle_of_word()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'h'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'e' (cursor at 'l')
    terminal.QueueKey(ConsoleKey.U, alt: true); // Upcase "llo" -> "LLO"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("heLLO")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("UpcaseWord should convert from cursor to end of word");

    await Task.CompletedTask;
  }

  // === DowncaseWord Tests (Alt+L) ===

  public static async Task Should_downcase_word_from_cursor_to_end()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("HELLO WORLD");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.L, alt: true); // Downcase "HELLO" -> "hello"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello WORLD")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("DowncaseWord should convert word to lowercase");

    await Task.CompletedTask;
  }

  public static async Task Should_downcase_from_middle_of_word()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("HELLO");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'H'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'E' (cursor at 'L')
    terminal.QueueKey(ConsoleKey.L, alt: true); // Downcase "LLO" -> "llo"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("HEllo")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("DowncaseWord should convert from cursor to end of word");

    await Task.CompletedTask;
  }

  // === CapitalizeWord Tests (Alt+C) ===

  public static async Task Should_capitalize_word_from_cursor()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.C, alt: true); // Capitalize "hello" -> "Hello"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("Hello world")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("CapitalizeWord should capitalize first char, lowercase rest");

    await Task.CompletedTask;
  }

  public static async Task Should_capitalize_uppercase_word()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("HELLO");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.C, alt: true); // Capitalize "HELLO" -> "Hello"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("Hello")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("CapitalizeWord should handle all-caps words");

    await Task.CompletedTask;
  }

  // === SwapCharacters Tests (Ctrl+T) ===

  public static async Task Should_swap_characters_at_cursor()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("teh");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 't'
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move to after 'e' (cursor at 'h')
    terminal.QueueKey(ConsoleKey.T, ctrl: true); // Swap 'e' and 'h' -> "the"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("the")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("SwapCharacters should transpose characters");

    await Task.CompletedTask;
  }

  public static async Task Should_swap_at_end_of_line()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("teh");
    // Cursor is at end after typing
    terminal.QueueKey(ConsoleKey.T, ctrl: true); // Swap 'e' and 'h' -> "the"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("the")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("SwapCharacters at end should swap last two chars");

    await Task.CompletedTask;
  }

  public static async Task Should_swap_at_beginning_of_line()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("eth");
    terminal.QueueKey(ConsoleKey.Home);         // Go to start
    terminal.QueueKey(ConsoleKey.T, ctrl: true); // Swap first two chars -> "teh"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("teh")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("SwapCharacters at beginning should swap first two chars");

    await Task.CompletedTask;
  }

  // === BackwardDeleteWord Tests (Ctrl+Backspace) ===

  public static async Task Should_delete_word_backward_with_ctrl_backspace()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Backspace, ctrl: true); // Delete "world"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello ")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("teh")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("Hello World")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("test")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("a")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Success!")
      .ShouldBeTrue("Case operation at end of line should be no-op");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.WordOperations
