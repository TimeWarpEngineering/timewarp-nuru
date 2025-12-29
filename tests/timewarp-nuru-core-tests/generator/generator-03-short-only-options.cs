#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Short-Only Options (#287, #288)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly handles short-only options like
// -v (flag) and -n {n:int} (value option) without generating invalid code.
//
// HOW IT WORKS:
// 1. Top-level NuruApp with short-only options triggers generator at compile time
// 2. If it compiles and runs, the generated code is valid
// 3. Jaribu tests verify the generated file content
//
// WHAT THIS TESTS:
// - Before fix #287: bool  = Array.Exists(...) - empty variable name, won't compile
// - Before fix #288: args[__idx] == "--" - wrong option check
// - After fix: bool v = Array.Exists(args, a => a == "-v") - correct code
//
// IMPORTANT: This test must be run in isolation (not via JARIBU_MULTI) because
// it reads the generated file from a path based on the runfile name. In multi-run
// mode, the project name changes and the path would be incorrect.
// To run: dotnet run tests/timewarp-nuru-core-tests/generator/generator-03-short-only-options.cs
// ═══════════════════════════════════════════════════════════════════════════════

#if JARIBU_MULTI
#error This test must be run in isolation. Run: dotnet run tests/timewarp-nuru-core-tests/generator/generator-03-short-only-options.cs
#endif

using TimeWarp.Nuru;

// Top-level NuruApp - triggers generator. If this compiles, the fix works!
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("flag -v")
    .WithHandler((bool v) => Console.WriteLine($"v={v}"))
    .Done()
  .Map("value -n {n:int}")
    .WithHandler((int n) => Console.WriteLine($"n={n}"))
    .Done()
  .Map("mixed --verbose,-v -f")
    .WithHandler((bool verbose, bool f) => Console.WriteLine($"verbose={verbose} f={f}"))
    .Done()
  .Build();

// Run the app to verify generated code executes
await app.RunAsync(["flag", "-v"]);

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.ShortOnlyOptions
{
  /// <summary>
  /// Tests that verify the generated file content for short-only options.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("Options")]
  [TestTag("Regression")]
  public class ShortOnlyOptionsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ShortOnlyOptionsTests>();

    /// <summary>
    /// Verify short-only flag generates correct variable name (not empty).
    /// </summary>
    public static async Task Should_generate_variable_name_for_short_only_flag()
    {
      string content = ReadGeneratedFile();

      // Variable name should be 'v' (not empty)
      content.ShouldContain("bool v = Array.Exists");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify short-only flag generates correct condition (not "--").
    /// </summary>
    public static async Task Should_generate_correct_condition_for_short_only_flag()
    {
      string content = ReadGeneratedFile();

      // Should check -v only
      content.ShouldContain("a == \"-v\"");

      // Should NOT have empty long form "--"
      content.ShouldNotContain("a == \"--\"");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify short-only value option generates correct condition.
    /// </summary>
    public static async Task Should_generate_correct_condition_for_short_only_value_option()
    {
      string content = ReadGeneratedFile();

      // Should check -n only (not "--")
      content.ShouldContain("args[__idx] == \"-n\"");

      // Should NOT have empty long form check
      content.ShouldNotContain("args[__idx] == \"--\"");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify mixed options (--verbose,-v) generate both forms.
    /// </summary>
    public static async Task Should_generate_both_forms_for_long_short_option()
    {
      string content = ReadGeneratedFile();

      // --verbose,-v should have both forms
      content.ShouldContain("a == \"--verbose\" || a == \"-v\"");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify short-only flag (-f) in mixed pattern generates correctly.
    /// </summary>
    public static async Task Should_generate_correct_code_for_short_only_in_mixed_pattern()
    {
      string content = ReadGeneratedFile();

      // -f (short-only) should only have short form
      content.ShouldContain("bool f = Array.Exists");
      content.ShouldContain("a == \"-f\"");

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
        "generator-03-short-only-options",
        "TimeWarp.Nuru.Analyzers",
        "TimeWarp.Nuru.Generators.NuruGenerator",
        "NuruGenerated.g.cs");

      if (!File.Exists(generatedFile))
      {
        throw new FileNotFoundException(
          $"Generated file not found at: {generatedFile}\n" +
          "This may indicate the generator did not run or the path has changed.");
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
