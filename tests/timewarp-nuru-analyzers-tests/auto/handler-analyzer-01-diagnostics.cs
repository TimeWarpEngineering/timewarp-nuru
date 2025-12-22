#!/usr/bin/dotnet --

// Integration tests for NuruHandlerAnalyzer diagnostics
// These tests verify that appropriate diagnostics are reported for unsupported handler patterns

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

return await RunTests<HandlerAnalyzerDiagnosticTests>();

/// <summary>
/// Tests for NuruHandlerAnalyzer diagnostics.
/// Verifies that NURU_H001, NURU_H002, NURU_H003, NURU_H004 are reported correctly.
/// </summary>
[TestTag("Analyzer")]
public sealed class HandlerAnalyzerDiagnosticTests
{
  /// <summary>
  /// Test that NURU_H001 is reported for instance method handlers.
  /// </summary>
  public static async Task Should_report_H001_for_instance_method()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      public class MyHandler
      {
        public void Handle(string env) => System.Console.WriteLine(env);
      }
      
      public class Program
      {
        public static void Main()
        {
          var handler = new MyHandler();
          var app = NuruCoreApp.CreateSlimBuilder()
            .Map("deploy {env}")
            .WithHandler(handler.Handle)
            .AsCommand()
            .Done()
            .Build();
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = await RunAnalyzer(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id == "NURU_H001").ShouldBeTrue(
      "Should report NURU_H001 for instance method handler");
  }

  /// <summary>
  /// Test that NURU_H002 is reported for lambdas with closures.
  /// </summary>
  public static async Task Should_report_H002_for_closure()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      public class Program
      {
        public static void Main()
        {
          bool executed = false;
          var app = NuruCoreApp.CreateSlimBuilder()
            .Map("test")
            .WithHandler(() => { executed = true; })
            .AsCommand()
            .Done()
            .Build();
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = await RunAnalyzer(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id == "NURU_H002").ShouldBeTrue(
      "Should report NURU_H002 for closure in handler lambda");
  }

  /// <summary>
  /// Test that NURU_H004 is reported for private method handlers.
  /// </summary>
  public static async Task Should_report_H004_for_private_method()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      public class Program
      {
        private static void PrivateHandler() => System.Console.WriteLine("private");
        
        public static void Main()
        {
          var app = NuruCoreApp.CreateSlimBuilder()
            .Map("secret")
            .WithHandler(PrivateHandler)
            .AsCommand()
            .Done()
            .Build();
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = await RunAnalyzer(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id == "NURU_H004").ShouldBeTrue(
      "Should report NURU_H004 for private method handler");
  }

  /// <summary>
  /// Test that no diagnostics are reported for valid public static method handlers.
  /// </summary>
  public static async Task Should_not_report_for_valid_public_static_method()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      public static class Handlers
      {
        public static void Deploy(string env) => System.Console.WriteLine(env);
      }
      
      public class Program
      {
        public static void Main()
        {
          var app = NuruCoreApp.CreateSlimBuilder()
            .Map("deploy {env}")
            .WithHandler(Handlers.Deploy)
            .AsCommand()
            .Done()
            .Build();
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = await RunAnalyzer(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id.StartsWith("NURU_H")).ShouldBeFalse(
      "Should not report any NURU_H diagnostics for valid public static method handler");
  }

  /// <summary>
  /// Test that no diagnostics are reported for valid lambda handlers without closures.
  /// </summary>
  public static async Task Should_not_report_for_valid_lambda()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      public class Program
      {
        public static void Main()
        {
          var app = NuruCoreApp.CreateSlimBuilder()
            .Map("greet {name}")
            .WithHandler((string name) => System.Console.WriteLine($"Hello, {name}!"))
            .AsCommand()
            .Done()
            .Build();
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = await RunAnalyzer(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id.StartsWith("NURU_H")).ShouldBeFalse(
      "Should not report any NURU_H diagnostics for valid lambda handler");
  }

  /// <summary>
  /// Test that no diagnostics are reported for internal static method handlers.
  /// </summary>
  public static async Task Should_not_report_for_internal_static_method()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      internal static class Handlers
      {
        internal static void Deploy(string env) => System.Console.WriteLine(env);
      }
      
      public class Program
      {
        public static void Main()
        {
          var app = NuruCoreApp.CreateSlimBuilder()
            .Map("deploy {env}")
            .WithHandler(Handlers.Deploy)
            .AsCommand()
            .Done()
            .Build();
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = await RunAnalyzer(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id.StartsWith("NURU_H")).ShouldBeFalse(
      "Should not report any NURU_H diagnostics for internal static method handler");
  }

  private static async Task<ImmutableArray<Diagnostic>> RunAnalyzer(string source)
  {
    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    
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

    if (!File.Exists(analyzerPath))
    {
      WriteLine($"ERROR: Analyzer not found at {analyzerPath}");
      return [];
    }

    Assembly analyzerAssembly = Assembly.LoadFrom(analyzerPath);
    
    // Find the NuruHandlerAnalyzer
    Type? analyzerType = analyzerAssembly.GetTypes()
      .FirstOrDefault(t => t.Name == "NuruHandlerAnalyzer");

    if (analyzerType == null)
    {
      WriteLine("ERROR: NuruHandlerAnalyzer type not found");
      return [];
    }

    DiagnosticAnalyzer analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(analyzerType)!;
    
    CompilationWithAnalyzersOptions options = new(
      new AnalyzerOptions([]),
      onAnalyzerException: null,
      concurrentAnalysis: true,
      logAnalyzerExecutionTime: false);

    CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
      [analyzer],
      options);

    ImmutableArray<Diagnostic> diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    
    return diagnostics;
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
