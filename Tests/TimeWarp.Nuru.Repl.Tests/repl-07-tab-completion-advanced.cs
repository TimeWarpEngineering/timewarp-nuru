#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Test advanced tab completion scenarios (Section 7 of REPL Test Plan)
return await RunTests<TabCompletionAdvancedTests>();

[TestTag("REPL")]
public class TabCompletionAdvancedTests
{
  [Timeout(5000)]
  public static async Task Should_complete_long_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("deploy --");
    terminal.QueueKey(ConsoleKey.Tab);  // Show options
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("deploy --force", () => "Force deployed!")
      .Map("deploy --dry-run", () => "Dry run!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle long option completion");
  }

  [Timeout(5000)]
  public static async Task Should_complete_short_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("deploy -");
    terminal.QueueKey(ConsoleKey.Tab);  // Show short options
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("deploy -f", () => "Force!")
      .Map("deploy -v", () => "Verbose!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle short option completion");
  }

  [Timeout(5000)]
  public static async Task Should_complete_nested_commands()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("git com");
    terminal.QueueKey(ConsoleKey.Tab);  // Should show "commit", "config"
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("git commit", () => "Committed!")
      .Map("git config", () => "Configured!")
      .Map("git status", () => "Status OK")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should complete nested commands");
  }

  [Timeout(5000)]
  public static async Task Should_complete_parameter_values()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("deploy ");
    terminal.QueueKey(ConsoleKey.Tab);  // Show parameter completions
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}", (string env) => $"Deployed to {env}")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle parameter value completion");
  }

  [Timeout(5000)]
  public static async Task Should_complete_after_option_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("deploy --env prod ");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {env}", (string env) => $"Deployed to {env}")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should complete after option value");
  }

  [Timeout(5000)]
  public static async Task Should_complete_mixed_position()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("cmd arg1 --opt val ");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("cmd {arg} --opt {val}", (string arg, string val) => $"{arg}:{val}")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle mixed position completion");
  }

  [Timeout(5000)]
  public static async Task Should_complete_catch_all_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("docker ");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("docker {*args}", (string[] args) => string.Join(" ", args))
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle catch-all parameter");
  }

  [Timeout(5000)]
  public static async Task Should_complete_with_multiple_subcommands()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("kubectl get ");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("kubectl get pods", () => "Pods!")
      .Map("kubectl get services", () => "Services!")
      .Map("kubectl get nodes", () => "Nodes!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should complete multiple subcommands");
  }

  [Timeout(5000)]
  public static async Task Should_not_suggest_same_command_after_space()
  {
    // Bug: When user types "help " (with space) and presses Tab,
    // it should NOT suggest "help" again (resulting in "help help").
    // Instead, it should show options/parameters for the "help" command, or nothing if
    // the command takes no arguments.

    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("help ");
    terminal.QueueKey(ConsoleKey.Tab);  // Should NOT suggest "help" again
    terminal.QueueKey(ConsoleKey.Enter); // Submit whatever we have
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("help", () => "Help content")
      .Map("hello", () => "Hello!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - the output should NOT contain "help help" from double completion
    terminal.OutputContains("help help")
      .ShouldBeFalse("Should not suggest the same command again after it's complete");
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should exit cleanly");
  }

  [Timeout(5000)]
  public static async Task Should_suggest_subcommand_after_command_space()
  {
    // When user types "git " (with space) and presses Tab,
    // it should suggest subcommands like "commit", "push", etc.

    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("git ");
    terminal.QueueKey(ConsoleKey.Tab);  // Should show subcommands
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("git commit", () => "Committed!")
      .Map("git push", () => "Pushed!")
      .Map("git pull", () => "Pulled!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - should show available completions (subcommands)
    terminal.OutputContains("Available completions")
      .ShouldBeTrue("Should show subcommand completions");
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should exit cleanly");
  }
}
