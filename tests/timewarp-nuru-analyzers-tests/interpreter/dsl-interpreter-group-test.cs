#!/usr/bin/dotnet run

// ═══════════════════════════════════════════════════════════════════════════════
// DSL INTERPRETER GROUP TESTS - Block-Based Processing
// ═══════════════════════════════════════════════════════════════════════════════
// Tests that the DslInterpreter correctly interprets nested route groups.
//
// Target scenarios:
// - Simple group (one level): .WithGroupPrefix("admin").Map("status")
// - Nested groups (two levels): .WithGroupPrefix("admin").WithGroupPrefix("config")
// - Route in outer group after nested group
// - Multiple routes in same group
// - Three levels of nesting
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TimeWarp.Nuru.Generators;

return await RunTests<InterpreterGroupTests>();

[TestTag("Interpreter")]
public sealed class InterpreterGroupTests
{
  // ═══════════════════════════════════════════════════════════════════════════
  // TEST: Simple group (one level)
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_simple_group()
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
            .WithGroupPrefix("admin")
              .Map("status")
                .WithHandler(() => "admin status")
                .AsQuery()
                .Done()
              .Done()
            .Build();

          await app.RunAsync(["admin", "status"]);
        }
      }
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    SyntaxTree tree = compilation.SyntaxTrees.First();
    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

    BlockSyntax? mainBlock = FindMainMethodBlock(tree);
    mainBlock.ShouldNotBeNull("Could not find Main method block in source");

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1, "Should have exactly one app");
    AppModel result = results[0];
    result.ShouldNotBeNull();
    result.Routes.Length.ShouldBe(1, "Should have exactly one route");
    result.Routes[0].OriginalPattern.ShouldBe("admin status", "Route should have group prefix prepended");
    result.Routes[0].MessageType.ShouldBe("Query");
    result.Routes[0].Handler.ShouldNotBeNull("Handler should be captured");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // TEST: Nested groups (two levels)
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_nested_groups()
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
            .WithGroupPrefix("admin")
              .WithGroupPrefix("config")
                .Map("get {key}")
                  .WithHandler((string key) => $"value: {key}")
                  .AsQuery()
                  .Done()
                .Done()
              .Done()
            .Build();

          await app.RunAsync(["admin", "config", "get", "foo"]);
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
    results[0].Routes.Length.ShouldBe(1, "Should have exactly one route");
    results[0].Routes[0].OriginalPattern.ShouldBe("admin config get {key}", "Route should have nested prefixes");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // TEST: Route in outer group after nested group
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_route_after_nested_group()
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
            .WithGroupPrefix("admin")
              .WithGroupPrefix("config")
                .Map("list")
                  .WithHandler(() => "config list")
                  .AsQuery()
                  .Done()
                .Done()
              .Map("status")
                .WithHandler(() => "admin status")
                .AsQuery()
                .Done()
              .Done()
            .Build();

          await app.RunAsync(["admin", "status"]);
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
    results[0].Routes[0].OriginalPattern.ShouldBe("admin config list", "First route should be in nested group");
    results[0].Routes[1].OriginalPattern.ShouldBe("admin status", "Second route should be in outer group only");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // TEST: Multiple routes in same group
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_multiple_routes_in_group()
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
            .WithGroupPrefix("admin")
              .Map("status")
                .WithHandler(() => "status")
                .AsQuery()
                .Done()
              .Map("info")
                .WithHandler(() => "info")
                .AsQuery()
                .Done()
              .Done()
            .Build();

          await app.RunAsync(["admin", "status"]);
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
    results[0].Routes[0].OriginalPattern.ShouldBe("admin status");
    results[0].Routes[1].OriginalPattern.ShouldBe("admin info");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // TEST: Three levels of nesting
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_three_levels_of_nesting()
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
            .WithGroupPrefix("admin")
              .WithGroupPrefix("config")
                .WithGroupPrefix("db")
                  .Map("status")
                    .WithHandler(() => "db status")
                    .AsQuery()
                    .Done()
                  .Done()
                .Done()
              .Done()
            .Build();

          await app.RunAsync(["admin", "config", "db", "status"]);
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
    results[0].Routes.Length.ShouldBe(1, "Should have exactly one route");
    results[0].Routes[0].OriginalPattern.ShouldBe("admin config db status", "Route should have all three prefixes");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // TEST: Mixed top-level and grouped routes
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_mixed_toplevel_and_grouped_routes()
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
            .WithGroupPrefix("admin")
              .Map("status")
                .WithHandler(() => "admin status")
                .AsQuery()
                .Done()
              .Done()
            .Map("version")
              .WithHandler(() => "1.0.0")
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
    results[0].Routes.Length.ShouldBe(3, "Should have three routes");
    results[0].Routes[0].OriginalPattern.ShouldBe("ping", "First route is top-level");
    results[0].Routes[1].OriginalPattern.ShouldBe("admin status", "Second route is grouped");
    results[0].Routes[2].OriginalPattern.ShouldBe("version", "Third route is top-level");
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
