#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Bug #302 - Optional positional params generate wrong pattern
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly handles optional positional
// parameters, matching routes both with and without the optional value.
//
// HOW IT WORKS:
// 1. Routes with optional positional params use length-based matching
// 2. When optional param is omitted, variable is null
// 3. When optional param is provided, variable has the parsed value
//
// WHAT THIS TESTS:
// - sleep {seconds:int?} → matches both "sleep" and "sleep 5"
// - greet {name?} → matches both "greet" and "greet World"
//
// IMPORTANT: This test must be run in isolation (not via JARIBU_MULTI) because
// it reads the generated file from a path based on the runfile name.
// To run: ./tests/timewarp-nuru-core-tests/generator/generator-09-optional-positional-params.cs
// ═══════════════════════════════════════════════════════════════════════════════

#if JARIBU_MULTI
#error This test must be run in isolation. Run: ./tests/timewarp-nuru-core-tests/generator/generator-09-optional-positional-params.cs
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
  .Map("greet {name?}")
    .WithHandler((string? name) =>
    {
      WriteLine($"Hello, {name ?? "World"}!");
    })
    .Done()
  .Map("")
    .WithHandler(() => "Optional positional params test works!")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);

namespace TimeWarp.Nuru.Tests.Generator.OptionalPositionalParams
{
  /// <summary>
  /// Tests that verify the generator handles optional positional parameters.
  /// Bug #302: Generator optional positional params generate wrong pattern.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("Bug")]
  [TestTag("Regression")]
  public class OptionalPositionalParamsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<OptionalPositionalParamsTests>();

    /// <summary>
    /// Verify generated file uses length-based matching for optional params.
    /// </summary>
    public static async Task Should_use_length_based_matching()
    {
      string content = ReadGeneratedFile();

      // Should have length check, not list pattern
      content.ShouldContain("args.Length >= 1");

      // Should NOT have list pattern for sleep route
      content.ShouldNotContain("args is [\"sleep\", var");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify generated file extracts optional param with null fallback.
    /// </summary>
    public static async Task Should_extract_optional_param_with_null_fallback()
    {
      string content = ReadGeneratedFile();

      // Should have null fallback for optional typed param
      content.ShouldContain("args.Length > 1 ? args[1] : null");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify generated file has null check before parsing optional typed param.
    /// </summary>
    public static async Task Should_check_null_before_parsing_optional_param()
    {
      string content = ReadGeneratedFile();

      // Should have null check pattern for optional typed param
      content.ShouldContain("is not null ? int.Parse(");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify generated file handles untyped optional param.
    /// </summary>
    public static async Task Should_handle_untyped_optional_param()
    {
      string content = ReadGeneratedFile();

      // Should have string? for untyped optional param
      content.ShouldContain("string? name = args.Length > 1 ? args[1] : null;");

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
        "generator-09-optional-positional-params",
        "TimeWarp.Nuru.Analyzers",
        "TimeWarp.Nuru.Generators.NuruGenerator",
        "NuruGenerated.g.cs");

      if (!File.Exists(generatedFile))
      {
        throw new FileNotFoundException(
          $"Generated file not found at: {generatedFile}\n" +
          "This may indicate the generator did not run or Bug #302 is not fixed.");
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
