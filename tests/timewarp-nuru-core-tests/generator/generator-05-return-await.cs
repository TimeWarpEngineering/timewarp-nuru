#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Bug #298 - return await app.RunAsync(args) not intercepted
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly intercepts RunAsync when used
// with "return await" pattern, not just "await" alone.
//
// HOW IT WORKS:
// 1. Top-level NuruApp with "return await app.RunAsync(args)" triggers generator
// 2. If it compiles and runs, the generated interceptor is working
// 3. Jaribu tests verify the generated file content
//
// WHAT THIS TESTS:
// - "await app.RunAsync(args);" - works (control case)
// - "return await app.RunAsync(args);" - Bug #298 says this fails
//
// IMPORTANT: This test must be run in isolation (not via JARIBU_MULTI) because
// it reads the generated file from a path based on the runfile name.
// To run: ./tests/timewarp-nuru-core-tests/generator/generator-05-return-await.cs
// ═══════════════════════════════════════════════════════════════════════════════

#if JARIBU_MULTI
#error This test must be run in isolation. Run: ./tests/timewarp-nuru-core-tests/generator/generator-05-return-await.cs
#endif

using TimeWarp.Nuru;

// Top-level NuruApp - triggers generator
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("")
    .WithHandler(() => "Return await pattern works!")
    .AsQuery()
    .Done()
  .Build();

// THIS IS THE BUG: "return await" pattern is not intercepted
// If this line works, Bug #298 is fixed
return await app.RunAsync(args);

#if !JARIBU_MULTI
// Note: We can't reach here because of the return above
// But if we could, we'd run tests
// return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.ReturnAwait
{
  /// <summary>
  /// Tests that verify the generator intercepts "return await app.RunAsync(args)".
  /// Bug #298: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/298
  /// </summary>
  [TestTag("Generator")]
  [TestTag("Bug")]
  [TestTag("Regression")]
  public class ReturnAwaitTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ReturnAwaitTests>();

    /// <summary>
    /// Verify generated file exists (generator ran successfully).
    /// </summary>
    public static async Task Should_generate_interceptor_for_return_await()
    {
      string content = ReadGeneratedFile();

      // Should have the interceptor method
      content.ShouldContain("RunAsync_Intercepted");

      // Should have InterceptsLocation attribute
      content.ShouldContain("InterceptsLocationAttribute");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify the route is in the generated code.
    /// </summary>
    public static async Task Should_generate_route_for_empty_pattern()
    {
      string content = ReadGeneratedFile();

      // Should have the empty route pattern
      content.ShouldContain("args is []");

      // Should have our handler output
      content.ShouldContain("Return await pattern works!");

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
        "generator-05-return-await",
        "TimeWarp.Nuru.Analyzers",
        "TimeWarp.Nuru.Generators.NuruGenerator",
        "NuruGenerated.g.cs");

      if (!File.Exists(generatedFile))
      {
        throw new FileNotFoundException(
          $"Generated file not found at: {generatedFile}\n" +
          "This may indicate the generator did not run or Bug #298 is not fixed.");
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
