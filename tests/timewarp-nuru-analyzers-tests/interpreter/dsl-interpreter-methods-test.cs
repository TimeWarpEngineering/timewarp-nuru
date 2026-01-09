#!/usr/bin/dotnet run

// ═══════════════════════════════════════════════════════════════════════════════
// DSL INTERPRETER METHODS TEST - Phase 4: Additional DSL Methods
// ═══════════════════════════════════════════════════════════════════════════════
// Tests that the DslInterpreter correctly handles additional DSL methods:
// - App-level: WithAiPrompt, AddHelp, AddRepl, AddConfiguration, AddBehavior, UseTerminal
// - Route-level: WithAlias
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TimeWarp.Nuru.Generators;

return await RunTests<InterpreterMethodsTests>();

[TestTag("Interpreter")]
public sealed class InterpreterMethodsTests
{
  // ═══════════════════════════════════════════════════════════════════════════
  // 4.1 APP-LEVEL METADATA: WithAiPrompt
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_WithAiPrompt()
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
            .WithName("my-app")
            .WithDescription("My CLI App")
            .WithAiPrompt("Use queries before commands.")
            .Map("ping")
              .WithHandler(() => "pong")
              .AsQuery()
              .Done()
            .Build();

          await app.RunAsync(["ping"]);
        }
      }
      """;

    (SemanticModel semanticModel, BlockSyntax mainBlock) = CompileAndGetMainBlock(source);

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1);
    AppModel result = results[0];
    result.Name.ShouldBe("my-app");
    result.Description.ShouldBe("My CLI App");
    result.AiPrompt.ShouldBe("Use queries before commands.");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // 4.2 HELP CONFIGURATION: AddHelp
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_AddHelp()
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
            .AddHelp()
            .Map("ping")
              .WithHandler(() => "pong")
              .AsQuery()
              .Done()
            .Build();

          await app.RunAsync(["ping"]);
        }
      }
      """;

    (SemanticModel semanticModel, BlockSyntax mainBlock) = CompileAndGetMainBlock(source);

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1);
    AppModel result = results[0];
    result.HasHelp.ShouldBeTrue("HasHelp should be true");
    result.HelpOptions.ShouldNotBeNull("HelpOptions should be set to defaults");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // 4.3 REPL CONFIGURATION: AddRepl
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_AddRepl()
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
            .AddRepl()
            .Map("ping")
              .WithHandler(() => "pong")
              .AsQuery()
              .Done()
            .Build();

          await app.RunAsync(["ping"]);
        }
      }
      """;

    (SemanticModel semanticModel, BlockSyntax mainBlock) = CompileAndGetMainBlock(source);

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1);
    AppModel result = results[0];
    result.HasRepl.ShouldBeTrue("HasRepl should be true");
    result.ReplOptions.ShouldNotBeNull("ReplOptions should be set to defaults");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // 4.4 CONFIGURATION: AddConfiguration
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_AddConfiguration()
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
            .AddConfiguration()
            .Map("ping")
              .WithHandler(() => "pong")
              .AsQuery()
              .Done()
            .Build();

          await app.RunAsync(["ping"]);
        }
      }
      """;

    (SemanticModel semanticModel, BlockSyntax mainBlock) = CompileAndGetMainBlock(source);

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1);
    AppModel result = results[0];
    result.HasConfiguration.ShouldBeTrue("HasConfiguration should be true");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // 4.6 BEHAVIORS: AddBehavior
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_AddBehavior()
  {
    await Task.CompletedTask;

    // Arrange
    string source = """
      using System.Threading.Tasks;
      using TimeWarp.Nuru;

      public class TelemetryBehavior<TRequest, TResponse> { }

      public class Program
      {
        public static async Task Main()
        {
          NuruCoreApp app = NuruApp.CreateBuilder([])
            .AddBehavior(typeof(TelemetryBehavior<,>))
            .Map("ping")
              .WithHandler(() => "pong")
              .AsQuery()
              .Done()
            .Build();

          await app.RunAsync(["ping"]);
        }
      }
      """;

    (SemanticModel semanticModel, BlockSyntax mainBlock) = CompileAndGetMainBlock(source);

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1);
    AppModel result = results[0];
    result.Behaviors.Length.ShouldBe(1, "Should have one behavior");
    result.Behaviors[0].FullTypeName.ShouldContain("TelemetryBehavior");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // 4.7 TERMINAL: UseTerminal (no-op)
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_UseTerminal_as_noop()
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
            .UseTerminal(null!)
            .Map("ping")
              .WithHandler(() => "pong")
              .AsQuery()
              .Done()
            .Build();

          await app.RunAsync(["ping"]);
        }
      }
      """;

    (SemanticModel semanticModel, BlockSyntax mainBlock) = CompileAndGetMainBlock(source);

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert - UseTerminal should be ignored, app should still work
    results.Count.ShouldBe(1);
    AppModel result = results[0];
    result.Routes.Length.ShouldBe(1, "Should have one route");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // 4.7.1 TYPE CONVERTERS: AddTypeConverter (no-op)
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_AddTypeConverter_as_noop()
  {
    await Task.CompletedTask;

    // Arrange
    string source = """
      using System;
      using System.Threading.Tasks;
      using TimeWarp.Nuru;

      public class MyConverter : IRouteTypeConverter
      {
        public Type TargetType => typeof(string);
        public string ConstraintName => "custom";
        public bool TryConvert(string value, out object? result) { result = value; return true; }
      }

      public class Program
      {
        public static async Task Main()
        {
          NuruCoreApp app = NuruApp.CreateBuilder([])
            .AddTypeConverter(new MyConverter())
            .Map("ping")
              .WithHandler(() => "pong")
              .AsQuery()
              .Done()
            .Build();

          await app.RunAsync(["ping"]);
        }
      }
      """;

    (SemanticModel semanticModel, BlockSyntax mainBlock) = CompileAndGetMainBlock(source);

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert - AddTypeConverter should be ignored, app should still work
    results.Count.ShouldBe(1);
    AppModel result = results[0];
    result.Routes.Length.ShouldBe(1, "Should have one route");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // 4.8 ROUTE-LEVEL: WithAlias
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_WithAlias()
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
              .WithAlias("s")
              .WithAlias("st")
              .AsQuery()
              .Done()
            .Build();

          await app.RunAsync(["status"]);
        }
      }
      """;

    (SemanticModel semanticModel, BlockSyntax mainBlock) = CompileAndGetMainBlock(source);

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1);
    AppModel result = results[0];
    result.Routes.Length.ShouldBe(1, "Should have one route");
    result.Routes[0].Aliases.Length.ShouldBe(2, "Should have two aliases");
    result.Routes[0].Aliases.ShouldContain("s");
    result.Routes[0].Aliases.ShouldContain("st");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // 4.9 FULL DSL EXAMPLE
  // ═══════════════════════════════════════════════════════════════════════════

  public static async Task Should_interpret_full_dsl_example()
  {
    await Task.CompletedTask;

    // Arrange - comprehensive example with all Phase 4 methods
    string source = """
      using System.Threading.Tasks;
      using TimeWarp.Nuru;

      public class TelemetryBehavior<TRequest, TResponse> { }
      public class ValidationBehavior<TRequest, TResponse> { }

      public class Program
      {
        public static async Task Main()
        {
          NuruCoreApp app = NuruApp.CreateBuilder([])
            .AddConfiguration()
            .AddBehavior(typeof(TelemetryBehavior<,>))
            .AddBehavior(typeof(ValidationBehavior<,>))
            .AddHelp()
            .AddRepl()
            .WithName("my-app")
            .WithDescription("Does Cool Things")
            .WithAiPrompt("Use queries before commands.")
            .Map("status")
              .WithHandler(() => "ok")
              .WithDescription("Check application status")
              .WithAlias("s")
              .AsQuery()
              .Done()
            .Map("deploy")
              .WithHandler(() => "deployed")
              .WithDescription("Deploy the application")
              .AsCommand()
              .Done()
            .Build();

          await app.RunAsync(["status"]);
        }
      }
      """;

    (SemanticModel semanticModel, BlockSyntax mainBlock) = CompileAndGetMainBlock(source);

    // Act
    DslInterpreter interpreter = new(semanticModel, CancellationToken.None);
    IReadOnlyList<AppModel> results = interpreter.Interpret(mainBlock);

    // Assert
    results.Count.ShouldBe(1);
    AppModel result = results[0];

    // App metadata
    result.Name.ShouldBe("my-app");
    result.Description.ShouldBe("Does Cool Things");
    result.AiPrompt.ShouldBe("Use queries before commands.");

    // Configuration flags
    result.HasConfiguration.ShouldBeTrue();
    result.HasHelp.ShouldBeTrue();
    result.HasRepl.ShouldBeTrue();

    // Behaviors
    result.Behaviors.Length.ShouldBe(2, "Should have two behaviors");
    result.Behaviors[0].FullTypeName.ShouldContain("TelemetryBehavior");
    result.Behaviors[1].FullTypeName.ShouldContain("ValidationBehavior");

    // Routes
    result.Routes.Length.ShouldBe(2, "Should have two routes");

    RouteDefinition statusRoute = result.Routes[0];
    statusRoute.OriginalPattern.ShouldBe("status");
    statusRoute.Description.ShouldBe("Check application status");
    statusRoute.MessageType.ShouldBe("Query");
    statusRoute.Aliases.Length.ShouldBe(1);
    statusRoute.Aliases.ShouldContain("s");

    RouteDefinition deployRoute = result.Routes[1];
    deployRoute.OriginalPattern.ShouldBe("deploy");
    deployRoute.Description.ShouldBe("Deploy the application");
    deployRoute.MessageType.ShouldBe("Command");
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // HELPER METHODS
  // ═══════════════════════════════════════════════════════════════════════════

  private static (SemanticModel, BlockSyntax) CompileAndGetMainBlock(string source)
  {
    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    SyntaxTree tree = compilation.SyntaxTrees.First();
    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

    BlockSyntax? mainBlock = FindMainMethodBlock(tree);
    if (mainBlock is null)
    {
      throw new InvalidOperationException("Could not find Main method block in source");
    }

    return (semanticModel, mainBlock);
  }

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
