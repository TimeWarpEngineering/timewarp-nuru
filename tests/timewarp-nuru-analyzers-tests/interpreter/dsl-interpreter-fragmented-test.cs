#!/usr/bin/dotnet run

// ═══════════════════════════════════════════════════════════════════════════════
// DSL INTERPRETER FRAGMENTED STYLE TESTS
// ═══════════════════════════════════════════════════════════════════════════════
// Tests that the DslInterpreter correctly handles fragmented code styles where
// builders are assigned to variables and chains are split across statements.
//
// Target scenarios:
// - Style 2: Builder in variable
// - Style 3: Fully fragmented (each call on separate line)
// - Style 4: Non-builder code interleaved (Console.WriteLine, etc.)
// - Mixed group and fragmented
// - Multiple apps in one block
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TimeWarp.Nuru.Generators;

return await RunTests<InterpreterFragmentedTests>();

[TestTag("Interpreter")]
public sealed class InterpreterFragmentedTests
{
  // ═══════════════════════════════════════════════════════════════════════════
  // TEST: Style 2 - Builder in variable
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_builder_in_variable()
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
          var builder = NuruApp.CreateBuilder();
          builder.Map("ping").WithHandler(() => "pong").AsQuery().Done();
          builder.Build();
          await builder.RunAsync(["ping"]);
        }
      }
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    SyntaxTree tree = compilation.SyntaxTrees.First();
    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

    BlockSyntax? block = tree.GetRoot()
      .DescendantNodes()
      .OfType<MethodDeclarationSyntax>()
      .First(m => m.Identifier.Text == "Main")
      .Body;

    block.ShouldNotBeNull("Could not find Main method body");

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(block);

    // Assert
    results.Count.ShouldBe(1, "Should have exactly one app");
    AppModel app = results[0];
    app.Routes.Length.ShouldBe(1, "Should have exactly one route");
    app.Routes[0].OriginalPattern.ShouldBe("ping");
    app.Routes[0].MessageType.ShouldBe("Query");
    app.InterceptSites.Length.ShouldBe(1, "Should have one intercept site");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // TEST: Style 3 - Fully fragmented
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_fully_fragmented()
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
          var builder = NuruApp.CreateBuilder();
          var endpoint = builder.Map("ping");
          endpoint.WithHandler(() => "pong");
          endpoint.AsQuery();
          endpoint.Done();
          builder.Build();
          await builder.RunAsync(["ping"]);
        }
      }
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    SyntaxTree tree = compilation.SyntaxTrees.First();
    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

    BlockSyntax? block = tree.GetRoot()
      .DescendantNodes()
      .OfType<MethodDeclarationSyntax>()
      .First(m => m.Identifier.Text == "Main")
      .Body;

    block.ShouldNotBeNull();

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(block);

    // Assert
    results.Count.ShouldBe(1, "Should have exactly one app");
    AppModel app = results[0];
    app.Routes.Length.ShouldBe(1, "Should have exactly one route");
    app.Routes[0].OriginalPattern.ShouldBe("ping");
    app.Routes[0].MessageType.ShouldBe("Query");
    app.Routes[0].Handler.ShouldNotBeNull("Handler should be captured");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // TEST: Style 4 - Non-builder code interleaved
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_ignore_non_builder_code()
  {
    await Task.CompletedTask;

    // Arrange
    string source = """
      using System;
      using System.Threading.Tasks;
      using TimeWarp.Nuru;

      public class Program
      {
        public static async Task Main()
        {
          var builder = NuruApp.CreateBuilder();
          var junk = "Hi mom";
          Console.WriteLine(junk);
          builder.Map("ping").WithHandler(() => "pong").AsQuery().Done();
          builder.Build();
          await builder.RunAsync(["ping"]);
        }
      }
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    SyntaxTree tree = compilation.SyntaxTrees.First();
    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

    BlockSyntax? block = tree.GetRoot()
      .DescendantNodes()
      .OfType<MethodDeclarationSyntax>()
      .First(m => m.Identifier.Text == "Main")
      .Body;

    block.ShouldNotBeNull();

    // Act - should NOT throw even with Console.WriteLine
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(block);

    // Assert
    results.Count.ShouldBe(1, "Should have exactly one app");
    AppModel app = results[0];
    app.Routes.Length.ShouldBe(1, "Should have exactly one route");
    app.Routes[0].OriginalPattern.ShouldBe("ping");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // TEST: Mixed group and fragmented
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_mixed_group_and_fragmented()
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
          var builder = NuruApp.CreateBuilder();
          var admin = builder.WithGroupPrefix("admin");
          admin.Map("status").WithHandler(() => "ok").AsQuery().Done();
          admin.Done();
          builder.Build();
          await builder.RunAsync(["admin", "status"]);
        }
      }
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    SyntaxTree tree = compilation.SyntaxTrees.First();
    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

    BlockSyntax? block = tree.GetRoot()
      .DescendantNodes()
      .OfType<MethodDeclarationSyntax>()
      .First(m => m.Identifier.Text == "Main")
      .Body;

    block.ShouldNotBeNull();

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(block);

    // Assert
    results.Count.ShouldBe(1, "Should have exactly one app");
    AppModel app = results[0];
    app.Routes.Length.ShouldBe(1, "Should have exactly one route");
    app.Routes[0].OriginalPattern.ShouldBe("admin status", "Route should have group prefix");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // TEST: Multiple apps in one block
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_multiple_apps_in_block()
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
          var app1 = NuruApp.CreateBuilder()
            .Map("ping").WithHandler(() => "pong").AsQuery().Done()
            .Build();
          var app2 = NuruApp.CreateBuilder()
            .Map("status").WithHandler(() => "ok").AsQuery().Done()
            .Build();
          await app1.RunAsync(["ping"]);
          await app2.RunAsync(["status"]);
        }
      }
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    SyntaxTree tree = compilation.SyntaxTrees.First();
    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

    BlockSyntax? block = tree.GetRoot()
      .DescendantNodes()
      .OfType<MethodDeclarationSyntax>()
      .First(m => m.Identifier.Text == "Main")
      .Body;

    block.ShouldNotBeNull();

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(block);

    // Assert
    results.Count.ShouldBe(2, "Should have two apps");

    // Find app1 (has "ping" route)
    AppModel? pingApp = results.FirstOrDefault(a => a.Routes.Any(r => r.OriginalPattern == "ping"));
    pingApp.ShouldNotBeNull("Should have app with 'ping' route");
    pingApp.Routes.Length.ShouldBe(1);
    pingApp.InterceptSites.Length.ShouldBe(1, "ping app should have one intercept site");

    // Find app2 (has "status" route)
    AppModel? statusApp = results.FirstOrDefault(a => a.Routes.Any(r => r.OriginalPattern == "status"));
    statusApp.ShouldNotBeNull("Should have app with 'status' route");
    statusApp.Routes.Length.ShouldBe(1);
    statusApp.InterceptSites.Length.ShouldBe(1, "status app should have one intercept site");

    // Verify they are different apps
    pingApp.ShouldNotBe(statusApp, "Should be different app instances");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // HELPER METHODS
  // ═══════════════════════════════════════════════════════════════════════════

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
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll")),
    ];

    // Add TimeWarp.Nuru assembly
    AddAssembliesFromDirectory(references, nuruDir);

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
