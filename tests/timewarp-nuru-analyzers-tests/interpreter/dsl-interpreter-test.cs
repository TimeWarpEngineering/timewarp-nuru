#!/usr/bin/dotnet run

// ═══════════════════════════════════════════════════════════════════════════════
// DSL INTERPRETER POC TEST
// ═══════════════════════════════════════════════════════════════════════════════
// Tests that the DslInterpreter correctly interprets the minimal fluent case.
//
// Target code under test:
//   NuruCoreApp app = NuruApp.CreateBuilder([])
//     .Map("ping")
//       .WithHandler(() => "pong")
//       .AsQuery()
//       .Done()
//     .Build();
//   await app.RunAsync(["ping"]);
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
          NuruCoreApp app = NuruApp.CreateBuilder([])
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

    // Find CreateBuilder invocation
    InvocationExpressionSyntax? createBuilderCall = tree.GetRoot()
      .DescendantNodes()
      .OfType<InvocationExpressionSyntax>()
      .FirstOrDefault(inv => GetMethodName(inv) == "CreateBuilder");

    createBuilderCall.ShouldNotBeNull("Could not find CreateBuilder() call in source");

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    AppModel result = interpreter.Interpret(createBuilderCall);

    // Assert
    result.ShouldNotBeNull();
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
          NuruCoreApp app = NuruApp.CreateBuilder([])
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

    InvocationExpressionSyntax? createBuilderCall = tree.GetRoot()
      .DescendantNodes()
      .OfType<InvocationExpressionSyntax>()
      .FirstOrDefault(inv => GetMethodName(inv) == "CreateBuilder");

    createBuilderCall.ShouldNotBeNull();

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    AppModel result = interpreter.Interpret(createBuilderCall);

    // Assert
    result.Routes.Length.ShouldBe(1);
    result.Routes[0].Description.ShouldBe("Returns status");
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
          NuruCoreApp app = NuruApp.CreateBuilder([])
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

    InvocationExpressionSyntax? createBuilderCall = tree.GetRoot()
      .DescendantNodes()
      .OfType<InvocationExpressionSyntax>()
      .FirstOrDefault(inv => GetMethodName(inv) == "CreateBuilder");

    createBuilderCall.ShouldNotBeNull();

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    AppModel result = interpreter.Interpret(createBuilderCall);

    // Assert
    result.Routes.Length.ShouldBe(1);
    result.Routes[0].MessageType.ShouldBe("Command");
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
          NuruCoreApp app = NuruApp.CreateBuilder([])
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

    InvocationExpressionSyntax? createBuilderCall = tree.GetRoot()
      .DescendantNodes()
      .OfType<InvocationExpressionSyntax>()
      .FirstOrDefault(inv => GetMethodName(inv) == "CreateBuilder");

    createBuilderCall.ShouldNotBeNull();

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    AppModel result = interpreter.Interpret(createBuilderCall);

    // Assert
    result.Routes.Length.ShouldBe(2, "Should have two routes");
    result.Routes[0].OriginalPattern.ShouldBe("ping");
    result.Routes[1].OriginalPattern.ShouldBe("status");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // HELPER METHODS
  // ═══════════════════════════════════════════════════════════════════════════

  private static string? GetMethodName(InvocationExpressionSyntax invocation)
  {
    return invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
      IdentifierNameSyntax identifier => identifier.Identifier.Text,
      _ => null
    };
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
