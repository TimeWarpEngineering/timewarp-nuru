#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Tests multiline editing functionality in the REPL.
// Validates Shift+Enter behavior, line splitting, continuation prompts,
// history integration, and mode reset after execution.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.MultilineEditing
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("Multiline")]
public class MultilineEditingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MultilineEditingTests>();

  // ============================================================================
  // Shift+Enter (AddLine) Tests
  // ============================================================================

  public static async Task Should_split_line_at_cursor_with_shift_enter()
  {
    #region Purpose
    // Shift+Enter splits the current line at the cursor position.
    // Input "echo helloworld" split at position 10 becomes "echo hello" + "world".
    // When executed, multiline input joins with space → "echo hello world".
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo helloworld");
    // Move cursor to position 10 (before 'w' in "world")
    // "echo helloworld" = positions 0-14, we want cursor at 10
    terminal.QueueKey(ConsoleKey.Home);                       // Position 0
    terminal.QueueKey(ConsoleKey.RightArrow, ctrl: true);     // Ctrl+Right: jump past "echo " to position 5
    terminal.QueueKey(ConsoleKey.RightArrow);                 // Position 6 (before 'e' in "hello")
    terminal.QueueKey(ConsoleKey.RightArrow);                 // Position 7 (before first 'l')
    terminal.QueueKey(ConsoleKey.RightArrow);                 // Position 8 (before second 'l')
    terminal.QueueKey(ConsoleKey.RightArrow);                 // Position 9 (before 'o')
    terminal.QueueKey(ConsoleKey.RightArrow);                 // Position 10 (before 'w')
    terminal.QueueKey(ConsoleKey.Enter, shift: true);         // Split: "echo hello" | "world"
    terminal.QueueKey(ConsoleKey.Enter);                      // Execute
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("echo {*args}")
        .WithHandler((string[] args) => $"ECHO:{string.Join(" ", args)}")
        .AsCommand()
        .Done()
      .Map("hello")
        .WithHandler(() => "SHOULD NOT EXECUTE")
        .AsCommand()
        .Done()
      .Map("world")
        .WithHandler(() => "SHOULD NOT EXECUTE")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - multiline input is joined and executed as ONE echo command
    terminal.OutputContains("ECHO:").ShouldBeTrue("Echo route should execute");
    terminal.OutputContains("hello").ShouldBeTrue("First part should be in output");
    terminal.OutputContains("world").ShouldBeTrue("Second part should be in output");
    terminal.OutputContains("SHOULD NOT EXECUTE").ShouldBeFalse("hello/world should not execute separately");
  }

  public static async Task Should_add_multiple_lines_with_shift_enter()
  {
    #region Purpose
    // shift-enter is used as a separator like space when matching commands
    #endregion

    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("line1");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line2");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line3");
    terminal.QueueKey(ConsoleKey.Enter);  // Execute
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("line1 line2 line3")
        .WithHandler(() => "command-matched")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - all three lines should be captured and executed as single command
    terminal.OutputContains("command-matched").ShouldBeTrue("All three lines should be executed as one command");
  }

  // ============================================================================
  // Enter Execution in Multiline Mode Tests
  // ============================================================================

  public static async Task Should_execute_full_multiline_input_on_enter()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("deploy");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("--env production");
    terminal.QueueKey(ConsoleKey.Enter);  // Execute
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {env}")
        .WithHandler((string env) => $"DEPLOYED: {env}")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - deploy command should execute with correct environment
    terminal.OutputContains("DEPLOYED: production").ShouldBeTrue("Deploy command should execute with env parameter");
  }

  // ============================================================================
  // History Integration Tests
  // ============================================================================

  public static async Task Should_add_multiline_input_to_history()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("testcmd");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("arg1");
    terminal.QueueKey(ConsoleKey.Enter);  // Execute first multiline command
    terminal.QueueLine("history");        // Check history
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("testcmd {*args}")
        .WithHandler((string[] args) => "OK")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - multiline command should be in history
    terminal.OutputContains("Command History:").ShouldBeTrue("History command should display");
    // The multiline command should appear in history output
    terminal.OutputContains("testcmd").ShouldBeTrue("Multiline command should be in history");
  }

  // ============================================================================
  // Continuation Prompt Tests
  // ============================================================================

  public static async Task Should_display_continuation_prompt()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("first");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("second");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("first")
        .WithHandler(() => "first-ok")
        .AsCommand()
        .Done()
      .Map("second")
        .WithHandler(() => "second-ok")
        .AsCommand()
        .Done()
      .AddRepl(options =>
      {
        options.EnableColors = false;
        options.ContinuationPrompt = ">> ";
      })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - continuation prompt should be visible in output
    terminal.OutputContains(">>").ShouldBeTrue("Should display continuation prompt");
  }

  public static async Task Should_use_custom_continuation_prompt()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("first");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("second");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("first")
        .WithHandler(() => "first-ok")
        .AsCommand()
        .Done()
      .Map("second")
        .WithHandler(() => "second-ok")
        .AsCommand()
        .Done()
      .AddRepl(options =>
      {
        options.EnableColors = false;
        options.ContinuationPrompt = "... ";  // Custom prompt
      })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("...").ShouldBeTrue("Should display custom continuation prompt");
  }

  // ============================================================================
  // Edge Cases
  // ============================================================================

  public static async Task Should_preserve_newline_inside_quoted_string()
  {
    #region Purpose
    // Verifies that newlines within quotes are preserved as part of the argument,
    // matching PSReadLine behavior. Quotes are stripped but content is preserved.
    #endregion

    // Arrange - multiline quoted string should be a SINGLE argument
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);  // Enter multiline mode
    terminal.QueueKeys("\"hello");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);  // Continue inside quotes
    terminal.QueueKeys("world\"");
    terminal.QueueKey(ConsoleKey.Enter);  // Execute
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("echo {message}")  // Single parameter, NOT catch-all
        .WithHandler((string message) => $"ECHO:message='{message}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - quotes stripped, newline preserved, single argument
    terminal.OutputContains("ECHO:message='hello").ShouldBeTrue("First part preserved");
    terminal.OutputContains("world'").ShouldBeTrue("Second part preserved");
  }

  public static async Task Should_handle_shift_enter_on_empty_line()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKey(ConsoleKey.Enter, shift: true);  // Empty first line
    terminal.QueueKeys("content");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("content")
        .WithHandler(() => "content-ok")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - should handle empty first line gracefully
    terminal.OutputContains("content-ok").ShouldBeTrue("Should handle content after empty first line");
  }

  public static async Task Should_reset_multiline_mode_after_execution()
  {
    // Arrange - Test that after multiline input, single-line commands still work
    using TestTerminal terminal = new();
    // Multiline: type "multi", Shift+Enter, type "line", Enter
    terminal.QueueKeys("multi");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line");
    terminal.QueueKey(ConsoleKey.Enter);
    // Single-line: type "test" and Enter
    terminal.QueueKeys("test");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test")
        .WithHandler(() => "TEST OK")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - single-line command should work after multiline
    // If multiline mode didn't reset, "test" wouldn't be recognized as a command
    terminal.OutputContains("TEST OK").ShouldBeTrue("Single-line command should work after multiline");
  }
}
}

// EOF
