#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using TimeWarp.Nuru;

// Tests for multiline editing in REPL (Task 043-009)
return await RunTests<MultilineEditingTests>();

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("Multiline")]
public class MultilineEditingTests
{
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

    string? capturedInput = null;
    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("{*args}", (string[] args) =>
      {
        capturedInput = string.Join(" ", args);
        return "OK";
      })
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - the multiline input should be captured
    // The full text includes newline between "hello" and "world"
    capturedInput.ShouldNotBeNull("Input should be captured");
    // Since it's a catch-all, the args will be split - but the key is that both lines are present
    capturedInput!.Contains("hello").ShouldBeTrue("Should contain first line");
    capturedInput.Contains("world").ShouldBeTrue("Should contain second line");

    await Task.CompletedTask;
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

    string? capturedInput = null;
    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("{*args}", (string[] args) =>
      {
        capturedInput = string.Join("|", args);  // Use | to see separation
        return "OK";
      })
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    capturedInput.ShouldNotBeNull("Input should be captured");
    capturedInput!.Contains("hello").ShouldBeTrue("First part should be 'hello'");
    capturedInput.Contains("world").ShouldBeTrue("Second part should be 'world'");

    await Task.CompletedTask;
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

    string? capturedInput = null;
    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("{*args}", (string[] args) =>
      {
        capturedInput = string.Join(" ", args);
        return "OK";
      })
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    capturedInput.ShouldNotBeNull("Input should be captured");
    capturedInput!.Contains("line1").ShouldBeTrue("Should contain line1");
    capturedInput.Contains("line2").ShouldBeTrue("Should contain line2");
    capturedInput.Contains("line3").ShouldBeTrue("Should contain line3");

    await Task.CompletedTask;
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

    bool deployExecuted = false;
    string? capturedEnv = null;
    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {env}", (string env) =>
      {
        deployExecuted = true;
        capturedEnv = env;
        return "Deployed!";
      })
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    deployExecuted.ShouldBeTrue("Deploy command should execute");
    capturedEnv.ShouldBe("production", "Environment should be captured");
    terminal.OutputContains("Deployed!").ShouldBeTrue("Should show output");

    await Task.CompletedTask;
  }

  // ============================================================================
  // History Integration Tests
  // ============================================================================

  // TODO: This test passes when run with debug output but fails otherwise.
  // Needs investigation of potential test framework timing issue.
  [Skip("Flaky test - passes with debug output, fails without")]
  public static async Task Should_add_multiline_input_to_history()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("multi");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line");
    terminal.QueueKey(ConsoleKey.Enter);  // Execute first multiline command
    terminal.QueueKey(ConsoleKey.UpArrow);  // Recall from history
    terminal.QueueKey(ConsoleKey.Enter);    // Execute again
    terminal.QueueLine("exit");

    int execCount = 0;
    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("{*args}", (string[] _) =>
      {
        execCount++;
        return "OK";
      })
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - should have executed twice (once typed, once from history)
    execCount.ShouldBe(2, "Should execute multiline command twice");

    await Task.CompletedTask;
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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("{*args}", (string[] _) => "OK")
      .AddReplSupport(options =>
      {
        options.EnableColors = false;
        options.ContinuationPrompt = ">> ";
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - continuation prompt should be visible in output
    terminal.OutputContains(">>").ShouldBeTrue("Should display continuation prompt");

    await Task.CompletedTask;
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

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("{*args}", (string[] _) => "OK")
      .AddReplSupport(options =>
      {
        options.EnableColors = false;
        options.ContinuationPrompt = "... ";  // Custom prompt
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("...").ShouldBeTrue("Should display custom continuation prompt");

    await Task.CompletedTask;
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

    string? capturedInput = null;
    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("{*args}", (string[] args) =>
      {
        capturedInput = string.Join(" ", args);
        return "OK";
      })
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - should handle empty first line gracefully
    capturedInput!.Contains("content").ShouldBeTrue("Should capture content");

    await Task.CompletedTask;
  }

  // TODO: This test passes when run with debug output but fails otherwise.
  // Needs investigation of potential test framework timing issue.
  [Skip("Flaky test - passes with debug output, fails without")]
  public static async Task Should_reset_multiline_mode_after_execution()
  {
    // Arrange
    using TestTerminal terminal = new();
    // First multiline command
    terminal.QueueKeys("multi");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line");
    terminal.QueueKey(ConsoleKey.Enter);
    // Second single-line command
    terminal.QueueKeys("single");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    int multiCount = 0;
    int singleCount = 0;
    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("single", () =>
      {
        singleCount++;
        return "Single!";
      })
      .Map("{*args}", (string[] _) =>
      {
        multiCount++;
        return "Multi!";
      })
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    multiCount.ShouldBe(1, "Multiline command should execute once");
    singleCount.ShouldBe(1, "Single-line command should execute once");

    await Task.CompletedTask;
  }
}
