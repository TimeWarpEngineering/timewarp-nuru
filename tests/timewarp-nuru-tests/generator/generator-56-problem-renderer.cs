#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

// A visible IResponseRenderer<SharedProblemDetails> replaces the default problem lines, and
// UseVerbatimRendererOutput() writes renderer text with no added newline (task 508-011). The same renderer
// is attached as NuruApp.ProblemRenderer for bind failures and escaped exceptions (task 508-018).
// Roslyn-hosted: a visible problem renderer in the CI multi-mode compilation would change every
// problem pair there, so this case runs in its own compilation.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen56ProblemRenderer
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using Microsoft.CodeAnalysis.Diagnostics;
  using System.Collections.Generic;
  using System.Linq;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class ProblemRendererTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ProblemRendererTests>();

    private const string Usings = """
      using System.Threading;
      using System.Threading.Tasks;
      using Microsoft.Extensions.DependencyInjection;
      using OneOf;
      using TimeWarp.Contracts;
      using TimeWarp.Foundation.Types;
      using TimeWarp.Mediator;
      using TimeWarp.Nuru;

      """;

    private const string Endpoint = """
      [Operation("t511gen")]
      public sealed class T511GenQuery : IQuery<OneOf<string, SharedProblemDetails>>
      {
        public sealed class Handler : IQueryHandler<T511GenQuery, OneOf<string, SharedProblemDetails>>
        {
          public Task<OneOf<string, SharedProblemDetails>> Handle(T511GenQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<string, SharedProblemDetails>>("ok");
        }
      }
      """;

    private const string VisibleRenderer = """
      public sealed class T511VisibleProblemRenderer : IResponseRenderer<SharedProblemDetails>
      {
        public string Render(SharedProblemDetails response) => "error=" + response.Title;
      }
      """;

    // Registered with ConfigureServices. An internal constructor keeps it from being the visible renderer.
    private const string RegisteredRenderer = """
      public sealed class T511RegisteredProblemRenderer : IResponseRenderer<SharedProblemDetails>
      {
        internal T511RegisteredProblemRenderer() { }

        public string Render(SharedProblemDetails response) => "registered=" + response.Title;
      }
      """;

    // Top-level statements must precede the type declarations.
    private static string Generate(string program, string types)
    {
      string source = Usings + program + "\n" + types;
      UpdateCompilation(source, null, out string generated, out IEnumerable<Diagnostic> diagnostics);
      // CS9137 is this harness: it does not enable the interceptors feature.
      List<Diagnostic> errors =
      [
        .. diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id != "CS9137")
      ];
      string.Join("\n", errors.Select(error => error.ToString())).ShouldBe(string.Empty);
      return generated;
    }

    public static async Task Should_write_a_visible_problem_renderer_to_stderr()
    {
      string generated = Generate(
        """
        NuruApp app = NuruApp.CreateBuilder()
          .Map<T511GenQuery>()
          .Build();

        app.RunAsync([]);
        """,
        Endpoint + "\n" + VisibleRenderer);

      generated.ShouldContain("app.Terminal.WriteErrorLine(new global::T511VisibleProblemRenderer().Render(__problem));");
      generated.ShouldNotContain("{__problem.Type} {__problem.Title}");
      generated.ShouldContain("int __status = __problem.Status is >= 1 and <= 255 ? __problem.Status.Value : 1;");
      await Task.CompletedTask;
    }

    public static async Task Should_write_a_visible_problem_renderer_verbatim_when_opted_in()
    {
      string generated = Generate(
        """
        NuruApp app = NuruApp.CreateBuilder()
          .UseVerbatimRendererOutput()
          .Map<T511GenQuery>()
          .Build();

        app.RunAsync([]);
        """,
        Endpoint + "\n" + VisibleRenderer);

      generated.ShouldContain("app.Terminal.Error.Write(new global::T511VisibleProblemRenderer().Render(__problem));");
      generated.ShouldNotContain("WriteErrorLine(new global::T511VisibleProblemRenderer()");
      await Task.CompletedTask;
    }

    public static async Task Should_keep_the_default_problem_lines_without_a_renderer()
    {
      string generated = Generate(
        """
        NuruApp app = NuruApp.CreateBuilder()
          .UseVerbatimRendererOutput()
          .Map<T511GenQuery>()
          .Build();

        app.RunAsync([]);
        """,
        Endpoint);

      generated.ShouldContain("app.Terminal.WriteErrorLine($\"{__problem.Type} {__problem.Title}\");");
      generated.ShouldNotContain(".Render(__problem)");
      await Task.CompletedTask;
    }

    public static async Task Should_attach_the_visible_problem_renderer_for_bind_failures()
    {
      string generated = Generate(
        """
        NuruApp app = NuruApp.CreateBuilder()
          .Map<T511GenQuery>()
          .Build();

        app.RunAsync([]);
        """,
        Endpoint + "\n" + VisibleRenderer);

      generated.ShouldContain("app.ProblemRenderer = __RenderProblem;");
      generated.ShouldContain("return new global::T511VisibleProblemRenderer().Render(__problem);");
      generated.ShouldNotContain("app.ProblemRendererForErrors = true;");
      generated.ShouldContain("app.WriteBindErrorAsync(");
      await Task.CompletedTask;
    }

    public static async Task Should_set_the_cli_opt_in_with_use_problem_renderer_for_errors()
    {
      string generated = Generate(
        """
        NuruApp app = NuruApp.CreateBuilder()
          .UseProblemRendererForErrors()
          .UseVerbatimRendererOutput()
          .Map<T511GenQuery>()
          .Build();

        app.RunAsync([]);
        """,
        Endpoint + "\n" + VisibleRenderer);

      generated.ShouldContain("app.ProblemRendererForErrors = true;");
      generated.ShouldContain("app.VerbatimRendererOutput = true;");
      await Task.CompletedTask;
    }

    public static async Task Should_prefer_a_registered_problem_renderer()
    {
      string generated = Generate(
        """
        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(services => services.AddSingleton<IResponseRenderer<SharedProblemDetails>, T511RegisteredProblemRenderer>())
          .Map<T511GenQuery>()
          .Build();

        app.RunAsync([]);
        """,
        Endpoint + "\n" + RegisteredRenderer);

      generated.ShouldContain("app.ProblemRenderer = __RenderProblem;");
      generated.ShouldContain(".Render(__problem);");
      generated.ShouldNotContain("return new global::T511RegisteredProblemRenderer().Render(__problem);");
      await Task.CompletedTask;
    }

    public static async Task Should_resolve_the_problem_renderer_from_runtime_di()
    {
      string generated = Generate(
        """
        NuruApp app = NuruApp.CreateBuilder()
          .UseMicrosoftDependencyInjection()
          .Map<T511GenQuery>()
          .Build();

        app.RunAsync([]);
        """,
        Endpoint);

      generated.ShouldContain("app.ProblemRenderer = __RenderProblem;");
      generated.ShouldContain("GetService<global::TimeWarp.Contracts.IResponseRenderer<global::TimeWarp.Foundation.Types.SharedProblemDetails>>(GetResolutionProvider(app))");
      await Task.CompletedTask;
    }

    public static async Task Should_not_attach_a_problem_renderer_when_there_is_none()
    {
      string generated = Generate(
        """
        NuruApp app = NuruApp.CreateBuilder()
          .Map<T511GenQuery>()
          .Build();

        app.RunAsync([]);
        """,
        Endpoint);

      generated.ShouldNotContain("__RenderProblem");
      generated.ShouldNotContain("app.ProblemRendererForErrors");
      await Task.CompletedTask;
    }

    private static CSharpCompilation UpdateCompilation(
      string source,
      IReadOnlyDictionary<string, string>? config,
      out string generated,
      out IEnumerable<Diagnostic> diagnostics)
    {
      CSharpGeneratorDriver driver = CreateDriver(source, config, "NuruProblemRenderer", out CSharpCompilation compilation);
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
      private readonly MapOptions Options;

      public MapConfig(IReadOnlyDictionary<string, string> values)
      {
        Options = new MapOptions(values);
      }

      public override AnalyzerConfigOptions GlobalOptions => Options;

      public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Options;

      public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Options;

      private sealed class MapOptions : AnalyzerConfigOptions
      {
        private readonly IReadOnlyDictionary<string, string> Values;

        public MapOptions(IReadOnlyDictionary<string, string> values)
        {
          Values = values;
        }

        public override bool TryGetValue(string key, out string value) =>
          Values.TryGetValue(key, out value!);
      }
    }
  }
}
