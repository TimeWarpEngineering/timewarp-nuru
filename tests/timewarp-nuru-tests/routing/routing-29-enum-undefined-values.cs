#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

/// <summary>
/// Tests that undefined enum values are rejected by the runtime type converter.
/// Covers non-[Flags] enums (numeric gaps, undefined numerics) and [Flags] enums
/// (raw numeric input and numeric comma-separated combinations).
/// </summary>
[TestTag("Routing")]
[TestTag("Enum")]
public class EnumUndefinedValueTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EnumUndefinedValueTests>();

  public enum Priority
  {
    Low = 0,
    Normal = 10,
    High = 20,
    Critical = 30
  }

  [Flags]
  public enum FilePermissions
  {
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    ReadWrite = 3
  }

  // ============================================================================
  // NON-FLAGS ENUM TESTS
  // ============================================================================

  public static async Task Should_reject_undefined_numeric_enum_value_positional()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set-priority {priority:Priority}")
        .WithHandler((Priority priority) => $"priority:{priority}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set-priority", "999"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Error").ShouldBeTrue();
  }

  public static async Task Should_reject_undefined_numeric_enum_value_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set-priority --priority {priority:Priority}")
        .WithHandler((Priority priority) => $"priority:{priority}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set-priority", "--priority", "999"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Error").ShouldBeTrue();
  }

  public static async Task Should_reject_undefined_numeric_between_defined_values()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set-priority {priority:Priority}")
        .WithHandler((Priority priority) => $"priority:{priority}")
        .AsCommand()
        .Done()
      .Build();

    // Act - 25 is a valid integer but does not map to a defined member
    int exitCode = await app.RunAsync(["set-priority", "25"]);

    // Assert
    exitCode.ShouldBe(1);
  }

  public static async Task Should_accept_numeric_string_mapping_to_defined_member()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set-priority {priority:Priority}")
        .WithHandler((Priority priority) => $"priority:{priority}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set-priority", "10"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("priority:Normal").ShouldBeTrue();
  }

  public static async Task Should_accept_valid_enum_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set-priority {priority:Priority}")
        .WithHandler((Priority priority) => $"priority:{priority}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set-priority", "High"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("priority:High").ShouldBeTrue();
  }

  public static async Task Should_accept_valid_enum_name_case_insensitive()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set-priority {priority:Priority}")
        .WithHandler((Priority priority) => $"priority:{priority}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set-priority", "HIGH"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("priority:High").ShouldBeTrue();
  }

  public static async Task Should_reject_non_parseable_enum_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set-priority {priority:Priority}")
        .WithHandler((Priority priority) => $"priority:{priority}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set-priority", "invalid"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Error").ShouldBeTrue();
  }

  public static async Task Should_show_valid_values_in_error_message()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set-priority {priority:Priority}")
        .WithHandler((Priority priority) => $"priority:{priority}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set-priority", "999"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Low").ShouldBeTrue();
    terminal.OutputContains("Normal").ShouldBeTrue();
    terminal.OutputContains("High").ShouldBeTrue();
    terminal.OutputContains("Critical").ShouldBeTrue();
  }

  // ============================================================================
  // FLAGS ENUM TESTS
  // ============================================================================

  public static async Task Should_accept_flags_single_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("grant --perm {perm:FilePermissions}")
        .WithHandler((FilePermissions perm) => $"perm:{perm}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["grant", "--perm", "Read"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("perm:Read").ShouldBeTrue();
  }

  public static async Task Should_accept_flags_comma_separated_names()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("grant --perm {perm:FilePermissions}")
        .WithHandler((FilePermissions perm) => $"perm:{perm}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["grant", "--perm", "Read,Write"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("perm:ReadWrite").ShouldBeTrue();
  }

  public static async Task Should_accept_flags_combination_case_insensitive()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("grant --perm {perm:FilePermissions}")
        .WithHandler((FilePermissions perm) => $"perm:{perm}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["grant", "--perm", "read,write"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("perm:ReadWrite").ShouldBeTrue();
  }

  public static async Task Should_accept_flags_combined_member_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("grant --perm {perm:FilePermissions}")
        .WithHandler((FilePermissions perm) => $"perm:{perm}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["grant", "--perm", "ReadWrite"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("perm:ReadWrite").ShouldBeTrue();
  }

  public static async Task Should_reject_flags_raw_numeric()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("grant --perm {perm:FilePermissions}")
        .WithHandler((FilePermissions perm) => $"perm:{perm}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["grant", "--perm", "12"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Error").ShouldBeTrue();
  }

  public static async Task Should_reject_flags_comma_separated_numerics()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("grant --perm {perm:FilePermissions}")
        .WithHandler((FilePermissions perm) => $"perm:{perm}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["grant", "--perm", "1,2"]);

    // Assert
    exitCode.ShouldBe(1);
  }

  public static async Task Should_reject_flags_numeric_for_defined_combined_member()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("grant --perm {perm:FilePermissions}")
        .WithHandler((FilePermissions perm) => $"perm:{perm}")
        .AsCommand()
        .Done()
      .Build();

    // Act - 3 equals ReadWrite, but numeric input is rejected for [Flags] enums
    int exitCode = await app.RunAsync(["grant", "--perm", "3"]);

    // Assert
    exitCode.ShouldBe(1);
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
