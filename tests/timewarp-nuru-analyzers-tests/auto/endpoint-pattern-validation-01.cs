#!/usr/bin/dotnet --

// Integration tests for endpoint pattern validation (NURU_A001)
// These tests verify that [NuruRoute] patterns must be single literal identifiers

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

return await RunTests<EndpointPatternValidationTests>();

/// <summary>
/// Tests for endpoint pattern validation through NuruGenerator.
/// Verifies that NURU_A001 is reported for invalid [NuruRoute] patterns.
/// </summary>
[TestTag("Analyzer")]
public sealed class EndpointPatternValidationTests
{
  /// <summary>
  /// Test that NURU_A001 is reported for multiple literals (spaces) in pattern.
  /// </summary>
  public static async Task Should_report_A001_for_multiple_literals()
  {
    const string code = """
      using TimeWarp.Nuru;

      [NuruRoute("work group")]
      public sealed class WorkGroupCommand : ICommand<Unit>
      {
        public sealed class Handler : ICommandHandler<WorkGroupCommand, Unit>
        {
          public ValueTask<Unit> Handle(WorkGroupCommand request, CancellationToken ct) => default;
        }
      }

      public class Program
      {
        public static async Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .DiscoverEndpoints()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    bool hasInvalidPatternError = HasDiagnostic(result, "NURU_A001");

    hasInvalidPatternError.ShouldBeTrue(
      "Should report NURU_A001 for route pattern with multiple literals");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that NURU_A001 is reported for parameters in pattern.
  /// </summary>
  public static async Task Should_report_A001_for_parameters_in_pattern()
  {
    const string code = """
      using TimeWarp.Nuru;

      [NuruRoute("work {duration:int}")]
      public sealed class WorkCommand : ICommand<Unit>
      {
        [Parameter]
        public int Duration { get; set; }

        public sealed class Handler : ICommandHandler<WorkCommand, Unit>
        {
          public ValueTask<Unit> Handle(WorkCommand request, CancellationToken ct) => default;
        }
      }

      public class Program
      {
        public static async Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .DiscoverEndpoints()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    bool hasInvalidPatternError = HasDiagnostic(result, "NURU_A001");

    hasInvalidPatternError.ShouldBeTrue(
      "Should report NURU_A001 for route pattern with parameter placeholders");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that NURU_A001 is reported for options in pattern.
  /// </summary>
  public static async Task Should_report_A001_for_options_in_pattern()
  {
    const string code = """
      using TimeWarp.Nuru;

      [NuruRoute("build --force")]
      public sealed class BuildCommand : ICommand<Unit>
      {
        [Option("force", "f")]
        public bool Force { get; set; }

        public sealed class Handler : ICommandHandler<BuildCommand, Unit>
        {
          public ValueTask<Unit> Handle(BuildCommand request, CancellationToken ct) => default;
        }
      }

      public class Program
      {
        public static async Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .DiscoverEndpoints()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    bool hasInvalidPatternError = HasDiagnostic(result, "NURU_A001");

    hasInvalidPatternError.ShouldBeTrue(
      "Should report NURU_A001 for route pattern with options");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that single literal patterns are accepted.
  /// </summary>
  public static async Task Should_accept_single_literal_pattern()
  {
    const string code = """
      using TimeWarp.Nuru;

      [NuruRoute("work")]
      public sealed class WorkCommand : ICommand<Unit>
      {
        [Parameter]
        public int Duration { get; set; }

        public sealed class Handler : ICommandHandler<WorkCommand, Unit>
        {
          public ValueTask<Unit> Handle(WorkCommand request, CancellationToken ct) => default;
        }
      }

      public class Program
      {
        public static async Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .DiscoverEndpoints()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    bool hasInvalidPatternError = HasDiagnostic(result, "NURU_A001");

    hasInvalidPatternError.ShouldBeFalse(
      "Should NOT report NURU_A001 for valid single literal pattern");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that empty string (root route) is accepted.
  /// </summary>
  public static async Task Should_accept_empty_string_root_route()
  {
    const string code = """
      using TimeWarp.Nuru;

      [NuruRoute("")]
      public sealed class DefaultCommand : ICommand<Unit>
      {
        public sealed class Handler : ICommandHandler<DefaultCommand, Unit>
        {
          public ValueTask<Unit> Handle(DefaultCommand request, CancellationToken ct) => default;
        }
      }

      public class Program
      {
        public static async Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .DiscoverEndpoints()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    bool hasInvalidPatternError = HasDiagnostic(result, "NURU_A001");

    hasInvalidPatternError.ShouldBeFalse(
      "Should NOT report NURU_A001 for empty string root route");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that group patterns with single literal routes are accepted.
  /// </summary>
  public static async Task Should_accept_group_with_single_literal_routes()
  {
    const string code = """
      using TimeWarp.Nuru;

      [NuruRouteGroup("docker")]
      public abstract class DockerGroupBase;

      [NuruRoute("run")]
      public sealed class DockerRunCommand : DockerGroupBase, ICommand<Unit>
      {
        [Parameter]
        public string Image { get; set; } = string.Empty;

        public sealed class Handler : ICommandHandler<DockerRunCommand, Unit>
        {
          public ValueTask<Unit> Handle(DockerRunCommand request, CancellationToken ct) => default;
        }
      }

      public class Program
      {
        public static async Task Main()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .DiscoverEndpoints()
            .Build();
          await app.RunAsync([]);
        }
      }
      """;

    GeneratorDriverRunResult result = RunGenerator(code);

    bool hasInvalidPatternError = HasDiagnostic(result, "NURU_A001");

    hasInvalidPatternError.ShouldBeFalse(
      "Should NOT report NURU_A001 for group pattern with single literal routes");

    await Task.CompletedTask;
  }

  // =========================================================================
  // Helper methods
  // =========================================================================

  private static bool HasDiagnostic(GeneratorDriverRunResult result, string diagnosticId)
  {
    // Check top-level diagnostics
    if (result.Diagnostics.Any(d => d.Id == diagnosticId))
      return true;

    // Check per-generator diagnostics
    return result.Results.Any(r => r.Diagnostics.Any(d => d.Id == diagnosticId));
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

    return CSharpCompilation.Create(
      "TestAssembly",
      [CSharpSyntaxTree.ParseText(source)],
      references,
      new CSharpCompilationOptions(OutputKind.ConsoleApplication));
  }
}
