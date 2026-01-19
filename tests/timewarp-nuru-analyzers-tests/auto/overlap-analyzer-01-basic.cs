#!/usr/bin/dotnet --

// Integration tests for NuruAnalyzer overlap detection (NURU_R001)
// These tests verify that overlapping route patterns with different type constraints are detected

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

return await RunTests<OverlapAnalyzerTests>();

/// <summary>
/// Tests for NuruAnalyzer overlap detection.
/// Verifies that NURU_R001 is reported for overlapping routes with different type constraints.
/// </summary>
[TestTag("Analyzer")]
public sealed class OverlapAnalyzerTests
{
  /// <summary>
  /// Test that NURU_R001 is reported for typed vs untyped parameter conflict.
  /// </summary>
  public static async Task Should_report_R001_for_typed_vs_untyped_parameter()
  {
    const string code = """
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          NuruCoreApp app = NuruApp.CreateBuilder([])
            .Map("delay {ms:int}").WithHandler((int ms) => $"typed:{ms}").AsQuery().Done()
            .Map("delay {duration}").WithHandler((string duration) => $"untyped:{duration}").AsQuery().Done()
            .Build();

          await app.RunAsync([]);
        }
      }
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    // Check for NURU_R001 diagnostics
    ImmutableArray<Diagnostic> diagnostics = result.Diagnostics;

    WriteLine($"Found {diagnostics.Length} diagnostics from generator:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()} at {d.Location.GetLineSpan()}");
    }

    // Also check generated source diagnostics
    foreach (GeneratorRunResult genResult in result.Results)
    {
      WriteLine($"\nGenerator {genResult.Generator.GetGeneratorType().Name}:");
      WriteLine($"  Diagnostics: {genResult.Diagnostics.Length}");
      foreach (Diagnostic d in genResult.Diagnostics)
      {
        WriteLine($"    {d.Id}: {d.GetMessage()}");
      }
    }

    bool hasOverlapError = diagnostics.Any(d => d.Id == "NURU_R001") ||
      result.Results.Any(r => r.Diagnostics.Any(d => d.Id == "NURU_R001"));

    hasOverlapError.ShouldBeTrue(
      "Should report NURU_R001 for typed vs untyped parameter overlap");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that NURU_R001 is reported for different typed parameters.
  /// </summary>
  public static async Task Should_report_R001_for_different_typed_parameters()
  {
    const string code = """
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          NuruCoreApp app = NuruApp.CreateBuilder([])
            .Map("get {id:int}").WithHandler((int id) => $"int:{id}").AsQuery().Done()
            .Map("get {id:guid}").WithHandler((Guid id) => $"guid:{id}").AsQuery().Done()
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

    bool hasOverlapError = diagnostics.Any(d => d.Id == "NURU_R001") ||
      result.Results.Any(r => r.Diagnostics.Any(d => d.Id == "NURU_R001"));

    hasOverlapError.ShouldBeTrue(
      "Should report NURU_R001 for different typed parameters (int vs guid)");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that no NURU_R001 is reported for non-overlapping routes.
  /// </summary>
  public static async Task Should_not_report_R001_for_non_overlapping_routes()
  {
    const string code = """
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          NuruCoreApp app = NuruApp.CreateBuilder([])
            .Map("get-by-id {id:int}").WithHandler((int id) => $"int:{id}").AsQuery().Done()
            .Map("get-by-name {name}").WithHandler((string name) => $"string:{name}").AsQuery().Done()
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

    bool hasOverlapError = diagnostics.Any(d => d.Id == "NURU_R001") ||
      result.Results.Any(r => r.Diagnostics.Any(d => d.Id == "NURU_R001"));

    hasOverlapError.ShouldBeFalse(
      "Should not report NURU_R001 for routes with different literal prefixes");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that no NURU_R001 is reported for required vs optional parameter.
  /// (These have different signatures: {P} vs {P?})
  /// </summary>
  public static async Task Should_not_report_R001_for_required_vs_optional()
  {
    const string code = """
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          NuruCoreApp app = NuruApp.CreateBuilder([])
            .Map("deploy {env}").WithHandler((string env) => $"required:{env}").AsQuery().Done()
            .Map("deploy {env?}").WithHandler((string? env) => $"optional:{env}").AsQuery().Done()
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

    bool hasOverlapError = diagnostics.Any(d => d.Id == "NURU_R001") ||
      result.Results.Any(r => r.Diagnostics.Any(d => d.Id == "NURU_R001"));

    hasOverlapError.ShouldBeFalse(
      "Should not report NURU_R001 for required vs optional parameters (different signatures)");

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

    WriteLine($"Looking for analyzer at: {analyzerPath}");
    WriteLine($"Exists: {File.Exists(analyzerPath)}");

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
      WriteLine($"Trying Release: {analyzerPath}");
      WriteLine($"Exists: {File.Exists(analyzerPath)}");
    }

    ISourceGenerator[] generators;

    if (File.Exists(analyzerPath))
    {
      Assembly analyzerAssembly = Assembly.LoadFrom(analyzerPath);

      // Find all IIncrementalGenerator types
      Type[] generatorTypes = analyzerAssembly.GetTypes()
        .Where(t => !t.IsAbstract && t.GetInterfaces().Any(i => i.Name == "IIncrementalGenerator"))
        .ToArray();

      WriteLine($"Found {generatorTypes.Length} generator types:");
      foreach (Type t in generatorTypes)
      {
        WriteLine($"  - {t.FullName}");
      }

      generators = generatorTypes
        .Select(t => (IIncrementalGenerator)Activator.CreateInstance(t)!)
        .Select(g => g.AsSourceGenerator())
        .ToArray();
    }
    else
    {
      WriteLine("WARNING: Could not find analyzer assembly, using empty generators");
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
      MetadataReference.CreateFromFile(typeof(System.Guid).Assembly.Location),
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
