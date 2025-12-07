#!/usr/bin/dotnet --
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.CodeAnalysis.CSharp

// Integration tests for NuruInvokerGenerator source generator
// These tests verify that the generator extracts delegate signatures and generates invoker code

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TimeWarp.Jaribu;
using Shouldly;
using static System.Console;
using static TimeWarp.Jaribu.TestRunner;

return await RunTests<NuruInvokerGeneratorTests>();

/// <summary>
/// Tests for NuruInvokerGenerator source generator.
/// Verifies that Map() calls are detected and typed invokers are generated.
/// </summary>
[TestTag("SourceGenerator")]
public sealed class NuruInvokerGeneratorTests
{
  /// <summary>
  /// Basic test to verify the generator runs without errors on a simple Map() call.
  /// </summary>
  public static async Task Should_run_generator_without_errors()
  {
    // Arrange - simple code with a Map() call
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("status", () => System.Console.WriteLine("OK"))
        .Build();
      """;

    // Act - run the generator
    GeneratorDriverRunResult result = RunGenerator(code);

    // Assert - no diagnostics (errors/warnings from generator)
    WriteLine($"Generator diagnostics count: {result.Diagnostics.Length}");
    foreach (Diagnostic d in result.Diagnostics)
    {
      WriteLine($"  {d.Severity}: {d.Id} - {d.GetMessage()}");
    }

    result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test to see what trees are generated.
  /// </summary>
  public static async Task Should_show_generated_trees()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("status", () => System.Console.WriteLine("OK"))
        .Map("greet {name}", (string name) => System.Console.WriteLine(name))
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    WriteLine($"Generated trees count: {result.GeneratedTrees.Length}");
    foreach (SyntaxTree tree in result.GeneratedTrees)
    {
      WriteLine($"  Tree: {tree.FilePath}");
      string content = tree.GetText().ToString();
      if (content.Length > 500)
      {
        WriteLine($"  Content (first 500 chars):\n{content[..500]}...");
      }
      else
      {
        WriteLine($"  Content:\n{content}");
      }
    }

    // Also check per-generator results
    foreach (GeneratorRunResult genResult in result.Results)
    {
      WriteLine($"Generator: {genResult.Generator.GetGeneratorType().Name}");
      WriteLine($"  Generated sources: {genResult.GeneratedSources.Length}");
      WriteLine($"  Diagnostics: {genResult.Diagnostics.Length}");
      WriteLine($"  Exception: {genResult.Exception?.Message ?? "none"}");
      
      foreach (GeneratedSourceResult source in genResult.GeneratedSources)
      {
        WriteLine($"  Source: {source.HintName}");
      }
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test with multiple different delegate signatures.
  /// </summary>
  public static async Task Should_handle_various_delegate_signatures()
  {
    const string code = """
      using TimeWarp.Nuru;
      using System.Threading.Tasks;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        // Action (no params, no return)
        .Map("status", () => System.Console.WriteLine("OK"))
        // Action<string>
        .Map("greet {name}", (string name) => System.Console.WriteLine(name))
        // Action<int, int>
        .Map("add {x:int} {y:int}", (int x, int y) => System.Console.WriteLine(x + y))
        // Func<int, int, int>
        .Map("multiply {x:int} {y:int}", (int x, int y) => x * y)
        // Func<string, Task>
        .Map("async-greet {name}", async (string name) => { await Task.Delay(1); System.Console.WriteLine(name); })
        // Action<string[]>
        .Map("docker {*args}", (string[] args) => System.Console.WriteLine(string.Join(" ", args)))
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    WriteLine($"Generated trees: {result.GeneratedTrees.Length}");
    
    // Check if GeneratedRouteInvokers.g.cs was created
    SyntaxTree? invokersTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedRouteInvokers"));
    
    if (invokersTree != null)
    {
      WriteLine("SUCCESS: GeneratedRouteInvokers.g.cs was generated!");
      WriteLine(invokersTree.GetText().ToString());
    }
    else
    {
      WriteLine("FAILURE: GeneratedRouteInvokers.g.cs was NOT generated");
      WriteLine("Available trees:");
      foreach (SyntaxTree tree in result.GeneratedTrees)
      {
        WriteLine($"  - {tree.FilePath}");
      }
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Debug test to trace through generator execution.
  /// </summary>
  public static async Task Should_trace_generator_execution()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      NuruCoreAppBuilder builder = NuruCoreApp.CreateSlimBuilder();
      builder.Map("test", () => System.Console.WriteLine("test"));
      var app = builder.Build();
      """;

    // Create compilation
    CSharpCompilation compilation = CreateCompilationWithNuru(code);

    // Check for compilation errors first
    ImmutableArray<Diagnostic> compDiagnostics = compilation.GetDiagnostics();
    WriteLine($"Compilation diagnostics: {compDiagnostics.Length}");
    foreach (Diagnostic d in compDiagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning))
    {
      WriteLine($"  {d.Severity}: {d.Id} - {d.GetMessage()}");
    }

    // Find all Map invocations in syntax trees
    foreach (SyntaxTree tree in compilation.SyntaxTrees)
    {
      SemanticModel model = compilation.GetSemanticModel(tree);
      var root = tree.GetCompilationUnitRoot();
      
      var invocations = root.DescendantNodes()
        .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>();
      
      foreach (var inv in invocations)
      {
        if (inv.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax memberAccess)
        {
          string methodName = memberAccess.Name.Identifier.Text;
          if (methodName == "Map")
          {
            WriteLine($"Found Map() call at {inv.GetLocation()}");
            WriteLine($"  Arguments: {inv.ArgumentList.Arguments.Count}");
            
            foreach (var arg in inv.ArgumentList.Arguments)
            {
              WriteLine($"    Arg: {arg.Expression.GetType().Name} - {arg}");
              
              // Get type info for lambdas
              if (arg.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.LambdaExpressionSyntax lambda)
              {
                Microsoft.CodeAnalysis.TypeInfo typeInfo = model.GetTypeInfo(lambda);
                WriteLine($"      Type: {typeInfo.Type?.ToDisplayString() ?? "null"}");
                WriteLine($"      ConvertedType: {typeInfo.ConvertedType?.ToDisplayString() ?? "null"}");
                
                SymbolInfo symbolInfo = model.GetSymbolInfo(lambda);
                WriteLine($"      Symbol: {symbolInfo.Symbol?.ToDisplayString() ?? "null"}");
              }
            }
          }
        }
      }
    }

    // Now run the generator
    GeneratorDriverRunResult result = RunGeneratorWithCompilation(compilation);
    
    WriteLine($"\nGenerator results:");
    WriteLine($"  Generated trees: {result.GeneratedTrees.Length}");
    
    foreach (GeneratorRunResult genResult in result.Results)
    {
      WriteLine($"  Generator: {genResult.Generator.GetGeneratorType().Name}");
      WriteLine($"    Exception: {genResult.Exception?.ToString() ?? "none"}");
      WriteLine($"    Sources: {genResult.GeneratedSources.Length}");
      
      // Check tracked steps if available
      if (genResult.TrackedSteps.Count > 0)
      {
        WriteLine($"    Tracked steps: {genResult.TrackedSteps.Count}");
        foreach (var step in genResult.TrackedSteps)
        {
          WriteLine($"      {step.Key}: {step.Value.Length} outputs");
        }
      }
    }

    await Task.CompletedTask;
  }

  private static GeneratorDriverRunResult RunGenerator(string source)
  {
    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    return RunGeneratorWithCompilation(compilation);
  }

  private static GeneratorDriverRunResult RunGeneratorWithCompilation(CSharpCompilation compilation)
  {
    // Use absolute path to the analyzer assembly in the source tree
    // This works because runfiles are executed from the repo root context
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
      // Try Release build
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

    // Create and run the driver with incremental tracking enabled
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
    // Look for common repo markers starting from current directory
    string? dir = Environment.CurrentDirectory;
    while (dir != null)
    {
      if (File.Exists(Path.Combine(dir, "timewarp-nuru.slnx")) ||
          File.Exists(Path.Combine(dir, "Directory.Build.props")))
      {
        return dir;
      }
      dir = Path.GetDirectoryName(dir);
    }
    
    // Fallback to hardcoded path for this worktree
    return "/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/Cramer-2025-08-30-dev";
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

    // Add all TimeWarp.Nuru.* assemblies from the build output
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
    else
    {
      WriteLine($"WARNING: Nuru directory not found: {nuruDir}");
    }

    return CSharpCompilation.Create(
      "TestAssembly",
      [CSharpSyntaxTree.ParseText(source)],
      references,
      new CSharpCompilationOptions(OutputKind.ConsoleApplication));
  }
}
