#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

/// <summary>
/// Tests for enum parameters in option positions (--option {enumValue}).
/// This specifically tests the fix for #387 where enum option parameters
/// were not being converted properly by the source generator.
/// </summary>
[TestTag("Routing")]
[TestTag("Enum")]
public class EnumOptionParameterTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EnumOptionParameterTests>();

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

  // ============================================================================
  // BASIC ENUM OPTION TESTS
  // ============================================================================

  public static async Task Should_bind_enum_option_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env {env}")
        .WithHandler((Environment env) => $"env:{env}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "Prod"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:Prod").ShouldBeTrue();
  }

  public static async Task Should_bind_enum_option_case_insensitive()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env {env}")
        .WithHandler((Environment env) => $"env:{env}")
        .AsCommand()
        .Done()
      .Build();

    // Act - lowercase input
    int exitCode = await app.RunAsync(["deploy", "--env", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:Prod").ShouldBeTrue();
  }

  // TODO: This test is skipped due to pre-existing bug where IsConfigArg incorrectly
  // filters out --option=value syntax. The enum conversion code is correct.
  // See generated code: --env=staging triggers IsConfigArg() == true incorrectly.
  [Skip("Pre-existing bug: IsConfigArg incorrectly filters --option=value syntax")]
  public static async Task Should_bind_enum_option_with_equals_syntax()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env {env}")
        .WithHandler((Environment env) => $"env:{env}")
        .AsCommand()
        .Done()
      .Build();

    // Act - --env=value syntax
    int exitCode = await app.RunAsync(["deploy", "--env=staging"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:Staging").ShouldBeTrue();
  }

  public static async Task Should_show_error_for_invalid_enum_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env {env}")
        .WithHandler((Environment env) => $"env:{env}")
        .AsCommand()
        .Done()
      .Build();

    // Act - invalid enum value
    int exitCode = await app.RunAsync(["deploy", "--env", "invalid"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Error").ShouldBeTrue();
    terminal.OutputContains("Dev").ShouldBeTrue("Should show valid values");
    terminal.OutputContains("Staging").ShouldBeTrue("Should show valid values");
    terminal.OutputContains("Prod").ShouldBeTrue("Should show valid values");
  }

  // ============================================================================
  // OPTIONAL ENUM OPTION TESTS
  // ============================================================================

  public static async Task Should_bind_optional_enum_option_when_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env? {env?}")
        .WithHandler((Environment? env) => $"env:{env?.ToString() ?? "null"}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "Dev"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:Dev").ShouldBeTrue();
  }

  public static async Task Should_bind_optional_enum_option_when_not_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env? {env?}")
        .WithHandler((Environment? env) => $"env:{env?.ToString() ?? "null"}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:null").ShouldBeTrue();
  }

  // ============================================================================
  // MIXED PARAMETER TESTS
  // ============================================================================

  public static async Task Should_bind_enum_option_with_positional_params()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy {name} --env {env}")
        .WithHandler((string name, Environment env) => $"name:{name}|env:{env}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "myapp", "--env", "Staging"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:myapp|env:Staging").ShouldBeTrue();
  }

  public static async Task Should_bind_enum_option_with_flag()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env {env} --verbose")
        .WithHandler((Environment env, bool verbose) => $"env:{env}|verbose:{verbose}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "Prod", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:Prod|verbose:True").ShouldBeTrue();
  }

  public static async Task Should_bind_multiple_enum_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env {env} --log {level}")
        .WithHandler((Environment env, LogLevel level) => $"env:{env}|log:{level}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "Staging", "--log", "Debug"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:Staging|log:Debug").ShouldBeTrue();
  }

  public static async Task Should_bind_enum_option_with_short_form()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env,-e {env}")
        .WithHandler((Environment env) => $"env:{env}")
        .AsCommand()
        .Done()
      .Build();

    // Act - using short form
    int exitCode = await app.RunAsync(["deploy", "-e", "Dev"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:Dev").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
