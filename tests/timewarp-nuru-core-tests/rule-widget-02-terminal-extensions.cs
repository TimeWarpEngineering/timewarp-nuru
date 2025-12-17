#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test Rule widget terminal extension methods

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.RuleWidgetTerminal
{

[TestTag("Widgets")]
public class RuleTerminalExtensionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RuleTerminalExtensionTests>();

  public static async Task Should_write_simple_rule_to_terminal()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WriteRule();

    // Assert
    terminal.Output.ShouldContain(new string('─', 40));

    await Task.CompletedTask;
  }

  public static async Task Should_write_rule_with_title()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WriteRule("Section");

    // Assert
    terminal.Output.ShouldContain("Section");
    terminal.Output.ShouldContain("─");

    await Task.CompletedTask;
  }

  public static async Task Should_write_rule_with_styled_title()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WriteRule("Results".Cyan().Bold());

    // Assert
    terminal.Output.ShouldContain("Results");
    terminal.Output.ShouldContain(AnsiColors.Cyan);

    await Task.CompletedTask;
  }

  public static async Task Should_write_rule_with_style()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WriteRule(LineStyle.Doubled);

    // Assert
    terminal.Output.ShouldContain(new string('═', 40));

    await Task.CompletedTask;
  }

  public static async Task Should_write_rule_with_fluent_builder()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WriteRule(rule => rule
        .Title("Configuration")
        .Style(LineStyle.Doubled)
        .Color(AnsiColors.Cyan));

    // Assert
    terminal.Output.ShouldContain("Configuration");
    terminal.Output.ShouldContain("═");
    terminal.Output.ShouldContain(AnsiColors.Cyan);

    await Task.CompletedTask;
  }

  public static async Task Should_write_preconfigured_rule()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };
    Rule rule = new()
    {
      Title = "Test",
      Style = LineStyle.Heavy,
      Color = AnsiColors.Yellow
    };

    // Act
    terminal.WriteRule(rule);

    // Assert
    terminal.Output.ShouldContain("Test");
    terminal.Output.ShouldContain("━");
    terminal.Output.ShouldContain(AnsiColors.Yellow);

    await Task.CompletedTask;
  }

  public static async Task Should_use_terminal_width()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 60 };

    // Act
    terminal.WriteRule();

    // Assert
    string output = terminal.Output.TrimEnd();
    output.ShouldBe(new string('─', 60));

    await Task.CompletedTask;
  }

  public static async Task Should_handle_narrow_terminal()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 20 };

    // Act
    terminal.WriteRule("Short");

    // Assert
    terminal.Output.ShouldContain("Short");
    terminal.Output.ShouldContain("─");

    await Task.CompletedTask;
  }

  public static async Task Should_include_newline_after_rule()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WriteRule();

    // Assert
    terminal.Output.ShouldEndWith(Environment.NewLine);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.RuleWidgetTerminal
