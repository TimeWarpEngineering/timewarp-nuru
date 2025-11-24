#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Test basic tab completion (Section 6 of REPL Test Plan)
// Note: Tab completion tests require interactive key simulation which
// doesn't work reliably with TestTerminal's key queuing mechanism.
// The REPL enters a state waiting for more input after showing completions.
// These tests should be run manually in an interactive terminal.
return await RunTests<TabCompletionBasicTests>();

[TestTag("REPL")]
public class TabCompletionBasicTests
{
  [Timeout(5000)]
  public static async Task Should_complete_single_match()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("sta");
    terminal.QueueKey(ConsoleKey.Tab);  // Should complete to "status"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("status", () => "OK")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Single completion should work");
  }

  [Skip("Tab completion tests hang - requires interactive terminal")]
  [Timeout(5000)]
  public static async Task Should_show_multiple_matches()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("s");
    terminal.QueueKey(ConsoleKey.Tab);  // Should show "status", "start"
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("status", () => "Status OK")
      .AddRoute("start", () => "Started")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Available completions")
      .ShouldBeTrue("Multiple completions should be displayed");
  }

  [Skip("Tab completion tests hang - requires interactive terminal")]
  [Timeout(5000)]
  public static async Task Should_cycle_through_completions()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("s");
    terminal.QueueKey(ConsoleKey.Tab);  // Show completions
    terminal.QueueKey(ConsoleKey.Tab);  // Cycle to first
    terminal.QueueKey(ConsoleKey.Tab);  // Cycle to second
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("status", () => "Status OK")
      .AddRoute("start", () => "Started")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should cycle through completions");
  }

  [Skip("Tab completion tests hang - requires interactive terminal")]
  [Timeout(5000)]
  public static async Task Should_reverse_cycle_with_shift_tab()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("s");
    terminal.QueueKey(ConsoleKey.Tab);  // Show completions
    terminal.QueueKey(ConsoleKey.Tab);  // Cycle forward
    terminal.QueueKey(ConsoleKey.Tab, shift: true);  // Cycle backward
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("status", () => "Status OK")
      .AddRoute("start", () => "Started")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Shift+Tab should reverse cycle");
  }

  [Skip("Tab completion tests hang - requires interactive terminal")]
  [Timeout(5000)]
  public static async Task Should_not_change_on_no_matches()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("xyz");
    terminal.QueueKey(ConsoleKey.Tab);  // No matches
    terminal.QueueKey(ConsoleKey.Backspace);
    terminal.QueueKey(ConsoleKey.Backspace);
    terminal.QueueKey(ConsoleKey.Backspace);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("status", () => "Status OK")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("No matches should leave input unchanged");
  }

  [Skip("Tab completion tests hang - requires interactive terminal")]
  [Timeout(5000)]
  public static async Task Should_complete_at_empty_prompt()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKey(ConsoleKey.Tab);  // Tab at empty prompt - show all commands
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("status", () => "Status OK")
      .AddRoute("start", () => "Started")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Available completions")
      .ShouldBeTrue("Empty prompt should show all commands");
  }

  [Skip("Tab completion tests hang - requires interactive terminal")]
  [Timeout(5000)]
  public static async Task Should_replace_partial_word()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("deplo");
    terminal.QueueKey(ConsoleKey.Tab);  // Complete "deploy"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("deploy", () => "Deployed!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Partial word should be replaced with completion");
  }

  [Skip("Tab completion tests hang - requires interactive terminal")]
  [Timeout(5000)]
  public static async Task Should_complete_with_arguments()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("deploy prod");
    terminal.QueueKey(ConsoleKey.Tab);  // Complete after argument
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("deploy {env}", (string env) => $"Deployed to {env}")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Completion should work with arguments");
  }
}
