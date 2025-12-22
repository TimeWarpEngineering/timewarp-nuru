#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Test prompt display when EnableArrowHistory is false (Bug Fix Verification)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.PromptDisplay
{

[TestTag("REPL")]
public class PromptDisplayTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PromptDisplayTests>();

  public static async Task Should_display_prompt_when_arrow_history_disabled()
  {
    // Arrange
    using TestTerminal terminal = new("hello\nexit\n");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Hello World!")
        .AsQuery()
        .Done()
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = false;  // Disable arrow history
        options.Prompt = "test> ";
        options.EnableColors = false;
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("test> ")
      .ShouldBeTrue("Prompt should be displayed when EnableArrowHistory = false");
  }

  public static async Task Should_display_colored_prompt_when_colors_enabled()
  {
    // Arrange
    using TestTerminal terminal = new("hello\nexit\n");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Hello World!")
        .AsQuery()
        .Done()
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = false;
        options.Prompt = "app> ";
        options.EnableColors = true;
        options.PromptColor = AnsiColors.Green;
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("app> ")
      .ShouldBeTrue("Prompt should be displayed with colors when EnableArrowHistory = false");

    terminal.OutputContains(AnsiColors.Green)
      .ShouldBeTrue("Prompt should include color codes when EnableColors = true");
  }

  public static async Task Should_display_prompt_without_colors_when_colors_disabled()
  {
    // Arrange
    using TestTerminal terminal = new("hello\nexit\n");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Hello World!")
        .AsQuery()
        .Done()
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = false;
        options.Prompt = "plain> ";
        options.EnableColors = false;
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("plain> ")
      .ShouldBeTrue("Prompt should be displayed without colors");

    terminal.OutputContains(AnsiColors.Green)
      .ShouldBeFalse("Prompt should not include color codes when EnableColors = false");
  }

  public static async Task Should_display_custom_prompt()
  {
    // Arrange
    using TestTerminal terminal = new("status\nexit\n");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status")
        .WithHandler(() => "Running")
        .AsQuery()
        .Done()
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = false;
        options.Prompt = "my-app $ ";
        options.EnableColors = false;
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("my-app $ ")
      .ShouldBeTrue("Custom prompt should be displayed");
  }

  public static async Task Should_display_prompt_for_each_command()
  {
    // Arrange
    using TestTerminal terminal = new("hello\nworld\nexit\n");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello")
        .WithHandler(() => "Hello!")
        .AsQuery()
        .Done()
      .Map("world")
        .WithHandler(() => "World!")
        .AsQuery()
        .Done()
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = false;
        options.Prompt = "> ";
        options.EnableColors = false;
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Should appear at least 3 times (hello, world, exit)
    int promptCount = terminal.Output.Split("> ").Length - 1;
    (promptCount >= 3)
      .ShouldBeTrue($"Prompt should appear multiple times, but appeared {promptCount} times");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.PromptDisplay
