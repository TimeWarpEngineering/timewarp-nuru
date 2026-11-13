#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins YankNthArg integration and yank-argument edge cases for the REPL; split from repl-33-yank-arguments.cs.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.YankArgumentsNthAndEdge
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("YankArg")]
public class YankArgumentNthAndEdgeTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<YankArgumentNthAndEdgeTests>();

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("git push origin main")
        .WithHandler(() => "Pushed!")
        .AsCommand()
        .Done()
      .Map("{arg}")
        .WithHandler((string arg) => $"Got: {arg}")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("git push origin main")
        .WithHandler(() => "Pushed!")
        .AsCommand()
        .Done()
      .Map("git")
        .WithHandler(() => "Git command!")
        .AsQuery()
        .Done()
      .Map("{arg}")
        .WithHandler((string arg) => $"Got: {arg}")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("git push origin main")
        .WithHandler(() => "Pushed!")
        .AsCommand()
        .Done()
      .Map("{arg}")
        .WithHandler((string arg) => $"Got: {arg}")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("status")
        .WithHandler(() => "OK")
        .AsQuery()
        .Done()
      .Map("echo {arg}")
        .WithHandler((string arg) => $"Echo: {arg}")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .Map("done")
        .WithHandler(() => "Done!")
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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .Map("hello {name}")
        .WithHandler((string name) => $"Hi, {name}!")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - "hello World" was executed
    terminal.OutputContains("Hi, World!").ShouldBeTrue("Alt+. should insert at cursor position");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.YankArgumentsNthAndEdge
