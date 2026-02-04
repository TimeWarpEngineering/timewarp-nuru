#!/usr/bin/dotnet --
#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CA1725 // Parameter names should match base declaration
#pragma warning disable CA1849 // Call async methods when in async method
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable RCS1248 // Use pattern matching to check for null

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Partial Class Options (#406)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly handles [Option] attributes
// on properties defined in partial class files.
//
// REGRESSION TEST FOR:
// - Bug #406: Options in partial classes not recognized by source generator
//
// HOW IT WORKS:
// 1. Define a partial class with [NuruRoute] in one file
// 2. Define [Option] properties in a separate partial class file
// 3. Source generator must discover ALL properties including those from partial files
// 4. If options work correctly, the generated code properly handles partial class properties
//
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.PartialClassOptions
{
  /// <summary>
  /// Tests for partial class option handling (bug #406).
  /// These tests verify that the source generator correctly discovers
  /// [Option] attributes on properties defined in partial class files.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("PartialClass")]
  [TestTag("Bug406")]
  public class PartialClassOptionsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<PartialClassOptionsTests>();

    /// <summary>
    /// Regression test for bug #406: Options defined in partial class files
    /// must be recognized by the source generator.
    /// </summary>
    public static async Task Should_recognize_options_in_partial_class()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints()
        .Build();

      // Act - Call the partial-command with options defined in partial file
      int exitCode = await app.RunAsync(["partial-test", "--verbose", "--count", "5"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Verbose: True").ShouldBeTrue();
      terminal.OutputContains("Count: 5").ShouldBeTrue();
    }

    /// <summary>
    /// Test that partial class options work with default values.
    /// </summary>
    public static async Task Should_handle_partial_class_options_with_defaults()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints()
        .Build();

      // Act - Call without specifying options to test defaults
      int exitCode = await app.RunAsync(["partial-test"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Verbose: False").ShouldBeTrue();
      terminal.OutputContains("Count: 1").ShouldBeTrue();
    }

    /// <summary>
    /// Test that partial class options work with short forms.
    /// </summary>
    public static async Task Should_handle_partial_class_options_short_forms()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints()
        .Build();

      // Act - Use short forms for options
      int exitCode = await app.RunAsync(["partial-test", "-v", "-c", "10"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Verbose: True").ShouldBeTrue();
      terminal.OutputContains("Count: 10").ShouldBeTrue();
    }

    /// <summary>
    /// Test that options can be mixed from main and partial class files.
    /// </summary>
    public static async Task Should_handle_mixed_options_from_both_files()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints()
        .Build();

      // Act - Use options from both main and partial class files
      int exitCode = await app.RunAsync(["partial-test", "--name", "Test", "-v", "-c", "3"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Name: Test").ShouldBeTrue();
      terminal.OutputContains("Verbose: True").ShouldBeTrue();
      terminal.OutputContains("Count: 3").ShouldBeTrue();
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// PARTIAL CLASS DEFINITION: Main file part
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Partial command class to test options in partial files (bug #406).
/// This file contains the main class definition with [NuruRoute].
/// </summary>
[NuruRoute("partial-test", Description = "Test partial class options")]
public sealed partial class PartialTestCommand : ICommand<Unit>
{
  // This property is defined in the main file
  [Option("name", "n", Description = "Name to display")]
  public string? Name { get; set; }

  public sealed class Handler(ITerminal terminal) : ICommandHandler<PartialTestCommand, Unit>
  {
    public ValueTask<Unit> Handle(PartialTestCommand command, CancellationToken ct)
    {
      string displayName = command.Name ?? "Default";
      terminal.WriteLine($"Name: {displayName}");
      terminal.WriteLine($"Verbose: {command.Verbose}");
      terminal.WriteLine($"Count: {command.Count}");
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// PARTIAL CLASS DEFINITION: Secondary file part
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Partial class extension with options defined in a separate file.
/// This tests that the source generator correctly discovers properties
/// from all partial class files, not just the one with [NuruRoute].
/// </summary>
public sealed partial class PartialTestCommand
{
  // Options defined in the partial file - this was not being recognized before bug #406 fix
  [Option("verbose", "v", Description = "Enable verbose output")]
  public bool Verbose { get; set; }

  [Option("count", "c", Description = "Number of items")]
  public int Count { get; set; } = 1;
}
