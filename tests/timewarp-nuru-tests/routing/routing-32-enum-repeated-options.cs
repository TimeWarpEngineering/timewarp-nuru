#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

/// <summary>
/// Tests for enum element types in repeated options (--env Dev --env Staging).
/// Covers MyEnum[], IEnumerable&lt;MyEnum&gt;, nullable arrays, aliases, and invalid-value UX.
/// </summary>
[TestTag("Routing")]
[TestTag("Enum")]
public class EnumRepeatedOptionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EnumRepeatedOptionTests>();

  public enum Environment
  {
    Dev,
    Staging,
    Prod
  }

  // ============================================================================
  // MyEnum[] BASIC
  // ============================================================================

  public static async Task Should_bind_enum_array_repeated_option_multiple_values()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e}*")
        .WithHandler((Environment[] e) => $"e:[{string.Join(",", e)}]|len:{e.Length}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "Dev", "--env", "Staging", "--env", "Prod"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[Dev,Staging,Prod]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();
  }

  public static async Task Should_bind_enum_array_repeated_option_single_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e}*")
        .WithHandler((Environment[] e) => $"e:[{string.Join(",", e)}]|len:{e.Length}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "Staging"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[Staging]").ShouldBeTrue();
    terminal.OutputContains("len:1").ShouldBeTrue();
  }

  public static async Task Should_bind_enum_array_repeated_option_empty()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e}*")
        .WithHandler((Environment[] e) => $"e:[{string.Join(",", e)}]|len:{e.Length}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[]").ShouldBeTrue();
    terminal.OutputContains("len:0").ShouldBeTrue();
  }

  public static async Task Should_bind_enum_array_case_insensitive()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e}*")
        .WithHandler((Environment[] e) => $"e:[{string.Join(",", e)}]")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod", "--env", "DEV"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[Prod,Dev]").ShouldBeTrue();
  }

  // ============================================================================
  // INVALID VALUE UX
  // ============================================================================

  public static async Task Should_show_error_and_valid_values_for_invalid_enum_in_array()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e}*")
        .WithHandler((Environment[] e) => $"e:[{string.Join(",", e)}]")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "invalid"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Error").ShouldBeTrue();
    terminal.OutputContains("invalid").ShouldBeTrue();
    terminal.OutputContains("Dev").ShouldBeTrue("Should show valid values");
    terminal.OutputContains("Staging").ShouldBeTrue("Should show valid values");
    terminal.OutputContains("Prod").ShouldBeTrue("Should show valid values");
  }

  public static async Task Should_show_error_when_invalid_value_among_valid_enums()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e}*")
        .WithHandler((Environment[] e) => $"e:[{string.Join(",", e)}]")
        .AsCommand()
        .Done()
      .Build();

    // Act - second value is invalid
    int exitCode = await app.RunAsync(["deploy", "--env", "Dev", "--env", "nope", "--env", "Prod"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Error").ShouldBeTrue();
    terminal.OutputContains("nope").ShouldBeTrue();
    terminal.OutputContains("Dev").ShouldBeTrue("Should show valid values");
    terminal.OutputContains("Staging").ShouldBeTrue("Should show valid values");
    terminal.OutputContains("Prod").ShouldBeTrue("Should show valid values");
  }

  // ============================================================================
  // SHORT ALIAS
  // ============================================================================

  public static async Task Should_bind_enum_array_with_short_alias()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env,-e {e}*")
        .WithHandler((Environment[] e) => $"e:[{string.Join(",", e)}]|len:{e.Length}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "Dev", "-e", "Staging", "--env", "Prod"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[Dev,Staging,Prod]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();
  }

  // ============================================================================
  // IEnumerable<MyEnum>
  // ============================================================================

  public static async Task Should_bind_ienumerable_enum_repeated_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e}*")
        .WithHandler((IEnumerable<Environment> e) => $"e:[{string.Join(",", e)}]|len:{e.Count()}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "Dev", "--env", "Prod"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[Dev,Prod]").ShouldBeTrue();
    terminal.OutputContains("len:2").ShouldBeTrue();
  }

  // ============================================================================
  // MyEnum[]?
  // ============================================================================

  public static async Task Should_bind_nullable_enum_array_repeated_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e}*")
        .WithHandler((Environment[]? e) => $"e:[{string.Join(",", e ?? [])}]|null:{e is null}|len:{(e?.Length ?? -1)}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "Staging"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[Staging]").ShouldBeTrue();
    terminal.OutputContains("null:False").ShouldBeTrue();
    terminal.OutputContains("len:1").ShouldBeTrue();
  }

  public static async Task Should_bind_nullable_enum_array_when_empty()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e}*")
        .WithHandler((Environment[]? e) => $"e:[{string.Join(",", e ?? [])}]|null:{e is null}|len:{(e?.Length ?? -1)}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy"]);

    // Assert - repeated options always materialize as an array (empty when omitted)
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[]").ShouldBeTrue();
    terminal.OutputContains("null:False").ShouldBeTrue();
    terminal.OutputContains("len:0").ShouldBeTrue();
  }

  // ============================================================================
  // MIXED OPTIONS
  // ============================================================================

  public static async Task Should_bind_enum_array_with_mixed_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e}* --tag {t} --verbose")
        .WithHandler((Environment[] e, string t, bool verbose) =>
          $"e:[{string.Join(",", e)}]|t:{t}|verbose:{verbose}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(
    [
      "deploy",
      "--env", "Dev",
      "--tag", "v1",
      "--env", "Staging",
      "--verbose"
    ]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[Dev,Staging]").ShouldBeTrue();
    terminal.OutputContains("t:v1").ShouldBeTrue();
    terminal.OutputContains("verbose:True").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
