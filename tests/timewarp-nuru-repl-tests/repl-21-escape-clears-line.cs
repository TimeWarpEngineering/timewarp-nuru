#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Test that Escape key clears the current line (PSReadLine RevertLine behavior)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.EscapeClearsLine
{

[TestTag("REPL")]
public class EscapeClearsLineTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EscapeClearsLineTests>();

  [Timeout(5000)]
  public static async Task Should_clear_line_on_escape()
  {
    // Arrange: Type some invalid text then press Escape, then valid command
    using TestTerminal terminal = new();
    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => 0)
      .AddReplSupport(options =>
      {
        options.Prompt = "demo> ";
        options.EnableArrowHistory = true;
      })
      .Build();

    terminal.QueueKeys("asdfasdf");
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    // Act
    await app.RunReplAsync();

    // Assert: The output should show "status" was typed (after Escape cleared "asdfasdf")
    terminal.OutputContains("status")
      .ShouldBeTrue("Should show 'status' command in output");

    // Should NOT show error for "asdfasdf" (it was cleared by Escape)
    terminal.OutputContains("No matching command found")
      .ShouldBeFalse("Should NOT show error since 'asdfasdf' was cleared by Escape");

    // Should NOT try to execute "asdfasdfstatus"
    terminal.OutputContains("asdfasdfstatus")
      .ShouldBeFalse("Should NOT have concatenated the cleared text with new input");
  }

  [Timeout(5000)]
  public static async Task Should_clear_partial_command_on_escape()
  {
    // Arrange: Type partial command then Escape
    using TestTerminal terminal = new();
    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => 0)
      .Map("start", () => 0)
      .AddReplSupport(options =>
      {
        options.Prompt = "> ";
        options.EnableArrowHistory = true;
      })
      .Build();

    terminal.QueueKeys("sta");  // Partial "status" or "start"
    terminal.QueueKey(ConsoleKey.Escape);  // Clear it
    terminal.QueueLine("status");  // Type full command
    terminal.QueueLine("exit");

    // Act
    await app.RunReplAsync();

    // Assert: Should show "status" was executed successfully
    terminal.OutputContains("status")
      .ShouldBeTrue("Should show 'status' command in output");

    terminal.OutputContains("No matching command found")
      .ShouldBeFalse("Should NOT show error since 'sta' was cleared by Escape");
  }

  [Timeout(5000)]
  public static async Task Should_clear_line_during_tab_completion()
  {
    // Arrange: Start tab completion then Escape to cancel
    using TestTerminal terminal = new();
    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => 0)
      .Map("start", () => 0)
      .AddReplSupport(options =>
      {
        options.Prompt = "> ";
        options.EnableArrowHistory = true;
      })
      .Build();

    terminal.QueueKeys("s");  // Ambiguous prefix
    terminal.QueueKey(ConsoleKey.Tab);  // Show completions
    terminal.QueueKey(ConsoleKey.Escape);  // Clear line AND completion state
    terminal.QueueLine("status");  // Fresh command
    terminal.QueueLine("exit");

    // Act
    await app.RunReplAsync();

    // Assert: Should show completions were displayed, then cleared, then status executed
    terminal.OutputContains("Available completions")
      .ShouldBeTrue("Should have shown completions for 's'");

    terminal.OutputContains("status")
      .ShouldBeTrue("Should execute 'status' after Escape cleared both input and completions");

    terminal.OutputContains("No matching command found")
      .ShouldBeFalse("Should NOT show error after Escape");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.EscapeClearsLine
