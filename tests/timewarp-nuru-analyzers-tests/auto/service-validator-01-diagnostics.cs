#!/usr/bin/dotnet --

// Integration tests for service validation diagnostics (NURU050-054)
// These tests verify that appropriate diagnostics are reported for unsupported DI patterns

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

return await RunTests<ServiceValidatorDiagnosticTests>();

/// <summary>
/// Tests for service validation diagnostics.
/// Verifies that NURU050, NURU051, NURU052, NURU053, NURU054 are reported correctly.
/// </summary>
[TestTag("Analyzer")]
[TestTag("DI")]
[TestTag("Task393")]
public sealed class ServiceValidatorDiagnosticTests
{
  /// <summary>
  /// Test that NURU050 is reported when handler requires an unregistered service.
  /// </summary>
  public static async Task Should_report_050_for_unregistered_service()
  {
    const string code = """
      using TimeWarp.Nuru;

      public interface IMyService
      {
        string GetValue();
      }

      public class Program
      {
        public static async System.Threading.Tasks.Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .Map("test").WithHandler((IMyService svc) => svc.GetValue()).AsQuery().Done()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = RunGenerator(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id == "NURU050").ShouldBeTrue(
      "Should report NURU050 for handler requiring unregistered service");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that NURU051 is reported when service has constructor dependencies.
  /// </summary>
  public static async Task Should_report_051_for_service_with_constructor_dependencies()
  {
    const string code = """
      using TimeWarp.Nuru;

      public interface IMyService
      {
        string GetValue();
      }

      public interface IDependency
      {
        void DoSomething();
      }

      public class MyService : IMyService
      {
        private readonly IDependency _dep;
        public MyService(IDependency dep) => _dep = dep;
        public string GetValue() => "value";
      }

      public class Program
      {
        public static async System.Threading.Tasks.Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .ConfigureServices(s => s.AddSingleton<IMyService, MyService>())
            .Map("test").WithHandler((IMyService svc) => svc.GetValue()).AsQuery().Done()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = RunGenerator(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id == "NURU051").ShouldBeTrue(
      "Should report NURU051 for service with constructor dependencies");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that NURU052 is reported for extension method calls like AddLogging().
  /// </summary>
  public static async Task Should_report_052_for_extension_method()
  {
    const string code = """
      using TimeWarp.Nuru;
      using Microsoft.Extensions.DependencyInjection;

      public class Program
      {
        public static async System.Threading.Tasks.Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .ConfigureServices(s => s.AddLogging())
            .Map("test").WithHandler(() => "result").AsQuery().Done()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = RunGenerator(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id == "NURU052").ShouldBeTrue(
      "Should report NURU052 for extension method call");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that NURU053 is reported for factory delegate registrations.
  /// </summary>
  public static async Task Should_report_053_for_factory_delegate()
  {
    const string code = """
      using TimeWarp.Nuru;

      public interface IMyService
      {
        string GetValue();
      }

      public class MyService : IMyService
      {
        public string GetValue() => "value";
      }

      public class Program
      {
        public static async System.Threading.Tasks.Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .ConfigureServices(s => s.AddSingleton<IMyService>(sp => new MyService()))
            .Map("test").WithHandler((IMyService svc) => svc.GetValue()).AsQuery().Done()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = RunGenerator(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id == "NURU053").ShouldBeTrue(
      "Should report NURU053 for factory delegate registration");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that NURU054 is reported for internal implementation types.
  /// </summary>
  public static async Task Should_report_054_for_internal_type()
  {
    const string code = """
      using TimeWarp.Nuru;

      public interface IMyService
      {
        string GetValue();
      }

      internal class MyInternalService : IMyService
      {
        public string GetValue() => "value";
      }

      public class Program
      {
        public static async System.Threading.Tasks.Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .ConfigureServices(s => s.AddSingleton<IMyService, MyInternalService>())
            .Map("test").WithHandler((IMyService svc) => svc.GetValue()).AsQuery().Done()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = RunGenerator(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id == "NURU054").ShouldBeTrue(
      "Should report NURU054 for internal implementation type");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that no service diagnostics are reported when UseMicrosoftDependencyInjection is enabled.
  /// </summary>
  public static async Task Should_skip_validation_with_runtime_di()
  {
    const string code = """
      using TimeWarp.Nuru;

      public interface IMyService
      {
        string GetValue();
      }

      public interface IDependency
      {
        void DoSomething();
      }

      public class MyService : IMyService
      {
        private readonly IDependency _dep;
        public MyService(IDependency dep) => _dep = dep;
        public string GetValue() => "value";
      }

      public class Program
      {
        public static async System.Threading.Tasks.Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .UseMicrosoftDependencyInjection()
            .ConfigureServices(s => s.AddSingleton<IMyService, MyService>())
            .Map("test").WithHandler((IMyService svc) => svc.GetValue()).AsQuery().Done()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = RunGenerator(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id.StartsWith("NURU05")).ShouldBeFalse(
      "Should not report any NURU05x diagnostics when UseMicrosoftDependencyInjection is enabled");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that no diagnostics are reported for properly registered services.
  /// </summary>
  public static async Task Should_not_report_for_valid_registration()
  {
    const string code = """
      using TimeWarp.Nuru;

      public interface IMyService
      {
        string GetValue();
      }

      public class MyService : IMyService
      {
        public string GetValue() => "value";
      }

      public class Program
      {
        public static async System.Threading.Tasks.Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .ConfigureServices(s => s.AddSingleton<IMyService, MyService>())
            .Map("test").WithHandler((IMyService svc) => svc.GetValue()).AsQuery().Done()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    ImmutableArray<Diagnostic> diagnostics = RunGenerator(code);

    WriteLine($"Found {diagnostics.Length} diagnostics:");
    foreach (Diagnostic d in diagnostics)
    {
      WriteLine($"  {d.Id}: {d.GetMessage()}");
    }

    diagnostics.Any(d => d.Id.StartsWith("NURU05")).ShouldBeFalse(
      "Should not report any NURU05x diagnostics for valid service registration");

    await Task.CompletedTask;
  }

  private static ImmutableArray<Diagnostic> RunGenerator(string source)
  {
    CSharpCompilation compilation = CreateCompilationWithNuru(source);
    return RunGeneratorWithCompilation(compilation);
  }

  private static ImmutableArray<Diagnostic> RunGeneratorWithCompilation(CSharpCompilation compilation)
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

      // Find all IIncrementalGenerator types
      Type[] generatorTypes = analyzerAssembly.GetTypes()
        .Where(t => !t.IsAbstract && t.GetInterfaces().Any(i => i.Name == "IIncrementalGenerator"))
        .ToArray();

      generators = generatorTypes
        .Select(t => (IIncrementalGenerator)Activator.CreateInstance(t)!)
        .Select(g => g.AsSourceGenerator())
        .ToArray();
    }
    else
    {
      generators = [];
    }

    GeneratorDriverOptions options = new(
      IncrementalGeneratorOutputKind.None,
      trackIncrementalGeneratorSteps: true);

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators,
      driverOptions: options);

    driver = driver.RunGenerators(compilation);
    GeneratorDriverRunResult result = driver.GetRunResult();

    return result.Diagnostics;
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

    if (!Directory.Exists(nuruDir))
    {
      nuruDir = Path.Combine(repoRoot, "source", "timewarp-nuru", "bin", "Release", "net10.0");
    }

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

    return CSharpCompilation.Create(
      "TestAssembly",
      [CSharpSyntaxTree.ParseText(source)],
      references,
      new CSharpCompilationOptions(OutputKind.ConsoleApplication));
  }
}
