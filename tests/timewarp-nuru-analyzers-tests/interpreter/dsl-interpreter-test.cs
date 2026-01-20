#!/usr/bin/dotnet run

// ═══════════════════════════════════════════════════════════════════════════════
// DSL INTERPRETER TEST - Block-Based Processing
// ═══════════════════════════════════════════════════════════════════════════════
// Tests that the DslInterpreter correctly interprets block-based DSL code.
//
// The interpreter now takes a BlockSyntax (method body) and returns
// IReadOnlyList<AppModel>, supporting multiple apps per block and
// variable tracking for fragmented code styles.
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TimeWarp.Nuru.Generators;

return await RunTests<InterpreterPocTests>();

[TestTag("Interpreter")]
public sealed class InterpreterPocTests
{
  public static async Task Should_interpret_minimal_fluent_case()
  {
    await Task.CompletedTask;

    // Arrange
    string source = """
      using System.Threading.Tasks;
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder([])
            .Map("ping")
              .WithHandler(() => "pong")
              .AsQuery()
              .Done()
            .Build();

          await app.RunAsync(["ping"]);
        }
      }
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    SyntaxTree tree = compilation.SyntaxTrees.First();
    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

    // Find Main method body (BlockSyntax)
    BlockSyntax? mainBlock = FindMainMethodBlock(tree);
    mainBlock.ShouldNotBeNull("Could not find Main method block in source");

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1, "Should have exactly one app");
    AppModel result = results[0];
    result.ShouldNotBeNull();
    result.VariableName.ShouldBe("app", "Variable name should be captured");
    result.Routes.Length.ShouldBe(1, "Should have exactly one route");
    result.Routes[0].OriginalPattern.ShouldBe("ping");
    result.Routes[0].MessageType.ShouldBe("Query");
    result.Routes[0].Handler.ShouldNotBeNull("Handler should be captured");
    result.InterceptSites.Length.ShouldBe(1, "Should have exactly one intercept site");
  }

  public static async Task Should_interpret_route_with_description()
  {
    await Task.CompletedTask;

    // Arrange
    string source = """
      using System.Threading.Tasks;
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder([])
            .Map("status")
              .WithHandler(() => "ok")
              .WithDescription("Returns status")
              .AsQuery()
              .Done()
            .Build();

          await app.RunAsync(["status"]);
        }
      }
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    SyntaxTree tree = compilation.SyntaxTrees.First();
    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

    BlockSyntax? mainBlock = FindMainMethodBlock(tree);
    mainBlock.ShouldNotBeNull();

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1);
    results[0].Routes.Length.ShouldBe(1);
    results[0].Routes[0].Description.ShouldBe("Returns status");
  }

  public static async Task Should_interpret_command_route()
  {
    await Task.CompletedTask;

    // Arrange
    string source = """
      using System.Threading.Tasks;
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder([])
            .Map("deploy")
              .WithHandler(() => "deployed")
              .AsCommand()
              .Done()
            .Build();

          await app.RunAsync(["deploy"]);
        }
      }
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    SyntaxTree tree = compilation.SyntaxTrees.First();
    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

    BlockSyntax? mainBlock = FindMainMethodBlock(tree);
    mainBlock.ShouldNotBeNull();

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1);
    results[0].Routes.Length.ShouldBe(1);
    results[0].Routes[0].MessageType.ShouldBe("Command");
  }

  public static async Task Should_interpret_multiple_routes()
  {
    await Task.CompletedTask;

    // Arrange
    string source = """
      using System.Threading.Tasks;
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder([])
            .Map("ping")
              .WithHandler(() => "pong")
              .AsQuery()
              .Done()
            .Map("status")
              .WithHandler(() => "ok")
              .AsQuery()
              .Done()
            .Build();

          await app.RunAsync(["ping"]);
        }
      }
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    SyntaxTree tree = compilation.SyntaxTrees.First();
    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

    BlockSyntax? mainBlock = FindMainMethodBlock(tree);
    mainBlock.ShouldNotBeNull();

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1);
    results[0].Routes.Length.ShouldBe(2, "Should have two routes");
    results[0].Routes[0].OriginalPattern.ShouldBe("ping");
    results[0].Routes[1].OriginalPattern.ShouldBe("status");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // HELPER METHODS
  // ═══════════════════════════════════════════════════════════════════════════

  private static BlockSyntax? FindMainMethodBlock(SyntaxTree tree)
  {
    return tree.GetRoot()
      .DescendantNodes()
      .OfType<MethodDeclarationSyntax>()
      .FirstOrDefault(m => m.Identifier.Text == "Main")
      ?.Body;
  }

  private static CSharpCompilation CreateCompilationWithNuru(string source)
  {
    string runtimePath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
    string repoRoot = FindRepoRoot();
    string nuruDir = Path.Combine(repoRoot, "source", "timewarp-nuru", "bin", "Debug", "net10.0");
    string nuruCoreDir = Path.Combine(repoRoot, "source", "timewarp-nuru-core", "bin", "Debug", "net10.0");

    List<MetadataReference> references =
    [
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll")),
    ];

    // Add TimeWarp.Nuru assemblies
    AddAssembliesFromDirectory(references, nuruDir);
    AddAssembliesFromDirectory(references, nuruCoreDir);

    return CSharpCompilation.Create(
      "TestAssembly",
      [CSharpSyntaxTree.ParseText(source)],
      references,
      new CSharpCompilationOptions(OutputKind.ConsoleApplication));
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
