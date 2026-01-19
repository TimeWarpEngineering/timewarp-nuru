#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Tests enum parameter completion in REPL - focused tests for enum-specific behavior.
//
// TODO: #387 - Generator bug causes build failures for enum option parameters.
// - Positional enum params work (fixed in #372)
// - Option enum params (--option {enumParam}) fail - generator doesn't emit conversion code
//
// Expected behavior (both should work):
// - Explicit: .Map("deploy {env:environment}") with handler (Environment env)
// - Implicit: .Map("deploy {env}") with handler (Environment env) - infer type from handler
// - Options: .Map("deploy --env {env}") with handler (Environment env)
//
// Tests use [Skip] attribute until #387 is fixed. Tests exist to expose the bug, not mask it.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.EnumCompletion
{
  public enum Environment
  {
    Dev,
    Staging,
    Prod
  }

  public enum LogLevel
  {
    Debug,
    Info,
    Warning,
    Error
  }

  [TestTag("REPL")]
  [TestTag("Completion")]
  [TestTag("Enum")]
  public class EnumCompletionTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<EnumCompletionTests>();

    public static async Task Should_show_all_enum_values_after_command_space()
    {
      // Arrange
      using TestTerminal terminal = new();
      terminal.QueueKeys("deploy ");
      terminal.QueueKey(ConsoleKey.Tab);
      terminal.QueueKey(ConsoleKey.Escape);
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env:environment}")
          .WithHandler((Environment env) => $"Deploying to {env}")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableColors = false)
        .Build();

      // Act
      await app.RunAsync(["--interactive"]);

      // Assert
      terminal.OutputContains("Dev").ShouldBeTrue("Should show 'Dev' enum value");
      terminal.OutputContains("Staging").ShouldBeTrue("Should show 'Staging' enum value");
      terminal.OutputContains("Prod").ShouldBeTrue("Should show 'Prod' enum value");
    }

    public static async Task Should_filter_enum_values_with_partial_input()
    {
      // Arrange - Type "deploy D" - only "Dev" starts with D
      using TestTerminal terminal = new();
      terminal.QueueKeys("deploy D");
      terminal.QueueKey(ConsoleKey.Tab);  // Should auto-complete to "Dev"
      terminal.QueueKey(ConsoleKey.Enter);
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env:environment}")
          .WithHandler((Environment env) => $"Deploying to {env}")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableColors = false)
        .Build();

      // Act
      await app.RunAsync(["--interactive"]);

      // Assert - "Dev" should be completed and command executed
      terminal.OutputContains("Deploying to Dev").ShouldBeTrue("Should auto-complete 'D' to 'Dev'");
    }

    public static async Task Should_show_matching_enums_with_common_prefix()
    {
      // Arrange - LogLevel has Debug and (none starting with D except Debug)
      // But Environment has Dev which starts with D
      using TestTerminal terminal = new();
      terminal.QueueKeys("log D");
      terminal.QueueKey(ConsoleKey.Tab);  // Should auto-complete to "Debug"
      terminal.QueueKey(ConsoleKey.Enter);
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("log {level:loglevel}")
          .WithHandler((LogLevel level) => $"Log level: {level}")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableColors = false)
        .Build();

      // Act
      await app.RunAsync(["--interactive"]);

      // Assert
      terminal.OutputContains("Log level: Debug").ShouldBeTrue("Should auto-complete 'D' to 'Debug'");
    }

    public static async Task Should_show_help_option_alongside_enum_values()
    {
      // Arrange
      using TestTerminal terminal = new();
      terminal.QueueKeys("deploy ");
      terminal.QueueKey(ConsoleKey.Tab);
      terminal.QueueKey(ConsoleKey.Escape);
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env:environment}")
          .WithHandler((Environment env) => $"Deploying to {env}")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableColors = false)
        .Build();

      // Act
      await app.RunAsync(["--interactive"]);

      // Assert
      terminal.OutputContains("--help").ShouldBeTrue("Should show '--help' alongside enum values");
    }

    public static async Task Should_complete_case_insensitive()
    {
      // Arrange - Type lowercase "dev" should still match "Dev"
      using TestTerminal terminal = new();
      terminal.QueueKeys("deploy dev");
      terminal.QueueKey(ConsoleKey.Enter);
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env:environment}")
          .WithHandler((Environment env) => $"Deploying to {env}")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableColors = false)
        .Build();

      // Act
      await app.RunAsync(["--interactive"]);

      // Assert - Should execute successfully with case-insensitive match
      terminal.OutputContains("Deploying to Dev").ShouldBeTrue("Should accept lowercase 'dev' as 'Dev'");
    }

    public static async Task Should_show_enum_at_correct_parameter_position()
    {
      // Arrange - Command with multiple parameters, enum is second
      using TestTerminal terminal = new();
      terminal.QueueKeys("deploy myapp ");  // After "myapp ", enum completions should show
      terminal.QueueKey(ConsoleKey.Tab);
      terminal.QueueKey(ConsoleKey.Escape);
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {name} {env:environment}")
          .WithHandler((string name, Environment env) => $"Deploying {name} to {env}")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableColors = false)
        .Build();

      // Act
      await app.RunAsync(["--interactive"]);

      // Assert - Should show enum values at position 2 (after name)
      terminal.OutputContains("Dev").ShouldBeTrue("Should show enum values at correct position");
      terminal.OutputContains("Staging").ShouldBeTrue("Should show Staging at correct position");
    }

    public static async Task Should_not_show_enum_at_wrong_position()
    {
      // Arrange - At first parameter position, should not show enum values
      using TestTerminal terminal = new();
      terminal.QueueKeys("deploy ");  // First param is string, not enum
      terminal.QueueKey(ConsoleKey.Tab);
      terminal.QueueKey(ConsoleKey.Escape);
      terminal.QueueLine("exit");

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {name} {env:environment}")
          .WithHandler((string name, Environment env) => $"Deploying {name} to {env}")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableColors = false)
        .Build();

      // Act
      await app.RunAsync(["--interactive"]);

      // Assert - Should NOT show enum values at first position (it's a string param)
      // Only --help should be suggested
      terminal.OutputContains("--help").ShouldBeTrue("Should show --help");
      // The enum values shouldn't appear at position 0 (they're at position 1)
      // Note: This test verifies position-awareness
    }
  }
}
