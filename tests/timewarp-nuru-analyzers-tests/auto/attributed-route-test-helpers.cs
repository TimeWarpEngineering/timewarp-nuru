// ═══════════════════════════════════════════════════════════════════════════════
// ATTRIBUTED ROUTE TEST HELPERS - Shared utilities for source generator tests
// ═══════════════════════════════════════════════════════════════════════════════
// Provides helper methods for running the NuruAttributedRouteGenerator and
// inspecting generated source code.
//
// This file is included via Directory.Build.props as a shared compile item.
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Shared test helpers for attributed route source generator tests.
/// </summary>
public static class AttributedRouteTestHelpers
{
  /// <summary>
  /// Runs the NuruAttributedRouteGenerator on the given source code.
  /// </summary>
  /// <param name="source">C# source code containing [NuruRoute] attributed classes.</param>
  /// <returns>The generator driver run result.</returns>
  public static GeneratorDriverRunResult RunAttributedRouteGenerator(string source)
  {
    CSharpCompilation compilation = CreateCompilationWithNuruAttributes(source);
    return RunGeneratorWithCompilation(compilation);
  }

  /// <summary>
  /// Gets the generated source code from the GeneratedAttributedRoutes.g.cs file.
  /// </summary>
  /// <param name="result">The generator run result.</param>
  /// <returns>The generated source code, or null if not found.</returns>
  public static string? GetGeneratedAttributedRoutesSource(GeneratorDriverRunResult result)
  {
    SyntaxTree? generated = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedAttributedRoutes"));

    return generated?.GetText().ToString();
  }

  /// <summary>
  /// Creates a C# compilation with references to TimeWarp.Nuru and Mediator.
  /// </summary>
  /// <param name="source">The source code to compile.</param>
  /// <returns>A CSharpCompilation ready for generator execution.</returns>
  public static CSharpCompilation CreateCompilationWithNuruAttributes(string source)
  {
    string runtimePath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
    string repoRoot = FindRepoRoot();
    string nuruDir = Path.Combine(repoRoot, "source", "timewarp-nuru", "bin", "Debug", "net10.0");
    string nuruCoreDir = Path.Combine(repoRoot, "source", "timewarp-nuru-core", "bin", "Debug", "net10.0");
    // attributed-routes sample has Mediator.dll for IQuery/ICommand interfaces
    string attributedRoutesDir = Path.Combine(repoRoot, "samples", "attributed-routes", "bin", "Debug", "net10.0");

    List<MetadataReference> references =
    [
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll"))
    ];

    // Add TimeWarp.Nuru assemblies
    AddAssembliesFromDirectory(references, nuruDir);
    AddAssembliesFromDirectory(references, nuruCoreDir);

    // Add Mediator.dll for IQuery<T>/ICommand<T> interface detection
    string mediatorDll = Path.Combine(attributedRoutesDir, "Mediator.dll");
    if (File.Exists(mediatorDll))
    {
      references.Add(MetadataReference.CreateFromFile(mediatorDll));
    }

    return CSharpCompilation.Create(
      "TestAssembly",
      [CSharpSyntaxTree.ParseText(source)],
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  }

  /// <summary>
  /// Runs the source generators on a given compilation.
  /// </summary>
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
      // Try Release build
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

      // Find NuruAttributedRouteGenerator specifically
      Type? generatorType = analyzerAssembly.GetTypes()
        .FirstOrDefault(t => t.Name == "NuruAttributedRouteGenerator" &&
                             !t.IsAbstract &&
                             t.GetInterfaces().Any(i => i.Name == "IIncrementalGenerator"));

      if (generatorType != null)
      {
        var generator = (IIncrementalGenerator)Activator.CreateInstance(generatorType)!;
        generators = [generator.AsSourceGenerator()];
      }
      else
      {
        Console.WriteLine("WARNING: NuruAttributedRouteGenerator not found in analyzer assembly");
        generators = [];
      }
    }
    else
    {
      Console.WriteLine($"WARNING: Analyzer assembly not found at: {analyzerPath}");
      generators = [];
    }

    // Create and run the driver
    GeneratorDriverOptions options = new(
      IncrementalGeneratorOutputKind.None,
      trackIncrementalGeneratorSteps: true);

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators,
      driverOptions: options);

    driver = driver.RunGenerators(compilation);
    return driver.GetRunResult();
  }

  /// <summary>
  /// Finds the repository root by looking for timewarp-nuru.slnx.
  /// </summary>
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

  /// <summary>
  /// Adds all DLL assemblies from a directory to the references list.
  /// </summary>
  private static void AddAssembliesFromDirectory(List<MetadataReference> references, string directory)
  {
    if (!Directory.Exists(directory))
    {
      Console.WriteLine($"WARNING: Directory not found: {directory}");
      return;
    }

    foreach (string dll in Directory.GetFiles(directory, "*.dll"))
    {
      string fileName = Path.GetFileName(dll);
      if (!references.Any(r => r.Display?.Contains(fileName) == true))
      {
        try
        {
          references.Add(MetadataReference.CreateFromFile(dll));
        }
        catch (Exception ex)
        {
          Console.WriteLine($"WARNING: Could not load {fileName}: {ex.Message}");
        }
      }
    }
  }
}
