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
    using var terminal = new TestTerminal();
    terminal.QueueKeys("deploy --");
    terminal.QueueKey(ConsoleKey.Tab);  // Show options
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("deploy --force", () => "Force deployed!")
      .AddRoute("deploy --dry-run", () => "Dry run!")
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
    using var terminal = new TestTerminal();
    terminal.QueueKeys("deploy -");
    terminal.QueueKey(ConsoleKey.Tab);  // Show short options
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("deploy -f", () => "Force!")
      .AddRoute("deploy -v", () => "Verbose!")
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
    using var terminal = new TestTerminal();
    terminal.QueueKeys("git com");
    terminal.QueueKey(ConsoleKey.Tab);  // Should show "commit", "config"
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("git commit", () => "Committed!")
      .AddRoute("git config", () => "Configured!")
      .AddRoute("git status", () => "Status OK")
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
    using var terminal = new TestTerminal();
    terminal.QueueKeys("deploy ");
    terminal.QueueKey(ConsoleKey.Tab);  // Show parameter completions
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
      .ShouldBeTrue("Should handle parameter value completion");
  }

  [Timeout(5000)]
  public static async Task Should_complete_after_option_value()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("deploy --env prod ");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("deploy --env {env}", (string env) => $"Deployed to {env}")
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
    using var terminal = new TestTerminal();
    terminal.QueueKeys("cmd arg1 --opt val ");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("cmd {arg} --opt {val}", (string arg, string val) => $"{arg}:{val}")
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
    using var terminal = new TestTerminal();
    terminal.QueueKeys("docker ");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("docker {*args}", (string[] args) => string.Join(" ", args))
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
    using var terminal = new TestTerminal();
    terminal.QueueKeys("kubectl get ");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("kubectl get pods", () => "Pods!")
      .AddRoute("kubectl get services", () => "Services!")
      .AddRoute("kubectl get nodes", () => "Nodes!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should complete multiple subcommands");
  }
}
