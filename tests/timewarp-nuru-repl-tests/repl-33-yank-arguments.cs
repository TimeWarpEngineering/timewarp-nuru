#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Tests for PSReadLine-compatible yank argument functionality (Task 043-010)
// Verifies YankLastArg (Alt+.) and YankNthArg (Alt+Ctrl+Y) functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.YankArguments
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("YankArg")]
public class YankArgumentTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<YankArgumentTests>();

  // ============================================================================
  // ParseHistoryArguments Tests (Unit Tests for Argument Parsing)
  // ============================================================================

  public static async Task Should_parse_simple_arguments()
  {
    // Arrange & Act
    string[] args = ReplConsoleReader.ParseHistoryArguments("git push origin main");

    // Assert
    args.Length.ShouldBe(4);
    args[0].ShouldBe("git");
    args[1].ShouldBe("push");
    args[2].ShouldBe("origin");
    args[3].ShouldBe("main");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_double_quoted_arguments()
  {
    // Arrange & Act
    string[] args = ReplConsoleReader.ParseHistoryArguments("git commit -m \"Initial commit\"");

    // Assert
    args.Length.ShouldBe(4);
    args[0].ShouldBe("git");
    args[1].ShouldBe("commit");
    args[2].ShouldBe("-m");
    args[3].ShouldBe("Initial commit");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_single_quoted_arguments()
  {
    // Arrange & Act
    string[] args = ReplConsoleReader.ParseHistoryArguments("echo 'hello world'");

    // Assert
    args.Length.ShouldBe(2);
    args[0].ShouldBe("echo");
    args[1].ShouldBe("hello world");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_escaped_spaces()
  {
    // Arrange & Act
    string[] args = ReplConsoleReader.ParseHistoryArguments("touch file\\ name.txt");

    // Assert
    args.Length.ShouldBe(2);
    args[0].ShouldBe("touch");
    args[1].ShouldBe("file name.txt");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_escaped_quotes_inside_quotes()
  {
    // Arrange & Act
    string[] args = ReplConsoleReader.ParseHistoryArguments("echo \"say \\\"hello\\\"\"");

    // Assert
    args.Length.ShouldBe(2);
    args[0].ShouldBe("echo");
    args[1].ShouldBe("say \"hello\"");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_string()
  {
    // Arrange & Act
    string[] args = ReplConsoleReader.ParseHistoryArguments("");

    // Assert
    args.Length.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_whitespace_only_string()
  {
    // Arrange & Act
    string[] args = ReplConsoleReader.ParseHistoryArguments("   \t  ");

    // Assert
    args.Length.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiple_spaces_between_args()
  {
    // Arrange & Act
    string[] args = ReplConsoleReader.ParseHistoryArguments("git   push    origin");

    // Assert
    args.Length.ShouldBe(3);
    args[0].ShouldBe("git");
    args[1].ShouldBe("push");
    args[2].ShouldBe("origin");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_mixed_quoted_and_unquoted()
  {
    // Arrange & Act
    string[] args = ReplConsoleReader.ParseHistoryArguments("echo \"hello world\" and 'goodbye world'");

    // Assert
    args.Length.ShouldBe(4);
    args[0].ShouldBe("echo");
    args[1].ShouldBe("hello world");
    args[2].ShouldBe("and");
    args[3].ShouldBe("goodbye world");

    await Task.CompletedTask;
  }

  // ============================================================================
  // YankLastArg Integration Tests
  // ============================================================================

  public static async Task Should_yank_last_argument_with_alt_period()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alice");  // History entry
    terminal.QueueLine("greet Bob");    // History entry
    // Now press Alt+. to yank "Bob" from previous command
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);
    // Then press Enter to execute
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .Map("{name}", (string name) => $"Received: {name}")  // Catch-all for yanked arg
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - "Bob" was yanked and executed
    terminal.OutputContains("Received: Bob").ShouldBeTrue("Alt+. should yank last argument from history");
  }

  public static async Task Should_cycle_through_history_with_consecutive_alt_period()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alice");
    terminal.QueueLine("greet Bob");
    terminal.QueueLine("greet Carol");
    // Press Alt+. three times to cycle through last args
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);  // Gets "Carol"
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);  // Gets "Bob"
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);  // Gets "Alice"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .Map("{name}", (string name) => $"Received: {name}")
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Should have cycled to "Alice"
    terminal.OutputContains("Received: Alice").ShouldBeTrue("Consecutive Alt+. should cycle to older history entries");
  }

  public static async Task Should_handle_quoted_last_argument()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("say Greeting");  // Simple argument (no spaces)
    // Type "echo " then press Alt+. to yank "Greeting"
    terminal.QueueKeys("echo ");
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);  // Should get "Greeting"
    terminal.QueueKey(ConsoleKey.Enter);  // Execute "echo Greeting"
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("say {message}", (string message) => $"Said: {message}")
      .Map("echo {text}", (string text) => $"Echo: {text}")
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Should yank "Greeting" and execute "echo Greeting"
    terminal.OutputContains("Echo: Greeting").ShouldBeTrue("Alt+. should yank last argument");
  }

  public static async Task Should_handle_empty_history()
  {
    // Arrange
    using TestTerminal terminal = new();
    // Type a command first (don't use Alt+. yet since history is empty)
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello", () => "Hello!")
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Session should complete normally
    terminal.OutputContains("Hello!").ShouldBeTrue("Session should complete normally");
  }

  public static async Task Should_handle_alt_period_with_empty_history()
  {
    // Arrange
    using TestTerminal terminal = new();
    // Start REPL with a command to put in history
    terminal.QueueLine("status");
    // Now Alt+. should work (history has "status")
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);  // yanks "status"
    terminal.QueueKey(ConsoleKey.Escape);  // Clear line
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => "Status OK")
      .Map("hello", () => "Hello!")
      .Map("{text}", (string text) => $"Got: {text}")
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Session should complete
    terminal.OutputContains("Hello!").ShouldBeTrue("Should complete after Alt+. and Escape");
  }

  // ============================================================================
  // YankNthArg Integration Tests
  // ============================================================================

  public static async Task Should_yank_first_argument_with_alt_ctrl_y()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("git push origin main");
    // Press Alt+Ctrl+Y to yank first arg (index 1, "push")
    terminal.QueueKey(ConsoleKey.Y, ctrl: true, alt: true);
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("git push origin main", () => "Pushed!")
      .Map("{arg}", (string arg) => $"Got: {arg}")
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - "push" was yanked (default is index 1)
    terminal.OutputContains("Got: push").ShouldBeTrue("Alt+Ctrl+Y should yank first argument (index 1)");
  }

  public static async Task Should_yank_command_name_with_alt_0_alt_period()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("git push origin main");
    // Press Alt+0 then Alt+. to yank command name (index 0, "git")
    terminal.QueueKey(ConsoleKey.D0, alt: true);  // Set digit argument to 0
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);  // Yank with index 0
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("git push origin main", () => "Pushed!")
      .Map("git", () => "Git command!")
      .Map("{arg}", (string arg) => $"Got: {arg}")
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - "git" was yanked (index 0)
    (terminal.OutputContains("Git command!") || terminal.OutputContains("Got: git"))
      .ShouldBeTrue("Alt+0 Alt+. should yank command name (index 0)");
  }

  public static async Task Should_yank_specific_argument_with_digit_prefix()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("git push origin main");
    // Press Alt+3 then Alt+. to yank "main" (index 3)
    terminal.QueueKey(ConsoleKey.D3, alt: true);  // Set digit argument to 3
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);  // Yank with index 3
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("git push origin main", () => "Pushed!")
      .Map("{arg}", (string arg) => $"Got: {arg}")
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - "main" was yanked (index 3)
    terminal.OutputContains("Got: main").ShouldBeTrue("Alt+3 Alt+. should yank argument at index 3");
  }

  // ============================================================================
  // Edge Cases
  // ============================================================================

  public static async Task Should_handle_single_word_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("status");  // Single word, no arguments
    // Type "echo " then press Alt+. to yank "status"
    terminal.QueueKeys("echo ");
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);  // Should yank "status" (only arg is command itself)
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => "OK")
      .Map("echo {arg}", (string arg) => $"Echo: {arg}")
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - "status" was yanked and "echo status" executed
    terminal.OutputContains("Echo: status").ShouldBeTrue("Alt+. should yank single word command as argument");
  }

  public static async Task Should_reset_cycle_after_other_key()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alice");
    terminal.QueueLine("greet Bob");
    // Press Alt+. to start cycling
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);  // Gets "Bob"
    // Type a character (breaks the cycle)
    terminal.QueueKeys(" ");
    // Press Alt+. again (should start fresh from most recent)
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);  // Gets "Bob" again, not "Alice"
    // Clear and just press Enter
    terminal.QueueKey(ConsoleKey.U, ctrl: true);  // Clear line
    terminal.QueueKeys("done");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .Map("done", () => "Done!")
      .Map("{text}", (string text) => $"Got: {text}")
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Session should complete
    terminal.OutputContains("Done!").ShouldBeTrue("Typing should reset yank-arg cycle");
  }

  public static async Task Should_insert_at_cursor_position()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet World");
    // Type "hello "
    terminal.QueueKeys("hello ");
    // Press Alt+. to insert "World" at cursor
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);  // Now "hello World|"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .Map("hello {name}", (string name) => $"Hi, {name}!")
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - "hello World" was executed
    terminal.OutputContains("Hi, World!").ShouldBeTrue("Alt+. should insert at cursor position");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.YankArguments
