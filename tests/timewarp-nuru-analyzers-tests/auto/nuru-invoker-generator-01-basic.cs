#!/usr/bin/dotnet --

// Integration tests for NuruInvokerGenerator source generator
// These tests verify that the generator extracts delegate signatures and generates invoker code

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

return await RunTests<NuruInvokerGeneratorTests>();

/// <summary>
/// Tests for NuruInvokerGenerator source generator.
/// Verifies that Map() calls are detected and typed invokers are generated.
/// </summary>
[TestTag("SourceGenerator")]
public sealed class NuruInvokerGeneratorTests
{
  /// <summary>
  /// Basic test to verify the generator runs without errors on a simple Map().WithHandler() call.
  /// </summary>
  public static async Task Should_run_generator_without_errors()
  {
    // Arrange - simple code with a Map().WithHandler() call (new fluent API)
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder()
        .Map("status")
        .WithHandler(() => System.Console.WriteLine("OK"))
        .AsQuery()
        .Done()
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
      
      var app = NuruApp.CreateBuilder()
        .Map("status")
        .WithHandler(() => System.Console.WriteLine("OK"))
        .AsQuery()
        .Done()
        .Map("greet {name}")
        .WithHandler((string name) => System.Console.WriteLine(name))
        .AsCommand()
        .Done()
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
      
      var app = NuruApp.CreateBuilder()
        // Action (no params, no return)
        .Map("status")
        .WithHandler(() => System.Console.WriteLine("OK"))
        .AsQuery()
        .Done()
        // Action<string>
        .Map("greet {name}")
        .WithHandler((string name) => System.Console.WriteLine(name))
        .AsCommand()
        .Done()
        // Action<int, int>
        .Map("add {x:int} {y:int}")
        .WithHandler((int x, int y) => System.Console.WriteLine(x + y))
        .AsCommand()
        .Done()
        // Func<int, int, int>
        .Map("multiply {x:int} {y:int}")
        .WithHandler((int x, int y) => x * y)
        .AsQuery()
        .Done()
        // Func<string, Task>
        .Map("async-greet {name}")
        .WithHandler(async (string name) => { await Task.Delay(1); System.Console.WriteLine(name); })
        .AsCommand()
        .Done()
        // Action<string[]>
        .Map("docker {*args}")
        .WithHandler((string[] args) => System.Console.WriteLine(string.Join(" ", args)))
        .AsCommand()
        .Done()
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
  /// Test that default route (empty pattern) is detected and generates invokers.
  /// This replaces the old MapDefault() test - now using Map("").WithHandler().
  /// </summary>
  public static async Task Should_detect_default_route_invocations()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder()
        // Default route with Func<int> - should generate invoker
        .Map("")
        .WithHandler(() => 42)
        .AsQuery()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    WriteLine($"Generated trees: {result.GeneratedTrees.Length}");
    
    // Check if GeneratedRouteInvokers.g.cs was created
    SyntaxTree? invokersTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedRouteInvokers"));
    
    invokersTree.ShouldNotBeNull("GeneratedRouteInvokers.g.cs should be generated for default route");
    
    string content = invokersTree.GetText().ToString();
    WriteLine("Generated content:");
    WriteLine(content);
    
    // The generated code should contain an invoker for Func<int> signature
    content.Contains("_Returns_Int").ShouldBeTrue("Generated code should contain invoker for Func<int> signature");
    
    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that both default route and named routes work together.
  /// </summary>
  public static async Task Should_detect_both_default_and_named_routes()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder()
        // Default route with Func<int>
        .Map("")
        .WithHandler(() => 42)
        .AsQuery()
        .Done()
        // Named route with Action<string>
        .Map("greet {name}")
        .WithHandler((string name) => System.Console.WriteLine(name))
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);
    
    SyntaxTree? invokersTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedRouteInvokers"));
    
    invokersTree.ShouldNotBeNull("GeneratedRouteInvokers.g.cs should be generated");
    
    string content = invokersTree.GetText().ToString();
    WriteLine("Generated content:");
    WriteLine(content);
    
    // Should contain invokers for both signatures
    content.Contains("_Returns_Int").ShouldBeTrue("Generated code should contain invoker for Func<int> from default route");
    // Action<string> generates just "String" (no returns void suffix)
    content.Contains("Invoke_String").ShouldBeTrue("Generated code should contain invoker for Action<string> from named route");
    
    await Task.CompletedTask;
  }

  /// <summary>
  /// Debug test to trace through generator execution.
  /// </summary>
  public static async Task Should_trace_generator_execution()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      NuruAppBuilder builder = NuruApp.CreateBuilder();
      builder.Map("test")
        .WithHandler(() => System.Console.WriteLine("test"))
        .AsCommand()
        .Done();
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

    // Find all WithHandler invocations in syntax trees (new fluent API)
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
          if (methodName == "WithHandler")
          {
            WriteLine($"Found WithHandler() call at {inv.GetLocation()}");
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
    // Look for the solution file starting from current directory
    // We specifically look for timewarp-nuru.slnx (not Directory.Build.props)
    // because Directory.Build.props exists in multiple subdirectories
    string? dir = Environment.CurrentDirectory;
    while (dir != null)
    {
      if (File.Exists(Path.Combine(dir, "timewarp-nuru.slnx")))
      {
        return dir;
      }
      dir = Path.GetDirectoryName(dir);
    }
    
    // If not found, throw a clear error instead of using hardcoded fallback
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
