#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test configuration options (Section 12 of REPL Test Plan)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.Configuration
{
  [TestTag("REPL")]
  public class ConfigurationTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ConfigurationTests>();

  public static string ThrowTestException() => throw new InvalidOperationException("Test");

  [Timeout(5000)]
  public static async Task Should_use_default_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()  // No options - use defaults
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - defaults should work
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Default options should work");
  }

  [Timeout(5000)]
  public static async Task Should_configure_custom_prompt()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options => options.Prompt = "myapp> ")
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("myapp>")
      .ShouldBeTrue("Custom prompt should be applied");
  }

  [Timeout(5000)]
  public static async Task Should_configure_max_history_size()
  {
    // Arrange
    using TestTerminal terminal = new();
    // Add more commands than max history
    for (int i = 0; i < 15; i++)
    {
      terminal.QueueLine($"cmd{i}");
    }

    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("cmd{n}")
        .WithHandler((string n) => $"Command {n}")
        .AsCommand()
        .Done()
      .AddRepl(options => options.MaxHistorySize = 10)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - session should complete (history trimmed internally)
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Max history size should be respected");
  }

  [Timeout(5000)]
  public static async Task Should_enable_arrow_history()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("first");
    terminal.QueueKey(ConsoleKey.UpArrow);  // Navigate history
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("first").WithHandler(() => "First!").AsCommand().Done()
      .AddRepl(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Arrow history should work when enabled");
  }

  [Timeout(5000)]
  public static async Task Should_configure_continue_on_error()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("fail");
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("fail").WithHandler(ThrowTestException).AsCommand().Done()
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddRepl(options => options.ContinueOnError = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Continue on error should work");
  }

  [Timeout(5000)]
  public static async Task Should_configure_history_file_path()
  {
    // Arrange
    string historyPath = Path.Combine(Path.GetTempPath(), $"test-history-{Guid.NewGuid()}.txt");
    try
    {
      using TestTerminal terminal = new();
      terminal.QueueLine("test");
      terminal.QueueLine("exit");

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("test").WithHandler(() => "OK").AsCommand().Done()
        .AddRepl(options =>
        {
          options.PersistHistory = true;
          options.HistoryFilePath = historyPath;
        })
        .Build();

      // Act
      await app.RunAsync(["--interactive"]);

      // Assert
      File.Exists(historyPath).ShouldBeTrue("Custom history path should be used");
    }
    finally
    {
      if (File.Exists(historyPath)) File.Delete(historyPath);
    }
  }

  [Timeout(5000)]
  public static async Task Should_configure_messages()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.WelcomeMessage = "Hello User!";
        options.GoodbyeMessage = "See you!";
      })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Hello User!")
      .ShouldBeTrue("Custom welcome message should be shown");
    terminal.OutputContains("See you!")
      .ShouldBeTrue("Custom goodbye message should be shown");
  }

  [Timeout(5000)]
  public static async Task Should_configure_mixed_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("test");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test").WithHandler(() => "OK").AsCommand().Done()
      .AddRepl(options =>
      {
        options.Prompt = "app> ";
        options.EnableColors = false;
        options.ShowTiming = true;
        options.ShowExitCode = true;
        options.ContinueOnError = true;
      })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("app>")
      .ShouldBeTrue("Custom prompt should be applied");
    terminal.OutputContains("ms)")
      .ShouldBeTrue("Timing should be shown");
    terminal.OutputContains("Exit code:")
      .ShouldBeTrue("Exit code should be shown");
  }

  // Note: HistoryIgnorePatterns is tested in repl-03b-history-security.cs
  // It's an init-only property so can't be set via Action<ReplOptions> lambda

  [Timeout(5000)]
  public static async Task Should_configure_custom_prompt_color()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.EnableColors = true;
        options.PromptColor = AnsiColors.Cyan;
      })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains(AnsiColors.Cyan)
      .ShouldBeTrue("Custom prompt color should be used");
  }
  }
}
