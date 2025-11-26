#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Test prompt display when EnableArrowHistory is false (Bug Fix Verification)
return await RunTests<PromptDisplayTests>();

[TestTag("REPL")]
public class PromptDisplayTests
{
  public static async Task Should_display_prompt_when_arrow_history_disabled()
  {
    // Arrange
    using var terminal = new TestTerminal("hello\nexit\n");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("hello", () => "Hello World!")
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
    using var terminal = new TestTerminal("hello\nexit\n");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("hello", () => "Hello World!")
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
    using var terminal = new TestTerminal("hello\nexit\n");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("hello", () => "Hello World!")
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
    using var terminal = new TestTerminal("status\nexit\n");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("status", () => "Running")
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
    using var terminal = new TestTerminal("hello\nworld\nexit\n");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("hello", () => "Hello!")
      .AddRoute("world", () => "World!")
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
