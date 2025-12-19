#!/usr/bin/dotnet --
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.CodeAnalysis.CSharp

// Integration tests for NuruDelegateCommandGenerator source generator
// These tests verify that Command classes are generated from delegate signatures

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TimeWarp.Jaribu;
using Shouldly;
using static System.Console;
using static TimeWarp.Jaribu.TestRunner;

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
  /// Test that DI parameters are excluded from the Command class.
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
      // ILogger should NOT be included as it's a DI parameter
      content.ShouldNotContain("Logger");
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
  /// Test that AsQuery() does NOT generate Command classes (only AsCommand does).
  /// </summary>
  public static async Task Should_not_generate_for_asquery()
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

    // GeneratedDelegateCommands should NOT be generated for AsQuery
    SyntaxTree? commandsTree = result.GeneratedTrees
      .FirstOrDefault(t => t.FilePath.Contains("GeneratedDelegateCommands"));
    
    commandsTree.ShouldBeNull("GeneratedDelegateCommands.g.cs should NOT be generated for AsQuery()");

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
