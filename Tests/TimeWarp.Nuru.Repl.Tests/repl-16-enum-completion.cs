#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Test enum parameter completion in REPL (Bug fix #041)
// Tests that enum values are properly shown in tab completions
return await RunTests<EnumCompletionTests>();

public enum Environment
{
  Dev,
  Staging,
  Prod
}

[TestTag("REPL")]
public class EnumCompletionTests
{
  [Timeout(5000)]
  public static async Task Should_show_enum_values_on_tab_after_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("deploy ");  // Type command + space
    terminal.QueueKey(ConsoleKey.Tab);  // Show all enum completions
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("");  // Submit empty (will show help)
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddTypeConverter(new EnumTypeConverter<Environment>())
      .Map("deploy {env:environment}", (Environment _) => 0)
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - verify enum values are shown in completions
    terminal.OutputContains("Dev")
      .ShouldBeTrue("Should show 'Dev' enum value in completions");
    terminal.OutputContains("Prod")
      .ShouldBeTrue("Should show 'Prod' enum value in completions");
    terminal.OutputContains("Staging")
      .ShouldBeTrue("Should show 'Staging' enum value in completions");
  }

  [Timeout(5000)]
  public static async Task Should_show_completions_including_help_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("deploy ");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddTypeConverter(new EnumTypeConverter<Environment>())
      .Map("deploy {env:environment}", (Environment _) => 0)
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - should also show --help option
    terminal.OutputContains("--help")
      .ShouldBeTrue("Should show '--help' option in completions");
  }

  [Timeout(5000)]
  public static async Task Should_show_completions_header()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("deploy ");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddTypeConverter(new EnumTypeConverter<Environment>())
      .Map("deploy {env:environment}", (Environment _) => 0)
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Available completions")
      .ShouldBeTrue("Should show 'Available completions' header");
  }

  [Timeout(5000)]
  public static async Task Should_show_only_matching_enum_with_partial_input()
  {
    // Arrange - Type "deploy S" and Tab - should show only "Staging"
    using TestTerminal terminal = new();
    terminal.QueueKeys("deploy S");  // Only "Staging" starts with S
    terminal.QueueKey(ConsoleKey.Tab);  // Should auto-complete (single match) or show just Staging
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddTypeConverter(new EnumTypeConverter<Environment>())
      .Map("deploy {env:environment}", (Environment _) => 0)
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - "Staging" should appear in output (either as completion applied or shown)
    terminal.OutputContains("Staging")
      .ShouldBeTrue("Should show or complete to 'Staging' with partial 'S'");
  }
}
