#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

// NURU_H008: an [Operation] with no visible handler is a warning. The build is
// accepted. Invoking that route returns exit code 1 when the handler is still
// unregistered. An [Operation] with no mediator kind is not an endpoint.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen55MissingOperationHandler
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using Microsoft.CodeAnalysis.Diagnostics;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class MissingOperationHandlerTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MissingOperationHandlerTests>();

    private static GeneratorDriverRunResult RunNuruGenerator(
      string source,
      IReadOnlyDictionary<string, string>? config = null,
      string assemblyName = "NuruH008MissingHandler")
    {
      CSharpGeneratorDriver driver = CreateDriver(source, config, assemblyName, out CSharpCompilation compilation);
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    private static CSharpCompilation UpdateCompilation(
      string source,
      IReadOnlyDictionary<string, string>? config,
      out string generated,
      out IEnumerable<Diagnostic> diagnostics)
    {
      CSharpGeneratorDriver driver = CreateDriver(source, config, "NuruMediatorDispatch", out CSharpCompilation compilation);
      GeneratorDriver ran = driver.RunGeneratorsAndUpdateCompilation(
        compilation,
        out Compilation updated,
        out System.Collections.Immutable.ImmutableArray<Diagnostic> generatorDiagnostics);
      generated = string.Join(
        "\n",
        ran.GetRunResult().Results.SelectMany(generator => generator.GeneratedSources).Select(sourceText => sourceText.SourceText.ToString()));
      diagnostics = generatorDiagnostics.Concat(updated.GetDiagnostics());
      return (CSharpCompilation)updated;
    }

    private static CSharpGeneratorDriver CreateDriver(
      string source,
      IReadOnlyDictionary<string, string>? config,
      string assemblyName,
      out CSharpCompilation compilation)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        if (path.Length > 0)
          references.Add(MetadataReference.CreateFromFile(path));
      }

      compilation = CSharpCompilation.Create(
        assemblyName: assemblyName,
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      if (config is null)
        return driver;

      return (CSharpGeneratorDriver)driver.WithUpdatedAnalyzerConfigOptions(new MapConfig(config));
    }

    private sealed class MapConfig : AnalyzerConfigOptionsProvider
    {
      private readonly MapOptions options;

      public MapConfig(IReadOnlyDictionary<string, string> values)
      {
        options = new MapOptions(values);
      }

      public override AnalyzerConfigOptions GlobalOptions => options;

      public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;

      public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => options;

      private sealed class MapOptions : AnalyzerConfigOptions
      {
        private readonly IReadOnlyDictionary<string, string> values;

        public MapOptions(IReadOnlyDictionary<string, string> values)
        {
          this.values = values;
        }

        public override bool TryGetValue(string key, out string value) =>
          values.TryGetValue(key, out value!);
      }
    }

    private static IEnumerable<Diagnostic> H008Diagnostics(GeneratorDriverRunResult result) =>
      result.Diagnostics
        .Concat(result.Results.SelectMany(generatorResult => generatorResult.Diagnostics))
        .Where(diagnostic => diagnostic.Id == "NURU_H008");

    private static bool HasH008(GeneratorDriverRunResult result) =>
      H008Diagnostics(result).Any();

    private static string GeneratedText(GeneratorDriverRunResult result) =>
      string.Join("\n", result.Results.SelectMany(generator => generator.GeneratedSources).Select(source => source.SourceText.ToString()));

    public static async Task Should_emit_nuru_h008_when_an_operation_has_no_visible_handler()
    {
      const string Source = """
        using TimeWarp.Contracts;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [Operation("t508missing")]
        public sealed class T508MissingQuery : IQuery<string>
        {
        }

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      HasH008(result).ShouldBeTrue();
      Diagnostic diagnostic = H008Diagnostics(result).First();
      diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
      diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("exits 1");
      GeneratedText(result).ShouldContain("No handler is registered for");
      await Task.CompletedTask;
    }

    public static async Task Should_skip_an_operation_that_is_not_a_mediator_message()
    {
      const string Source = """
        using TimeWarp.Contracts;
        using TimeWarp.Nuru;

        [Operation("t508plain")]
        public sealed class T508PlainOperation
        {
          public string Name { get; set; } = "";
        }

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      HasH008(result).ShouldBeFalse();
      string generated = GeneratedText(result);
      generated.ShouldNotContain("No handler is registered for");
      generated.ShouldNotContain("t508plain");
      await Task.CompletedTask;
    }

    public static async Task Should_not_emit_nuru_h008_when_a_handler_is_visible()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Contracts;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [Operation("t508present")]
        public sealed class T508PresentQuery : IQuery<string>
        {
        }

        public sealed class T508PresentHandler : IQueryHandler<T508PresentQuery, string>
        {
          public Task<string> Handle(T508PresentQuery query, CancellationToken cancellationToken) =>
            Task.FromResult("ok");
        }

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        app.RunAsync([]);
        """;

      HasH008(RunNuruGenerator(Source)).ShouldBeFalse();
      await Task.CompletedTask;
    }

    public static async Task Should_skip_pattern_validation_when_the_operation_is_not_a_mediator_message()
    {
      const string Source = """
        using TimeWarp.Contracts;
        using TimeWarp.Nuru;

        [Operation("not an endpoint")]
        public sealed class T508SpacedOperation
        {
          public string Name { get; set; } = "";
        }

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      result.Diagnostics.Concat(result.Results.SelectMany(generator => generator.Diagnostics))
        .Any(diagnostic => diagnostic.Id == "NURU_A001")
        .ShouldBeFalse();
      GeneratedText(result).ShouldNotContain("not an endpoint");
      await Task.CompletedTask;
    }

    public static async Task Should_call_a_nested_handler_directly_when_no_generated_mediator_exists()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Contracts;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [Operation("t508off")]
        public sealed class T508OffQuery : IQuery<string>
        {
          public sealed class Handler : IQueryHandler<T508OffQuery, string>
          {
            public Task<string> Handle(T508OffQuery query, CancellationToken cancellationToken) =>
              Task.FromResult("ok");
          }
        }

        NuruApp app = NuruApp.CreateBuilder()
          .Map<T508OffQuery>()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(
        Source,
        new Dictionary<string, string> { ["build_property.TimeWarpMediatorAssembly"] = "false" });
      string generated = GeneratedText(result);
      generated.ShouldContain("T508OffQuery.Handler");
      generated.ShouldContain("__handler.Handle(");
      generated.ShouldNotContain("__GetMediator");
      await Task.CompletedTask;
    }

    public static async Task Should_keep_get_mediator_when_direct_invoke_has_only_an_external_handler()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Contracts;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [Operation("t508directext")]
        public sealed class T508DirectExternal : ICommand<string>
        {
          public string Name { get; set; } = "ada";
        }

        public sealed class T508DirectExternalHandler : ICommandHandler<T508DirectExternal, string>
        {
          public Task<string> Handle(T508DirectExternal command, CancellationToken cancellationToken) =>
            Task.FromResult(command.Name);
        }

        NuruApp app = NuruApp.CreateBuilder()
          .UseDirectInvoke()
          .Map<T508DirectExternal>()
          .Build();

        app.RunAsync([]);
        """;

      Dictionary<string, string> config = new()
      {
        ["build_property.TimeWarpMediatorAssembly"] = "true"
      };
      UpdateCompilation(Source, config, out string generated, out IEnumerable<Diagnostic> diagnostics);
      generated.ShouldContain("private static global::TimeWarp.Mediator.IMediator __GetMediator()");
      generated.ShouldContain("__GetMediator()");
      diagnostics.Any(diagnostic =>
        diagnostic.Id == "CS0103" && diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("__GetMediator", StringComparison.Ordinal))
        .ShouldBeFalse();
      await Task.CompletedTask;
    }

    public static async Task Should_exit_1_for_an_external_handler_when_no_generated_mediator_exists()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Contracts;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [Operation("t508nomediator")]
        public sealed class T508NoMediatorCommand : ICommand<string>
        {
          public string Name { get; set; } = "ada";
        }

        public sealed class T508NoMediatorHandler : ICommandHandler<T508NoMediatorCommand, string>
        {
          public Task<string> Handle(T508NoMediatorCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(command.Name);
        }

        NuruApp app = NuruApp.CreateBuilder()
          .Map<T508NoMediatorCommand>()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(
        Source,
        new Dictionary<string, string> { ["build_property.TimeWarpMediatorAssembly"] = "false" });
      string generated = GeneratedText(result);
      generated.ShouldContain("The TimeWarp.Mediator generated mediator is not available in this compilation.");
      generated.ShouldContain("return 1;");
      generated.ShouldNotContain("IMediator __mediator = __GetMediator()");
      await Task.CompletedTask;
    }

    public static async Task Should_report_nuru_h009_when_the_endpoint_has_no_callable_constructor()
    {
      const string Source = """
        using TimeWarp.Contracts;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [Operation("t508badctor")]
        public sealed class T508BadCtorQuery : IQuery<string>
        {
          private T508BadCtorQuery()
          {
          }

          public T508BadCtorQuery(string missing)
          {
            _ = missing;
          }

          public string Name { get; set; } = "a";
        }

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      Diagnostic diagnostic = result.Diagnostics
        .Concat(result.Results.SelectMany(generator => generator.Diagnostics))
        .First(item => item.Id == "NURU_H009");
      diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
      GeneratedText(result).ShouldNotContain("T508BadCtorQuery __command");
      await Task.CompletedTask;
    }
  }
}
