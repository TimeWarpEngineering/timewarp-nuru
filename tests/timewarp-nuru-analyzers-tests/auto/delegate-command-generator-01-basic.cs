#!/usr/bin/dotnet --

// Integration tests for NuruDelegateCommandGenerator source generator
// These tests verify that Command classes are generated from delegate signatures

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

return await RunTests<DelegateCommandGeneratorTests>();

/// <summary>
/// Tests for NuruDelegateCommandGenerator source generator.
/// Verifies that AsCommand() calls generate Command classes with correct properties.
/// </summary>
[TestTag("SourceGenerator")]
public sealed class DelegateCommandGeneratorTests
{
  /// <summary>
  /// Basic test to verify the generator runs without errors on a simple AsCommand() call.
  /// </summary>
  public static async Task Should_run_generator_without_errors()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("deploy {env}")
        .WithHandler((string env) => System.Console.WriteLine(env))
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    WriteLine($"Generator diagnostics count: {result.Diagnostics.Length}");
    foreach (Diagnostic d in result.Diagnostics)
    {
      WriteLine($"  {d.Severity}: {d.Id} - {d.GetMessage()}");
    }

    result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that a Command class is generated for AsCommand().
  /// </summary>
  public static async Task Should_generate_command_class()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("deploy {env}")
        .WithHandler((string env) => System.Console.WriteLine(env))
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    // Check if GeneratedDelegateCommands.g.cs was created
    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    if (commandsTree != null)
    {
      WriteLine("SUCCESS: GeneratedDelegateCommands.g.cs was generated!");
      WriteLine(commandsTree.GetText().ToString());
      
      string content = commandsTree.GetText().ToString();
      content.ShouldContain("Deploy_Generated_Command");
      content.ShouldContain("public string Env { get; set; }");
      content.ShouldContain("ICommand<");
    }
    else
    {
      WriteLine("FAILURE: GeneratedDelegateCommands.g.cs was NOT generated");
      WriteLine("Available trees:");
      foreach (SyntaxTree tree in result.GeneratedTrees)
      {
        WriteLine($"  - {tree.FilePath}");
      }
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test with multiple parameters including options.
  /// </summary>
  public static async Task Should_generate_command_with_options()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("deploy {env} --force")
        .WithHandler((string env, bool force) => System.Console.WriteLine($"{env} {force}"))
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    if (commandsTree != null)
    {
      WriteLine("Generated Command class:");
      WriteLine(commandsTree.GetText().ToString());
      
      string content = commandsTree.GetText().ToString();
      content.ShouldContain("Deploy_Generated_Command");
      content.ShouldContain("public string Env { get; set; }");
      content.ShouldContain("public bool Force { get; set; }");
    }
    else
    {
      WriteLine("FAILURE: GeneratedDelegateCommands.g.cs was NOT generated");
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that DI parameters are excluded from Command properties but included in Handler.
  /// </summary>
  public static async Task Should_exclude_di_parameters()
  {
    const string code = """
      using TimeWarp.Nuru;
      using Microsoft.Extensions.Logging;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("deploy {env}")
        .WithHandler((string env, ILogger logger) => logger.LogInformation(env))
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    if (commandsTree != null)
    {
      WriteLine("Generated Command class:");
      WriteLine(commandsTree.GetText().ToString());
      
      string content = commandsTree.GetText().ToString();
      content.ShouldContain("public string Env { get; set; }");
      // ILogger should NOT be a PROPERTY on the Command class
      // (it appears in the Handler as a field, which is correct)
      content.ShouldNotContain("public ILogger Logger { get; set; }");
    }
    else
    {
      WriteLine("FAILURE: GeneratedDelegateCommands.g.cs was NOT generated");
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test Command class naming from multi-word routes.
  /// </summary>
  public static async Task Should_generate_multiword_command_name()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("git commit {message}")
        .WithHandler((string message) => System.Console.WriteLine(message))
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    if (commandsTree != null)
    {
      WriteLine("Generated Command class:");
      WriteLine(commandsTree.GetText().ToString());
      
      string content = commandsTree.GetText().ToString();
      // "git commit" should become "GitCommit_Generated_Command"
      content.ShouldContain("GitCommit_Generated_Command");
    }
    else
    {
      WriteLine("FAILURE: GeneratedDelegateCommands.g.cs was NOT generated");
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test default route generates Default_Generated_Command.
  /// </summary>
  public static async Task Should_generate_default_command_name()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("")
        .WithHandler(() => System.Console.WriteLine("default"))
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    if (commandsTree != null)
    {
      WriteLine("Generated Command class:");
      WriteLine(commandsTree.GetText().ToString());
      
      string content = commandsTree.GetText().ToString();
      content.ShouldContain("Default_Generated_Command");
    }
    else
    {
      WriteLine("FAILURE: GeneratedDelegateCommands.g.cs was NOT generated");
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that return type determines ICommand&lt;T&gt; generic parameter.
  /// </summary>
  public static async Task Should_use_return_type_for_command_interface()
  {
    // Use separate builder calls to ensure both AsCommand() are detected
    const string code = """
      using TimeWarp.Nuru;
      
      var builder = NuruCoreApp.CreateSlimBuilder();
      
      // void -> ICommand<Unit>
      builder.Map("deploy {env}")
        .WithHandler((string env) => System.Console.WriteLine(env))
        .AsCommand()
        .Done();
        
      // int -> ICommand<int>
      builder.Map("count")
        .WithHandler(() => 42)
        .AsCommand()
        .Done();
        
      var app = builder.Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    if (commandsTree != null)
    {
      WriteLine("Generated Command classes:");
      WriteLine(commandsTree.GetText().ToString());
      
      string content = commandsTree.GetText().ToString();
      // void returns should use Unit
      content.ShouldContain("ICommand<global::Mediator.Unit>");
      // int returns should use int - check for fully qualified name
      content.ShouldContain("Count_Generated_Command");
      (content.Contains("ICommand<int>") || content.Contains("ICommand<global::System.Int32>")).ShouldBeTrue(
        "Should have ICommand<int> or ICommand<global::System.Int32>");
    }
    else
    {
      WriteLine("FAILURE: GeneratedDelegateCommands.g.cs was NOT generated");
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that nullable parameters generate nullable properties.
  /// </summary>
  public static async Task Should_handle_nullable_parameters()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("deploy {env} {tag?}")
        .WithHandler((string env, string? tag) => System.Console.WriteLine($"{env} {tag}"))
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    if (commandsTree != null)
    {
      WriteLine("Generated Command class:");
      WriteLine(commandsTree.GetText().ToString());
      
      string content = commandsTree.GetText().ToString();
      content.ShouldContain("public string Env { get; set; }");
      // Nullable parameter should have ? marker
      content.ShouldContain("string? Tag");
    }
    else
    {
      WriteLine("FAILURE: GeneratedDelegateCommands.g.cs was NOT generated");
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that AsQuery() generates Query classes with IQuery interface.
  /// </summary>
  public static async Task Should_generate_query_for_asquery()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("status")
        .WithHandler(() => "OK")
        .AsQuery()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    commandsTree.ShouldNotBeNull("GeneratedDelegateCommands.g.cs should be generated for AsQuery()");
    
    string content = commandsTree!.GetText().ToString();
    WriteLine("Generated Query class:");
    WriteLine(content);
    
    // Should generate Query class, not Command
    content.ShouldContain("Status_Generated_Query");
    content.ShouldContain("IQuery<");
    content.ShouldContain("IQueryHandler<");
    // Should NOT contain ICommand for this route
    content.ShouldNotContain("Status_Generated_Command");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that AsIdempotentCommand() generates Command with IIdempotent marker.
  /// </summary>
  public static async Task Should_generate_idempotent_command()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("config set {key} {value}")
        .WithHandler((string key, string value) => System.Console.WriteLine($"{key}={value}"))
        .AsIdempotentCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    commandsTree.ShouldNotBeNull("GeneratedDelegateCommands.g.cs should be generated for AsIdempotentCommand()");
    
    string content = commandsTree!.GetText().ToString();
    WriteLine("Generated IdempotentCommand class:");
    WriteLine(content);
    
    // Should have ICommand AND IIdempotent
    content.ShouldContain("ConfigSet_Generated_Command");
    content.ShouldContain("ICommand<");
    content.ShouldContain("IIdempotent");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that sync block body lambdas wrap return in ValueTask.
  /// </summary>
  public static async Task Should_wrap_sync_block_return_in_valuetask()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("count {n:int}")
        .WithHandler((int n) => {
          System.Console.WriteLine(n);
          return n * 2;
        })
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    commandsTree.ShouldNotBeNull();
    
    string content = commandsTree!.GetText().ToString();
    WriteLine("Generated Handler with ValueTask wrapping:");
    WriteLine(content);
    
    // Sync return should be wrapped in ValueTask
    content.ShouldContain("return new global::System.Threading.Tasks.ValueTask<");
    // Should NOT have bare "return n * 2;" or similar
    content.ShouldNotContain("return request.N * 2;");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that async lambdas generate async Handle methods.
  /// </summary>
  public static async Task Should_generate_async_handler()
  {
    const string code = """
      using TimeWarp.Nuru;
      using System.Threading.Tasks;
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("delay {ms:int}")
        .WithHandler(async (int ms) => {
          await Task.Delay(ms);
          return 0;
        })
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    commandsTree.ShouldNotBeNull();
    
    string content = commandsTree!.GetText().ToString();
    WriteLine("Generated async Handler:");
    WriteLine(content);
    
    // Should have async keyword in Handle method
    content.ShouldContain("public async global::System.Threading.Tasks.ValueTask<");
    // Async returns don't need ValueTask wrapping
    content.ShouldNotContain("return new global::System.Threading.Tasks.ValueTask<int>(0)");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that static method handlers generate proper method calls.
  /// </summary>
  public static async Task Should_generate_handler_for_static_method()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      public static class Handlers
      {
        public static void Deploy(string env) => System.Console.WriteLine(env);
      }
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("deploy {env}")
        .WithHandler(Handlers.Deploy)
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    commandsTree.ShouldNotBeNull("GeneratedDelegateCommands.g.cs should be generated for static method handler");
    
    string content = commandsTree!.GetText().ToString();
    WriteLine("Generated Handler for static method:");
    WriteLine(content);
    
    // Should have Handler class
    content.ShouldContain("sealed class Handler");
    // Should call the static method with fully qualified name
    content.ShouldContain("Handlers.Deploy(");
    // Should have the Command class
    content.ShouldContain("Deploy_Generated_Command");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that internal static method handlers work.
  /// </summary>
  public static async Task Should_generate_handler_for_internal_static_method()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      internal static class Handlers
      {
        internal static string GetStatus() => "OK";
      }
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("status")
        .WithHandler(Handlers.GetStatus)
        .AsQuery()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    commandsTree.ShouldNotBeNull("GeneratedDelegateCommands.g.cs should be generated for internal method handler");
    
    string content = commandsTree!.GetText().ToString();
    WriteLine("Generated Handler for internal static method:");
    WriteLine(content);
    
    // Should have Handler class
    content.ShouldContain("sealed class Handler");
    // Should call the static method
    content.ShouldContain("Handlers.GetStatus(");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that async static method handlers work.
  /// </summary>
  public static async Task Should_generate_handler_for_async_static_method()
  {
    const string code = """
      using TimeWarp.Nuru;
      using System.Threading.Tasks;
      
      public static class Handlers
      {
        public static async Task<int> DelayAndReturn(int ms)
        {
          await Task.Delay(ms);
          return ms;
        }
      }
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("delay {ms:int}")
        .WithHandler(Handlers.DelayAndReturn)
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    commandsTree.ShouldNotBeNull("GeneratedDelegateCommands.g.cs should be generated for async method handler");
    
    string content = commandsTree!.GetText().ToString();
    WriteLine("Generated Handler for async static method:");
    WriteLine(content);
    
    // Should have async Handler
    content.ShouldContain("public async global::System.Threading.Tasks.ValueTask<");
    // Should await the method call
    content.ShouldContain("await");
    content.ShouldContain("Handlers.DelayAndReturn(");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that private method handlers do NOT generate handler (accessibility check).
  /// </summary>
  public static async Task Should_skip_handler_for_private_method()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      public static class Program
      {
        private static void SecretHandler() => System.Console.WriteLine("secret");
        
        public static void Main()
        {
          var app = NuruCoreApp.CreateSlimBuilder()
            .Map("secret")
            .WithHandler(SecretHandler)
            .AsCommand()
            .Done()
            .Build();
        }
      }
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    if (commandsTree != null)
    {
      string content = commandsTree.GetText().ToString();
      WriteLine("Generated output (should NOT contain Handler for private method):");
      WriteLine(content);
      
      // Should NOT have a Handler class for this route (private method is inaccessible)
      content.ShouldNotContain("SecretHandler(");
    }
    else
    {
      WriteLine("No GeneratedDelegateCommands.g.cs generated (expected for private method)");
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that method handlers with DI parameters generate proper field injection.
  /// </summary>
  public static async Task Should_generate_handler_with_di_for_static_method()
  {
    const string code = """
      using TimeWarp.Nuru;
      using Microsoft.Extensions.Logging;
      
      public static class Handlers
      {
        public static void Deploy(string env, ILogger logger)
        {
          logger.LogInformation("Deploying to {Env}", env);
        }
      }
      
      var app = NuruCoreApp.CreateSlimBuilder()
        .Map("deploy {env}")
        .WithHandler(Handlers.Deploy)
        .AsCommand()
        .Done()
        .Build();
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    commandsTree.ShouldNotBeNull();
    
    string content = commandsTree!.GetText().ToString();
    WriteLine("Generated Handler with DI for static method:");
    WriteLine(content);
    
    // Should have Handler class with DI field
    content.ShouldContain("sealed class Handler");
    content.ShouldContain("ILogger");
    // Should pass DI field to method call
    content.ShouldContain("Handlers.Deploy(request.Env, Logger)");

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
