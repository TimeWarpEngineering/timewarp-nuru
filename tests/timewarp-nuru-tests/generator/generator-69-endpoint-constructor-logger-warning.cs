#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Kanban 545: an endpoint handler constructor that injects ILogger<T> gets a NullLogger when the app
// configures no logging, and NURU_H007 did not warn for it (541 covered only behaviors and delegate
// handlers). nuru search lost every warning and error that way. A registered service's constructor
// logger was worse: it was a NullLogger even with AddLogging, and never warned. These tests check that
// H007 fires for endpoint and service constructor loggers without AddLogging, stays quiet once logging
// is configured, and that a service logger then comes from the AddLogging factory.
#endregion

#region Design
// Roslyn-hosted like generator-67, so the CI multi-mode assembly excludes it (CS0433) and it runs
// standalone. Assertions read the generator diagnostics and the generated resolution expression.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen69EndpointConstructorLoggerWarning
{
  using System.Collections.Immutable;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class EndpointConstructorLoggerWarningTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<EndpointConstructorLoggerWarningTests>();

    private sealed record GeneratorRun(
      ImmutableArray<Diagnostic> GeneratorDiagnostics,
      ImmutableArray<Diagnostic> CompileErrors,
      string GeneratedSource);

    private const string Endpoint = """
      [NuruRoute("gen69-work")]
      public sealed class Gen69WorkCommand : ICommand<Unit>
      {
        public sealed class Handler(ILogger<Gen69WorkCommand> logger) : ICommandHandler<Gen69WorkCommand, Unit>
        {
          public Task<Unit> Handle(Gen69WorkCommand request, CancellationToken cancellationToken)
          {
            _ = logger;
            return Task.FromResult(Unit.Value);
          }
        }
      }
      """;

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
        assemblyName: "Gen69App",
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

    public static async Task Should_warn_for_endpoint_constructor_logger_without_logging()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.Logging;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map<Gen69WorkCommand>()
          .Build();
        return await app.RunAsync([]);

        """ + Endpoint;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("NullLoggerFactory.Instance.CreateLogger<global::Gen69WorkCommand>()");
      Diagnostic[] h007 = [.. run.GeneratorDiagnostics.Where(static d => d.Id == "NURU_H007")];
      h007.Length.ShouldBe(1, Describe(run.GeneratorDiagnostics));
      h007[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("Gen69WorkCommand");

      await Task.CompletedTask;
    }

    public static async Task Should_not_warn_for_endpoint_constructor_logger_with_add_logging()
    {
      const string Source = """
        using Microsoft.Extensions.DependencyInjection;
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.Logging;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(services => services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning)))
          .Map<Gen69WorkCommand>()
          .Build();
        return await app.RunAsync([]);

        """ + Endpoint;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("__loggerFactory.CreateLogger<global::Gen69WorkCommand>()");
      run.GeneratorDiagnostics.Any(static d => d.Id == "NURU_H007").ShouldBeFalse(Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }

    private const string Service = """
      public sealed class Gen69Service(ILogger<Gen69Service> logger)
      {
        public ILogger Logger { get; } = logger;
      }

      [NuruRoute("gen69-service")]
      public sealed class Gen69ServiceQuery : IQuery<Unit>
      {
        public sealed class Handler(Gen69Service service) : IQueryHandler<Gen69ServiceQuery, Unit>
        {
          public Task<Unit> Handle(Gen69ServiceQuery request, CancellationToken cancellationToken)
          {
            _ = service;
            return Task.FromResult(Unit.Value);
          }
        }
      }
      """;

    public static async Task Should_warn_for_service_constructor_logger_without_logging()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Logging;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(services => services.AddSingleton<Gen69Service>())
          .Map<Gen69ServiceQuery>()
          .Build();
        return await app.RunAsync([]);

        """ + Service;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      Diagnostic[] h007 = [.. run.GeneratorDiagnostics.Where(static d => d.Id == "NURU_H007")];
      h007.Length.ShouldBe(1, Describe(run.GeneratorDiagnostics));
      h007[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("Gen69Service");

      await Task.CompletedTask;
    }

    public static async Task Should_resolve_service_constructor_logger_from_add_logging_factory()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Logging;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(services =>
          {
            services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
            services.AddSingleton<Gen69Service>();
          })
          .Map<Gen69ServiceQuery>()
          .Build();
        return await app.RunAsync([]);

        """ + Service;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("__fw_ILogger_Gen69Service = __loggerFactory.CreateLogger<global::Gen69Service>();");
      run.GeneratedSource.ShouldNotContain("NullLoggerFactory.Instance.CreateLogger<global::Gen69Service>()");
      run.GeneratorDiagnostics.Any(static d => d.Id == "NURU_H007").ShouldBeFalse(Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }

    public static async Task Should_resolve_service_constructor_logger_from_telemetry_factory()
    {
      // UseTelemetry() logs through app.LoggerFactory, which EnsureServicesInitialized reads from its
      // app parameter. The service logger falls back to a NullLogger when telemetry is off at runtime.
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Logging;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseTelemetry()
          .ConfigureServices(services => services.AddSingleton<Gen69Service>())
          .Map<Gen69ServiceQuery>()
          .Build();
        return await app.RunAsync([]);

        """ + Service;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("__fw_ILogger_Gen69Service = (app.LoggerFactory?.CreateLogger<global::Gen69Service>()");
      run.GeneratorDiagnostics.Any(static d => d.Id == "NURU_H007").ShouldBeFalse(Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }
  }
}
