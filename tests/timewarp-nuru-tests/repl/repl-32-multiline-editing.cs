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

  public static async Task Should_add_new_line_with_shift_enter()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);  // Shift+Enter adds new line
    terminal.QueueKeys("world");
    terminal.QueueKey(ConsoleKey.Enter);  // Execute multiline command
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("hello {*args}")
        .WithHandler((string[] args) => $"hello[{string.Join(" ", args)}]")
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

    // Assert - multiline input "hello\nworld" is joined to "hello world"
    // The "hello" route matches, "world" becomes args, NOT a separate command
    terminal.OutputContains("hello[").ShouldBeTrue("First line should match hello pattern");
    terminal.OutputContains("world").ShouldBeTrue("Second line content should appear in output");
    terminal.OutputContains("SHOULD NOT EXECUTE").ShouldBeFalse("World should not execute as separate command");
  }

  public static async Task Should_split_line_at_cursor_with_shift_enter()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("helloworld");
    terminal.QueueKey(ConsoleKey.Home);         // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow);   // Move to 'e'
    terminal.QueueKey(ConsoleKey.RightArrow);   // Move to 'l'
    terminal.QueueKey(ConsoleKey.RightArrow);   // Move to 'l'
    terminal.QueueKey(ConsoleKey.RightArrow);   // Move to 'o'
    terminal.QueueKey(ConsoleKey.RightArrow);   // Move past 'o', cursor before 'w'
    terminal.QueueKey(ConsoleKey.Enter, shift: true);  // Split here
    terminal.QueueKey(ConsoleKey.Enter);  // Execute
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "hello")
        .AsCommand()
        .Done()
      .Map("world")
        .WithHandler(() => "world")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - lines should be separated at cursor position
    terminal.OutputContains("hello").ShouldBeTrue("First part should be 'hello'");
    terminal.OutputContains("world").ShouldBeTrue("Second part should be 'world'");
  }

  public static async Task Should_add_multiple_lines_with_shift_enter()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("line1");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line2");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line3");
    terminal.QueueKey(ConsoleKey.Enter);  // Execute
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("line1 line2 line3")
        .WithHandler(() => "all-lines-executed")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - all three lines should be captured and executed as single command
    terminal.OutputContains("all-lines-executed").ShouldBeTrue("All three lines should be executed as one command");
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

    NuruCoreApp app = NuruApp.CreateBuilder([])
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

    NuruCoreApp app = NuruApp.CreateBuilder([])
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

    NuruCoreApp app = NuruApp.CreateBuilder([])
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

    NuruCoreApp app = NuruApp.CreateBuilder([])
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

  public static async Task Should_handle_shift_enter_on_empty_line()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKey(ConsoleKey.Enter, shift: true);  // Empty first line
    terminal.QueueKeys("content");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
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

    NuruCoreApp app = NuruApp.CreateBuilder([])
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
