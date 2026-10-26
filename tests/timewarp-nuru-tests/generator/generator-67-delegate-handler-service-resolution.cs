#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp
#:package Microsoft.Extensions.Http

#region Purpose
// Kanban 541: a delegate handler parameter resolves through the same service path as an endpoint
// handler constructor. ILogger<T> used to compile to `default!` with no diagnostic, so the handler
// threw on its first log call. These tests run the generator in-memory and check the generated
// resolution and diagnostics for each shape a delegate parameter can take.
#endregion

#region Design
// Roslyn-hosted like generator-63, so the CI multi-mode assembly excludes it (CS0433) and it runs
// standalone. Assertions read the generated source for the resolution expression; the end-to-end
// logging write through a delegate is covered by generator-66.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen67DelegateHandlerServiceResolution
{
  using System.Collections.Immutable;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class DelegateHandlerServiceResolutionTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<DelegateHandlerServiceResolutionTests>();

    private sealed record GeneratorRun(
      ImmutableArray<Diagnostic> GeneratorDiagnostics,
      ImmutableArray<Diagnostic> CompileErrors,
      string GeneratedSource);

    private static GeneratorRun Run(string source)
    {
      CSharpParseOptions parseOptions = CSharpParseOptions.Default
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "TimeWarp.Nuru.Generated")]);
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "app.cs");

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        // The analyzers assembly shares parsing types with timewarp-nuru (CS0433); a real app
        // loads it as an analyzer, not as a reference.
        if (Path.GetFileNameWithoutExtension(path) == "TimeWarp.Nuru.Analyzers")
          continue;
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Gen67App",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(
        generators: [new NuruGenerator().AsSourceGenerator()],
        parseOptions: parseOptions);
      _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);

      string generated = string.Join("\n", output.SyntaxTrees.Where(t => t != tree).Select(static t => t.ToString()));
      ImmutableArray<Diagnostic> errors = [.. output.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)];
      return new GeneratorRun(generatorDiagnostics, errors, generated);
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics) =>
      string.Join("\n", diagnostics.Select(static d => d.ToString()));

    public static async Task Should_resolve_delegate_logger_from_add_logging_factory()
    {
      const string Source = """
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Logging;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(services => services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Information)))
          .Map("gen67-log").WithHandler((ILogger<Gen67Category> logger) => "ok").AsCommand().Done()
          .Build();
        return await app.RunAsync([]);

        public sealed class Gen67Category;
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("logger = __loggerFactory.CreateLogger<global::Gen67Category>();");
      run.GeneratedSource.ShouldNotContain("not registered");
      run.GeneratorDiagnostics.Any(static d => d.Id == "NURU_H007").ShouldBeFalse(Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }

    public static async Task Should_use_null_logger_and_warn_without_logging()
    {
      const string Source = """
        using Microsoft.Extensions.Logging;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("gen67-nolog").WithHandler((ILogger<Gen67Category> logger, ILoggerFactory factory) => "ok").AsCommand().Done()
          .Build();
        return await app.RunAsync([]);

        public sealed class Gen67Category;
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("logger = global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<global::Gen67Category>();");
      run.GeneratedSource.ShouldContain("factory = global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;");
      run.GeneratorDiagnostics.Count(static d => d.Id == "NURU_H007").ShouldBe(1, Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }

    public static async Task Should_treat_logger_of_logger_factory_category_as_logger()
    {
      // Review 541 M1: ILogger<ILoggerFactory> ends with "ILoggerFactory" but is a logger, so it gets
      // CreateLogger<T>() and warns like any other category, not the factory itself.
      const string Source = """
        using Microsoft.Extensions.Logging;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("gen67-factory-category").WithHandler((ILogger<ILoggerFactory> logger) => "ok").AsCommand().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("logger = global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<global::Microsoft.Extensions.Logging.ILoggerFactory>();");
      run.GeneratorDiagnostics.Count(static d => d.Id == "NURU_H007").ShouldBe(1, Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }

    public static async Task Should_resolve_logger_of_logger_factory_category_from_add_logging_factory()
    {
      const string Source = """
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Logging;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(services => services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Information)))
          .Map("gen67-factory-category-log").WithHandler((ILogger<ILoggerFactory> logger) => "ok").AsCommand().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("logger = __loggerFactory.CreateLogger<global::Microsoft.Extensions.Logging.ILoggerFactory>();");

      await Task.CompletedTask;
    }

    public static async Task Should_not_warn_when_telemetry_configures_logging()
    {
      // UseTelemetry() logs through app.LoggerFactory, so the logger is not a NullLogger.
      const string Source = """
        using Microsoft.Extensions.Logging;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseTelemetry()
          .Map("gen67-otel").WithHandler((ILogger<Gen67Category> logger) => "ok").AsCommand().Done()
          .Build();
        return await app.RunAsync([]);

        public sealed class Gen67Category;
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("logger = (app.LoggerFactory?.CreateLogger<global::Gen67Category>()");
      run.GeneratorDiagnostics.Any(static d => d.Id == "NURU_H007").ShouldBeFalse(Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }

    public static async Task Should_resolve_delegate_typed_http_client()
    {
      const string Source = """
        using System.Net.Http;
        using Microsoft.Extensions.DependencyInjection;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(services => services.AddHttpClient<IGen67Client, Gen67Client>())
          .Map("gen67-http").WithHandler((IGen67Client client) => client.Name).AsQuery().Done()
          .Build();
        return await app.RunAsync([]);

        public interface IGen67Client
        {
          string Name { get; }
        }

        public sealed class Gen67Client(HttpClient httpClient) : IGen67Client
        {
          public string Name => httpClient.GetType().Name;
        }
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("client = new global::Gen67Client(");
      run.GeneratedSource.ShouldNotContain("not registered");

      await Task.CompletedTask;
    }

    public static async Task Should_report_nuru050_for_unbound_options_monitor()
    {
      const string Source = """
        using Microsoft.Extensions.Options;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("gen67-monitor").WithHandler((IOptionsMonitor<Gen67Options> options) => options.CurrentValue.Name).AsQuery().Done()
          .Build();
        return await app.RunAsync([]);

        public sealed class Gen67Options
        {
          public string Name { get; set; } = "";
        }
        """;

      GeneratorRun run = Run(Source);

      run.GeneratorDiagnostics.Any(static d => d.Id == "NURU050").ShouldBeTrue(Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }

    public static async Task Should_report_nuru050_for_service_named_like_a_logger()
    {
      // A user interface whose name contains "ILogger" is an ordinary service, not a logger.
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("gen67-audit").WithHandler((IAuditILogger audit) => "ok").AsCommand().Done()
          .Build();
        return await app.RunAsync([]);

        public interface IAuditILogger;
        """;

      GeneratorRun run = Run(Source);

      run.GeneratorDiagnostics.Any(static d => d.Id == "NURU050").ShouldBeTrue(Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }
  }
}
