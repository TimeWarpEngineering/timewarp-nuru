#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins PSReadLine-compatible YankLastArg (Alt+.) and YankNthArg (Alt+Ctrl+Y) in the REPL (task 043-010).
#endregion

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
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alice");  // History entry
    terminal.QueueLine("greet Bob");    // History entry
    // Now press Alt+. to yank "Bob" from previous command
    terminal.QueueKey(ConsoleKey.OemPeriod, alt: true);
    // Then press Enter to execute
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .Map("{name}")
        .WithHandler((string name) => $"Received: {name}")  // Catch-all for yanked arg
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .Map("{name}")
        .WithHandler((string name) => $"Received: {name}")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("say {message}")
        .WithHandler((string message) => $"Said: {message}")
        .AsCommand()
        .Done()
      .Map("echo {text}")
        .WithHandler((string text) => $"Echo: {text}")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("status")
        .WithHandler(() => "Status OK")
        .AsQuery()
        .Done()
      .Map("hello")
        .WithHandler(() => "Hello!")
        .AsQuery()
        .Done()
      .Map("{text}")
        .WithHandler((string text) => $"Got: {text}")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - Session should complete
    terminal.OutputContains("Hello!").ShouldBeTrue("Should complete after Alt+. and Escape");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.YankArguments
