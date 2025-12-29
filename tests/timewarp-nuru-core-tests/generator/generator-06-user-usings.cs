#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Bug #299 - Generator does not include user's using directives
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly includes user's using directives
// (both regular and static) in generated code.
//
// HOW IT WORKS:
// 1. Source file has "using static System.Console;" and "using static System.Math;"
// 2. Handler lambdas use WriteLine() and Abs() without namespace prefix
// 3. If it compiles and runs, the generator included the usings correctly
//
// WHAT THIS TESTS:
// - "using static System.Console;" - WriteLine() works in handlers
// - "using static System.Math;" - Abs(), Round() work in handlers
// - Multiple user usings are preserved
//
// IMPORTANT: This test must be run in isolation (not via JARIBU_MULTI) because
// it reads the generated file from a path based on the runfile name.
// To run: ./tests/timewarp-nuru-core-tests/generator/generator-06-user-usings.cs
// ═══════════════════════════════════════════════════════════════════════════════

#if JARIBU_MULTI
#error This test must be run in isolation. Run: ./tests/timewarp-nuru-core-tests/generator/generator-06-user-usings.cs
#endif

using static System.Console;
using static System.Math;
using TimeWarp.Nuru;

// Top-level NuruApp - triggers generator
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("echo {message}")
    .WithHandler((string message) =>
    {
      // This uses WriteLine from "using static System.Console;"
      WriteLine($"Echo: {message}");
    })
    .Done()
  .Map("abs {value:int}")
    .WithHandler((int value) =>
    {
      // This uses Abs from "using static System.Math;"
      int result = Abs(value);
      WriteLine($"Abs({value}) = {result}");
    })
    .Done()
  .Map("")
    .WithHandler(() => "User usings test works!")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);

namespace TimeWarp.Nuru.Tests.Generator.UserUsings
{
  /// <summary>
  /// Tests that verify the generator includes user's using directives.
  /// Bug #299: Generator does not include user's using directives in generated code.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("Bug")]
  [TestTag("Regression")]
  public class UserUsingsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<UserUsingsTests>();

    /// <summary>
    /// Verify generated file contains user's static usings.
    /// </summary>
    public static async Task Should_include_user_static_usings_in_generated_code()
    {
      string content = ReadGeneratedFile();

      // Should have the user's static usings in global form
      content.ShouldContain("using static global::System.Console;");
      content.ShouldContain("using static global::System.Math;");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify the user usings section comment is present.
    /// </summary>
    public static async Task Should_have_user_usings_section_comment()
    {
      string content = ReadGeneratedFile();

      // Should have the comment marking user usings section
      content.ShouldContain("// User-defined usings");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify the generated file contains both routes.
    /// </summary>
    public static async Task Should_generate_routes_with_user_using_dependencies()
    {
      string content = ReadGeneratedFile();

      // Should have the echo handler
      content.ShouldContain("Echo:");

      // Should have the abs handler
      content.ShouldContain("Abs(");

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
        "generator-06-user-usings",
        "TimeWarp.Nuru.Analyzers",
        "TimeWarp.Nuru.Generators.NuruGenerator",
        "NuruGenerated.g.cs");

      if (!File.Exists(generatedFile))
      {
        throw new FileNotFoundException(
          $"Generated file not found at: {generatedFile}\n" +
          "This may indicate the generator did not run or Bug #299 is not fixed.");
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
