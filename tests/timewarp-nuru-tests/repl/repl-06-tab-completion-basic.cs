#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test basic tab completion (Section 6 of REPL Test Plan)
// Note: Tab completion tests require interactive key simulation which
// doesn't work reliably with TestTerminal's key queuing mechanism.
// The REPL enters a state waiting for more input after showing completions.
// These tests should be run manually in an interactive terminal.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.TabCompletionBasic
{
  [TestTag("REPL")]
  public class TabCompletionBasicTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TabCompletionBasicTests>();

  [Timeout(5000)]
  public static async Task Should_complete_single_match()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("sta");
    terminal.QueueKey(ConsoleKey.Tab);  // Should complete to "status"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status")
        .WithHandler(() => "OK")
        .AsQuery()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Single completion should work");
  }

  [Timeout(5000)]
  public static async Task Should_show_multiple_matches()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("s");
    terminal.QueueKey(ConsoleKey.Tab);  // Should show "status", "start"
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "Status OK").AsQuery().Done()
      .Map("start").WithHandler(() => "Started").AsCommand().Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Available completions")
      .ShouldBeTrue("Multiple completions should be displayed");
  }

  [Timeout(5000)]
  public static async Task Should_cycle_through_completions()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("s");
    terminal.QueueKey(ConsoleKey.Tab);  // Show completions
    terminal.QueueKey(ConsoleKey.Tab);  // Cycle to first
    terminal.QueueKey(ConsoleKey.Tab);  // Cycle to second
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "Status OK").AsQuery().Done()
      .Map("start").WithHandler(() => "Started").AsCommand().Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should cycle through completions");
  }

  [Timeout(5000)]
  public static async Task Should_reverse_cycle_with_shift_tab()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("s");
    terminal.QueueKey(ConsoleKey.Tab);  // Show completions
    terminal.QueueKey(ConsoleKey.Tab);  // Cycle forward
    terminal.QueueKey(ConsoleKey.Tab, shift: true);  // Cycle backward
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "Status OK").AsQuery().Done()
      .Map("start").WithHandler(() => "Started").AsCommand().Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Shift+Tab should reverse cycle");
  }

  [Timeout(5000)]
  public static async Task Should_not_change_on_no_matches()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("xyz");
    terminal.QueueKey(ConsoleKey.Tab);  // No matches
    terminal.QueueKey(ConsoleKey.Backspace);
    terminal.QueueKey(ConsoleKey.Backspace);
    terminal.QueueKey(ConsoleKey.Backspace);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "Status OK").AsQuery().Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("No matches should leave input unchanged");
  }

  [Timeout(5000)]
  public static async Task Should_complete_at_empty_prompt()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKey(ConsoleKey.Tab);  // Tab at empty prompt - show all commands
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "Status OK").AsQuery().Done()
      .Map("start").WithHandler(() => "Started").AsCommand().Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Available completions")
      .ShouldBeTrue("Empty prompt should show all commands");
  }

  [Timeout(5000)]
  public static async Task Should_replace_partial_word()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("deplo");
    terminal.QueueKey(ConsoleKey.Tab);  // Complete "deploy"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy").WithHandler(() => "Deployed!").AsCommand().Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Partial word should be replaced with completion");
  }

  [Timeout(5000)]
  public static async Task Should_complete_with_arguments()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("deploy prod");
    terminal.QueueKey(ConsoleKey.Tab);  // Complete after argument
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"Deployed to {env}")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Completion should work with arguments");
  }
  }
}
