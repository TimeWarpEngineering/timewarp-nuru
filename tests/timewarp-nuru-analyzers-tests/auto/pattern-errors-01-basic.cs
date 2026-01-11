#!/usr/bin/dotnet --

// Integration tests for pattern parse error reporting through unified NuruAnalyzer
// These tests verify that NURU_P### and NURU_S### errors are reported through extraction

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

return await RunTests<PatternErrorTests>();

/// <summary>
/// Tests for pattern error reporting through unified analyzer.
/// Verifies that NURU_P### (parse) and NURU_S### (semantic) errors flow through extraction.
/// </summary>
[TestTag("Analyzer")]
public sealed class PatternErrorTests
{
  /// <summary>
  /// Test that NURU_S001 is reported for duplicate parameter names.
  /// </summary>
  public static async Task Should_report_S001_for_duplicate_parameter_names()
  {
    const string code = """
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          NuruCoreApp app = NuruApp.CreateBuilder([])
            .Map("copy {source} {source}").WithHandler((string source) => $"copy:{source}").AsQuery().Done()
            .Build();

          await app.RunAsync([]);
        }
      }
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    ImmutableArray<Diagnostic> diagnostics = result.Diagnostics;

    WriteLine($"Found {diagnostics.Length} diagnostics from generator:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    // Also check per-generator diagnostics
    foreach (GeneratorRunResult genResult in result.Results)
    {
      WriteLine($"\nGenerator {genResult.Generator.GetGeneratorType().Name}:");
      foreach (Diagnostic d in genResult.Diagnostics)
      {
        WriteLine($"  {d.Id}: {d.GetMessage()}");
      }
    }

    bool hasDuplicateParamError = diagnostics.Any(d => d.Id == "NURU_S001") ||
      result.Results.Any(r => r.Diagnostics.Any(d => d.Id == "NURU_S001"));

    hasDuplicateParamError.ShouldBeTrue(
      "Should report NURU_S001 for duplicate parameter names");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that NURU_S006 is reported for optional before required parameter.
  /// </summary>
  public static async Task Should_report_S006_for_optional_before_required()
  {
    const string code = """
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          NuruCoreApp app = NuruApp.CreateBuilder([])
            .Map("deploy {env?} {region}").WithHandler((string? env, string region) => $"deploy").AsQuery().Done()
            .Build();

          await app.RunAsync([]);
        }
      }
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    ImmutableArray<Diagnostic> diagnostics = result.Diagnostics;

    WriteLine($"Found {diagnostics.Length} diagnostics from generator:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    // Check for NURU_S006 (OptionalBeforeRequired)
    bool hasOptionalError = diagnostics.Any(d => d.Id == "NURU_S006") ||
      result.Results.Any(r => r.Diagnostics.Any(d => d.Id == "NURU_S006"));

    hasOptionalError.ShouldBeTrue(
      "Should report NURU_S006 for optional parameter before required parameter");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that no errors are reported for valid patterns.
  /// </summary>
  public static async Task Should_not_report_errors_for_valid_patterns()
  {
    const string code = """
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          NuruCoreApp app = NuruApp.CreateBuilder([])
            .Map("deploy {env} {region?}").WithHandler((string env, string? region) => $"deploy:{env}").AsQuery().Done()
            .Build();

          await app.RunAsync([]);
        }
      }
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    ImmutableArray<Diagnostic> diagnostics = result.Diagnostics;

    WriteLine($"Found {diagnostics.Length} diagnostics from generator:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    bool hasPatternError = diagnostics.Any(d => d.Id.StartsWith("NURU_P") || d.Id.StartsWith("NURU_S")) ||
      result.Results.Any(r => r.Diagnostics.Any(d => d.Id.StartsWith("NURU_P") || d.Id.StartsWith("NURU_S")));

    // Filter out DEBUG messages
    hasPatternError = diagnostics.Any(d => (d.Id.StartsWith("NURU_P") || d.Id.StartsWith("NURU_S")) && !d.Id.Contains("DEBUG"));

    hasPatternError.ShouldBeFalse(
      "Should not report pattern errors for valid patterns");

    await Task.CompletedTask;
  }

  private static GeneratorDriverRunResult RunGenerator(string source)
  {
    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    return RunGeneratorWithCompilation(compilation);
  }

  private static GeneratorDriverRunResult RunGeneratorWithCompilation(CSharpCompilation compilation)
  {
    string repoRoot = FindRepoRoot();
    string analyzerPath = Path.Combine(
      repoRoot,
      "source",
      "timewarp-nuru-analyzers",
      "bin",
      "Debug",
      "net10.0",
      "TimeWarp.Nuru.Analyzers.dll");

    if (!File.Exists(analyzerPath))
    {
      analyzerPath = Path.Combine(
        repoRoot,
        "source",
        "timewarp-nuru-analyzers",
        "bin",
        "Release",
        "net10.0",
        "TimeWarp.Nuru.Analyzers.dll");
    }

    ISourceGenerator[] generators;

    if (File.Exists(analyzerPath))
    {
      Assembly analyzerAssembly = Assembly.LoadFrom(analyzerPath);

      Type[] generatorTypes = analyzerAssembly.GetTypes()
        .Where(t => !t.IsAbstract && t.GetInterfaces().Any(i => i.Name == "IIncrementalGenerator"))
        .ToArray();

      generators = generatorTypes
        .Select(t => (IIncrementalGenerator)Activator.CreateInstance(t)!)
        .Select(g => g.AsSourceGenerator())
        .ToArray();
    }
    else
    {
      generators = [];
    }

    GeneratorDriverOptions options = new(
      IncrementalGeneratorOutputKind.None,
      trackIncrementalGeneratorSteps: true);

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators,
      driverOptions: options);

    driver = driver.RunGenerators(compilation);
    return driver.GetRunResult();
  }

  private static string FindRepoRoot()
  {
    string? dir = Environment.CurrentDirectory;
    while (dir != null)
    {
      if (File.Exists(Path.Combine(dir, "timewarp-nuru.slnx")))
      {
        return dir;
      }

      dir = Path.GetDirectoryName(dir);
    }

    throw new InvalidOperationException(
      $"Could not find repository root (timewarp-nuru.slnx) starting from {Environment.CurrentDirectory}");
  }

  private static CSharpCompilation CreateCompilationWithNuru(string source)
  {
    string runtimePath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
    string repoRoot = FindRepoRoot();
    string nuruDir = Path.Combine(repoRoot, "source", "timewarp-nuru", "bin", "Debug", "net10.0");

    List<MetadataReference> references =
    [
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll"))
    ];

    if (Directory.Exists(nuruDir))
    {
      foreach (string dll in Directory.GetFiles(nuruDir, "*.dll"))
      {
        string fileName = Path.GetFileName(dll);
        if (!references.Any(r => r.Display?.Contains(fileName) == true))
        {
          references.Add(MetadataReference.CreateFromFile(dll));
        }
      }
    }

    return CSharpCompilation.Create(
      "TestAssembly",
      [CSharpSyntaxTree.ParseText(source)],
      references,
      new CSharpCompilationOptions(OutputKind.ConsoleApplication));
  }
}
