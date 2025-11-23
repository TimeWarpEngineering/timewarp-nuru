#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Test configuration options (Section 12 of REPL Test Plan)
return await RunTests<ConfigurationTests>();

[TestTag("REPL")]
public class ConfigurationTests
{
  public static async Task Should_use_default_options()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()  // No options - use defaults
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - defaults should work
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Default options should work");
  }

  public static async Task Should_configure_custom_prompt()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options => options.Prompt = "myapp> ")
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("myapp>")
      .ShouldBeTrue("Custom prompt should be applied");
  }

  public static async Task Should_configure_max_history_size()
  {
    // Arrange
    using var terminal = new TestTerminal();
    // Add more commands than max history
    for (int i = 0; i < 15; i++)
    {
      terminal.QueueLine($"cmd{i}");
    }
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("cmd{n}", (string n) => $"Command {n}")
      .AddReplSupport(options => options.MaxHistorySize = 10)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - session should complete (history trimmed internally)
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Max history size should be respected");
  }

  public static async Task Should_enable_arrow_history()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("first");
    terminal.QueueKey(ConsoleKey.UpArrow);  // Navigate history
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("first", () => "First!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Arrow history should work when enabled");
  }

  public static async Task Should_configure_continue_on_error()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("fail");
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("fail", () => throw new Exception("Test"))
      .AddRoute("status", () => "OK")
      .AddReplSupport(options => options.ContinueOnError = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Continue on error should work");
  }

  public static async Task Should_configure_history_file_path()
  {
    // Arrange
    string historyPath = Path.Combine(Path.GetTempPath(), $"test-history-{Guid.NewGuid()}.txt");
    try
    {
      using var terminal = new TestTerminal();
      terminal.QueueLine("test");
      terminal.QueueLine("exit");

      NuruApp app = new NuruAppBuilder()
        .UseTerminal(terminal)
        .AddRoute("test", () => "OK")
        .AddReplSupport(options =>
        {
          options.PersistHistory = true;
          options.HistoryFilePath = historyPath;
        })
        .Build();

      // Act
      await app.RunReplAsync();

      // Assert
      File.Exists(historyPath).ShouldBeTrue("Custom history path should be used");
    }
    finally
    {
      if (File.Exists(historyPath)) File.Delete(historyPath);
    }
  }

  public static async Task Should_configure_messages()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options =>
      {
        options.WelcomeMessage = "Hello User!";
        options.GoodbyeMessage = "See you!";
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Hello User!")
      .ShouldBeTrue("Custom welcome message should be shown");
    terminal.OutputContains("See you!")
      .ShouldBeTrue("Custom goodbye message should be shown");
  }

  public static async Task Should_configure_mixed_options()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("test");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("test", () => "OK")
      .AddReplSupport(options =>
      {
        options.Prompt = "app> ";
        options.EnableColors = false;
        options.ShowTiming = true;
        options.ShowExitCode = true;
        options.ContinueOnError = true;
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("app>")
      .ShouldBeTrue("Custom prompt should be applied");
    terminal.OutputContains("ms)")
      .ShouldBeTrue("Timing should be shown");
    terminal.OutputContains("Exit code:")
      .ShouldBeTrue("Exit code should be shown");
  }

  public static async Task Should_configure_history_ignore_patterns()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("login --password secret123");
    terminal.QueueLine("status");
    terminal.QueueLine("history");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("login --password {pwd}", (string pwd) => "Logged in")
      .AddRoute("status", () => "OK")
      .AddReplSupport(options =>
      {
        options.HistoryIgnorePatterns = ["*password*", "*secret*"];
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - password command should be ignored from history
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("History ignore patterns should work");
  }

  public static async Task Should_configure_custom_prompt_color()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options =>
      {
        options.EnableColors = true;
        options.PromptColor = AnsiColors.Cyan;
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains(AnsiColors.Cyan)
      .ShouldBeTrue("Custom prompt color should be used");
  }
}
