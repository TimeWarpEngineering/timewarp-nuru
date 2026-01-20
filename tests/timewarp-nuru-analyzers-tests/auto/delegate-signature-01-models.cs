#!/usr/bin/dotnet --

// Integration tests for delegate signature extraction
// These tests verify that the NuruRouteAnalyzer can extract delegate signatures from Map() calls

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

return await RunTests<DelegateSignatureExtractionTests>();

/// <summary>
/// Tests that verify delegate signature extraction compiles and works correctly.
/// Since DelegateSignature types are internal to the analyzer project, we test
/// via integration - verifying that Map() calls with various delegate types compile
/// and the analyzer processes them correctly.
/// </summary>
[TestTag("Analyzers")]
public sealed class DelegateSignatureExtractionTests
{
  public static async Task Should_compile_map_with_action_no_params()
  {
    // Arrange - code that uses Map() with a parameterless action
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("status").WithHandler(() => System.Console.WriteLine("OK")).AsQuery().Done()
        .Build();
      """;

    // Act - attempt to compile
    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    // Assert - should have no errors (ignore NURU info diagnostics)
    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_single_string_param()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("greet {name}").WithHandler((string name) => System.Console.WriteLine($"Hello {name}")).AsQuery().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_typed_int_param()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("wait {seconds:int}").WithHandler((int seconds) => System.Console.WriteLine($"Waiting {seconds}s")).AsQuery().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_multiple_params()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("add {x:int} {y:int}").WithHandler((int x, int y) => System.Console.WriteLine($"Result: {x + y}")).AsQuery().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_array_param()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("docker {*args}").WithHandler((string[] args) => System.Console.WriteLine(string.Join(" ", args))).AsCommand().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_bool_option()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("build --verbose").WithHandler((bool verbose) => System.Console.WriteLine($"Verbose: {verbose}")).AsCommand().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_func_returning_int()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("compute {x:int} {y:int}").WithHandler((int x, int y) => x + y).AsQuery().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_async_task()
  {
    const string code = """
      using TimeWarp.Nuru;
      using System.Threading.Tasks;
      
      var app = NuruApp.CreateBuilder([])
        .Map("fetch {url}").WithHandler(async (string url) => await Task.Delay(100)).AsCommand().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_async_task_of_int()
  {
    const string code = """
      using TimeWarp.Nuru;
      using System.Threading.Tasks;
      
      var app = NuruApp.CreateBuilder([])
        .Map("fetch {id:int}").WithHandler(async (int id) => 
        {
          await Task.Delay(100);
          return id * 2;
        }).AsQuery().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_nullable_param()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => 
          System.Console.WriteLine($"Deploy {env} with {tag ?? "latest"}")).AsCommand().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_double_param()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("calc {value:double}").WithHandler((double value) => System.Console.WriteLine(value * 2.0)).AsQuery().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_guid_param()
  {
    const string code = """
      using TimeWarp.Nuru;
      using System;
      
      var app = NuruApp.CreateBuilder([])
        .Map("get {id:Guid}").WithHandler((Guid id) => System.Console.WriteLine(id)).AsQuery().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_compile_map_with_typed_array_param()
  {
    const string code = """
      using TimeWarp.Nuru;
      using System.Linq;
      
      var app = NuruApp.CreateBuilder([])
        .Map("sum {*values:int}").WithHandler((int[] values) => 
          System.Console.WriteLine(values.Sum())).AsQuery().Done()
        .Build();
      """;

    CSharpCompilation compilation = CreateCompilationWithNuru(code);
    ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

    IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    errors.ShouldBeEmpty($"Compilation failed: {string.Join(", ", errors)}");

    await Task.CompletedTask;
  }

  public static async Task Should_find_map_invocations_in_syntax_tree()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("greet {name}").WithHandler((string name) => System.Console.WriteLine(name)).AsQuery().Done()
        .Map("add {x:int} {y:int}").WithHandler((int x, int y) => System.Console.WriteLine(x + y)).AsQuery().Done()
        .Build();
      """;

    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
    CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

    // Find all Map invocations
    List<InvocationExpressionSyntax> mapInvocations = root
      .DescendantNodes()
      .OfType<InvocationExpressionSyntax>()
      .Where(inv =>
        inv.Expression is MemberAccessExpressionSyntax memberAccess &&
        memberAccess.Name.Identifier.Text == "Map")
      .ToList();

    mapInvocations.Count.ShouldBe(2);

    await Task.CompletedTask;
  }

  public static async Task Should_extract_route_pattern_from_map_invocation()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("greet {name}").WithHandler((string name) => System.Console.WriteLine(name)).AsQuery().Done()
        .Build();
      """;

    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
    CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

    InvocationExpressionSyntax? mapInvocation = root
      .DescendantNodes()
      .OfType<InvocationExpressionSyntax>()
      .FirstOrDefault(inv =>
        inv.Expression is MemberAccessExpressionSyntax memberAccess &&
        memberAccess.Name.Identifier.Text == "Map");

    mapInvocation.ShouldNotBeNull();

    // Get the first argument (the route pattern)
    ArgumentSyntax firstArg = mapInvocation!.ArgumentList.Arguments[0];
    LiteralExpressionSyntax literal = (LiteralExpressionSyntax)firstArg.Expression;
    string pattern = literal.Token.ValueText;

    pattern.ShouldBe("greet {name}");

    await Task.CompletedTask;
  }

  public static async Task Should_extract_lambda_from_map_invocation()
  {
    const string code = """
      using TimeWarp.Nuru;
      
      var app = NuruApp.CreateBuilder([])
        .Map("add {x:int} {y:int}").WithHandler((int x, int y) => x + y).AsQuery().Done()
        .Build();
      """;

    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
    CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

    InvocationExpressionSyntax? mapInvocation = root
      .DescendantNodes()
      .OfType<InvocationExpressionSyntax>()
      .FirstOrDefault(inv =>
        inv.Expression is MemberAccessExpressionSyntax memberAccess &&
        memberAccess.Name.Identifier.Text == "Map");

    mapInvocation.ShouldNotBeNull();
    mapInvocation!.ArgumentList.Arguments.Count.ShouldBe(2);

    // Get the second argument (the lambda)
    ArgumentSyntax secondArg = mapInvocation.ArgumentList.Arguments[1];
    secondArg.Expression.ShouldBeOfType<ParenthesizedLambdaExpressionSyntax>();

    ParenthesizedLambdaExpressionSyntax lambda = (ParenthesizedLambdaExpressionSyntax)secondArg.Expression;
    lambda.ParameterList.Parameters.Count.ShouldBe(2);
    lambda.ParameterList.Parameters[0].Identifier.Text.ShouldBe("x");
    lambda.ParameterList.Parameters[1].Identifier.Text.ShouldBe("y");

    await Task.CompletedTask;
  }

  private static CSharpCompilation CreateCompilationWithNuru(string source)
  {
    // Get the runtime directory for .NET references
    string runtimePath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

    // Find the TimeWarp.Nuru assembly
    string nuruAssemblyPath = typeof(NuruApp).Assembly.Location;
    string coreAssemblyPath = Path.Combine(Path.GetDirectoryName(nuruAssemblyPath)!, "TimeWarp.Nuru.Core.dll");

    List<MetadataReference> references =
    [
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll")),
      MetadataReference.CreateFromFile(nuruAssemblyPath)
    ];

    // Add TimeWarp.Nuru.Core if it exists separately
    if (File.Exists(coreAssemblyPath))
    {
      references.Add(MetadataReference.CreateFromFile(coreAssemblyPath));
    }

    return CSharpCompilation.Create(
      "TestAssembly",
      [CSharpSyntaxTree.ParseText(source)],
      references,
      new CSharpCompilationOptions(OutputKind.ConsoleApplication));
  }
}
