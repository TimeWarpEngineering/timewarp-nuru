#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Bug #300 - Generator does not handle nullable type constraints
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly handles nullable type constraints
// like {seconds:int?}, {value:double?}, etc.
//
// HOW IT WORKS:
// 1. Source file has routes with nullable type constraints
// 2. Handler lambdas use nullable types (int?, double?)
// 3. If it compiles and runs, the generator parsed the type correctly
//
// WHAT THIS TESTS:
// - {seconds:int?} generates int? type declaration
// - {value:double?} generates double? type declaration
// - Parsing still works correctly for the base type
//
// NOTE: This test only verifies the case where the optional value IS provided.
// Bug #302 addresses the case where optional positional param is omitted.
//
// IMPORTANT: This test must be run in isolation (not via JARIBU_MULTI) because
// it reads the generated file from a path based on the runfile name.
// To run: ./tests/timewarp-nuru-core-tests/generator/generator-07-nullable-type-conversion.cs
// ═══════════════════════════════════════════════════════════════════════════════

#if JARIBU_MULTI
#error This test must be run in isolation. Run: ./tests/timewarp-nuru-core-tests/generator/generator-07-nullable-type-conversion.cs
#endif

using static System.Console;
using TimeWarp.Nuru;

// Top-level NuruApp - triggers generator
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("sleep {seconds:int?}")
    .WithHandler((int? seconds) =>
    {
      int sleepTime = seconds ?? 1;
      WriteLine($"Sleeping for {sleepTime} seconds");
    })
    .Done()
  .Map("scale {value:double?}")
    .WithHandler((double? value) =>
    {
      double result = (value ?? 1.0) * 2;
      WriteLine($"Scaled: {result}");
    })
    .Done()
  .Map("")
    .WithHandler(() => "Nullable type conversion test works!")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);

namespace TimeWarp.Nuru.Tests.Generator.NullableTypeConversion
{
  /// <summary>
  /// Tests that verify the generator handles nullable type constraints.
  /// Bug #300: Generator does not handle nullable type constraints.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("Bug")]
  [TestTag("Regression")]
  public class NullableTypeConversionTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<NullableTypeConversionTests>();

    /// <summary>
    /// Verify generated file contains nullable int type declaration.
    /// </summary>
    public static async Task Should_generate_nullable_int_type()
    {
      string content = ReadGeneratedFile();

      // Should have nullable int declaration
      content.ShouldContain("int? seconds = int.Parse(");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify generated file contains nullable double type declaration.
    /// </summary>
    public static async Task Should_generate_nullable_double_type()
    {
      string content = ReadGeneratedFile();

      // Should have nullable double declaration
      content.ShouldContain("double? value = double.Parse(");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify NO TODO comments for nullable types.
    /// </summary>
    public static async Task Should_not_have_todo_for_nullable_types()
    {
      string content = ReadGeneratedFile();

      // Should NOT have TODO comment for nullable types
      content.ShouldNotContain("TODO: Type conversion for");

      await Task.CompletedTask;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════════

    private static string ReadGeneratedFile()
    {
      string repoRoot = FindRepoRoot();
      string generatedFile = Path.Combine(
        repoRoot,
        "artifacts",
        "generated",
        "generator-07-nullable-type-conversion",
        "TimeWarp.Nuru.Analyzers",
        "TimeWarp.Nuru.Generators.NuruGenerator",
        "NuruGenerated.g.cs");

      if (!File.Exists(generatedFile))
      {
        throw new FileNotFoundException(
          $"Generated file not found at: {generatedFile}\n" +
          "This may indicate the generator did not run or Bug #300 is not fixed.");
      }

      return File.ReadAllText(generatedFile);
    }

    private static string FindRepoRoot()
    {
      string? dir = Environment.CurrentDirectory;

      while (dir is not null)
      {
        if (File.Exists(Path.Combine(dir, "timewarp-nuru.slnx")))
          return dir;

        dir = Path.GetDirectoryName(dir);
      }

      throw new InvalidOperationException(
        $"Could not find repository root from {Environment.CurrentDirectory}");
    }
  }
}
