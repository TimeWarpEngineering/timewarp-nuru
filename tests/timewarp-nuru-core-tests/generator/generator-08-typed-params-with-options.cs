#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Bug #301 - Complex routes skip type conversion
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly handles typed parameters in
// routes that have options (which use EmitComplexMatch instead of EmitSimpleMatch).
//
// HOW IT WORKS:
// 1. Routes with options use a different code path (EmitComplexMatch)
// 2. Before fix, typed params were declared as string instead of parsed
// 3. If it compiles and runs, typed params are correctly parsed
//
// WHAT THIS TESTS:
// - round {value:double} --mode {mode}  -> double value is parsed correctly
// - scale {value:int} --factor {factor} -> int value is parsed correctly
//
// IMPORTANT: This test must be run in isolation (not via JARIBU_MULTI) because
// it reads the generated file from a path based on the runfile name.
// To run: ./tests/timewarp-nuru-core-tests/generator/generator-08-typed-params-with-options.cs
// ═══════════════════════════════════════════════════════════════════════════════

#if JARIBU_MULTI
#error This test must be run in isolation. Run: ./tests/timewarp-nuru-core-tests/generator/generator-08-typed-params-with-options.cs
#endif

using static System.Console;
using TimeWarp.Nuru;

// Top-level NuruApp - triggers generator
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("round {value:double} --mode {mode}")
    .WithHandler((double value, string mode) =>
    {
      double result = mode?.ToLowerInvariant() switch
      {
        "up" => Math.Ceiling(value),
        "down" => Math.Floor(value),
        _ => Math.Round(value)
      };
      WriteLine($"Round({value}, {mode ?? "default"}) = {result}");
    })
    .Done()
  .Map("scale {value:int} --factor,-f {factor}")
    .WithHandler((int value, string factor) =>
    {
      double f = double.Parse(factor ?? "1", System.Globalization.CultureInfo.InvariantCulture);
      double result = value * f;
      WriteLine($"Scale({value}, {f}) = {result}");
    })
    .Done()
  .Map("")
    .WithHandler(() => "Typed params with options test works!")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);

namespace TimeWarp.Nuru.Tests.Generator.TypedParamsWithOptions
{
  /// <summary>
  /// Tests that verify the generator handles typed params in complex routes (with options).
  /// Bug #301: Generator complex routes skip type conversion.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("Bug")]
  [TestTag("Regression")]
  public class TypedParamsWithOptionsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TypedParamsWithOptionsTests>();

    /// <summary>
    /// Verify generated file extracts typed params to unique var names.
    /// </summary>
    public static async Task Should_extract_typed_param_to_unique_var()
    {
      string content = ReadGeneratedFile();

      // Should have unique var extraction (not direct to final name)
      content.ShouldContain("string __value_");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify generated file has type conversion for double.
    /// </summary>
    public static async Task Should_convert_double_typed_param()
    {
      string content = ReadGeneratedFile();

      // Should have double parsing
      content.ShouldContain("double value = double.Parse(");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify generated file has type conversion for int.
    /// </summary>
    public static async Task Should_convert_int_typed_param()
    {
      string content = ReadGeneratedFile();

      // Should have int parsing
      content.ShouldContain("int value = int.Parse(");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify generated file has option parsing for factor.
    /// </summary>
    public static async Task Should_parse_string_option_param()
    {
      string content = ReadGeneratedFile();

      // Should have factor option parsing
      content.ShouldContain("string? factor =");

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
        "generator-08-typed-params-with-options",
        "TimeWarp.Nuru.Analyzers",
        "TimeWarp.Nuru.Generators.NuruGenerator",
        "NuruGenerated.g.cs");

      if (!File.Exists(generatedFile))
      {
        throw new FileNotFoundException(
          $"Generated file not found at: {generatedFile}\n" +
          "This may indicate the generator did not run or Bug #301 is not fixed.");
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
