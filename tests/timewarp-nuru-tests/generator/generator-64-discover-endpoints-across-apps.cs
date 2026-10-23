#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

// NURU_A003 (task 522): unfiltered DiscoverEndpoints() in a compilation that builds Nuru apps in
// more than one source file pulls the endpoints of every other file into the app. One file with
// many apps (a standalone test run) and a one-app CLI stay silent. #pragma suppresses it.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen63DiscoverEndpointsAcrossApps
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using Microsoft.CodeAnalysis.Diagnostics;
  using System.Collections.Generic;
  using System.Collections.Immutable;
  using System.Globalization;
  using System.Linq;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class DiscoverEndpointsAcrossAppsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<DiscoverEndpointsAcrossAppsTests>();

    private const string Endpoints = """
      using TimeWarp.Mediator;
      using TimeWarp.Nuru;

      [NuruRoute("ping")]
      public sealed class PingQuery : IQuery<string>
      {
        public sealed class Handler : IQueryHandler<PingQuery, string>
        {
          public Task<string> Handle(PingQuery query, CancellationToken ct) => Task.FromResult("pong");
        }
      }

      [NuruRoute("pong")]
      public sealed class PongQuery : IQuery<string>
      {
        public sealed class Handler : IQueryHandler<PongQuery, string>
        {
          public Task<string> Handle(PongQuery query, CancellationToken ct) => Task.FromResult("ping");
        }
      }
      """;

    private const string DiscoveringProgram = """
      using TimeWarp.Nuru;

      NuruApp app = NuruApp.CreateBuilder()
        .DiscoverEndpoints()
        .Build();

      await app.RunAsync([]);
      """;

    private const string OtherApp = """
      using TimeWarp.Nuru;

      public static class OtherApp
      {
        public static async Task Run()
        {
          NuruApp app = NuruApp.CreateBuilder()
            .Map("other").WithHandler(() => "other").AsQuery().Done()
            .Build();

          await app.RunAsync([]);
        }
      }
      """;

    private static (CSharpCompilation Compilation, ImmutableArray<Diagnostic> Diagnostics) Run(
      params (string Path, string Source)[] files)
    {
      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        if (path.Length > 0)
          references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpParseOptions parseOptions = new(LanguageVersion.Preview);
      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Gen63DiscoverEndpoints",
        syntaxTrees: [.. files.Select(file => CSharpSyntaxTree.ParseText(file.Source, parseOptions, path: file.Path))],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication)
          .WithUsings("System", "System.Threading", "System.Threading.Tasks"));

      GeneratorDriverRunResult result = CSharpGeneratorDriver.Create(new NuruGenerator())
        .RunGenerators(compilation)
        .GetRunResult();

      return (compilation, [.. result.Diagnostics.Where(diagnostic => diagnostic.Id == "NURU_A003")]);
    }

    public static async Task Should_warn_when_apps_are_built_in_more_than_one_file()
    {
      (_, ImmutableArray<Diagnostic> diagnostics) = Run(
        ("program.cs", DiscoveringProgram),
        ("other-app.cs", OtherApp),
        ("endpoints.cs", Endpoints));

      diagnostics.Length.ShouldBe(1);
      Diagnostic diagnostic = diagnostics[0];
      diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
      string message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
      message.ShouldContain("all 2 endpoints");
      message.ShouldContain("in 2 source files");
      message.ShouldContain("Map<TEndpoint>()");

      // The location starts at DiscoverEndpoints, not at the start of the builder chain.
      FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
      span.Path.ShouldBe("program.cs");
      span.StartLinePosition.Line.ShouldBe(3);
      diagnostic.Location.SourceTree.ShouldNotBeNull();
      diagnostic.Location.SourceTree!.ToString()
        .Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length)
        .ShouldBe("DiscoverEndpoints()");
      await Task.CompletedTask;
    }

    public static async Task Should_not_warn_for_a_single_app()
    {
      (_, ImmutableArray<Diagnostic> diagnostics) = Run(
        ("program.cs", DiscoveringProgram),
        ("endpoints.cs", Endpoints));

      diagnostics.ShouldBeEmpty();
      await Task.CompletedTask;
    }

    public static async Task Should_not_warn_for_many_apps_in_one_file()
    {
      // A standalone Jaribu test file builds one app per test, all in the same file.
      const string ManyApps = """
        using TimeWarp.Nuru;

        NuruApp first = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        NuruApp second = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        await first.RunAsync([]);
        await second.RunAsync([]);
        """;

      (_, ImmutableArray<Diagnostic> diagnostics) = Run(
        ("many-apps.cs", ManyApps),
        ("endpoints.cs", Endpoints));

      diagnostics.ShouldBeEmpty();
      await Task.CompletedTask;
    }

    public static async Task Should_not_warn_for_group_filtered_discovery_or_map()
    {
      const string ScopedProgram = """
        using TimeWarp.Nuru;

        [NuruRouteGroup("tools")]
        public abstract class ToolsGroup;

        public static class Scoped
        {
          public static async Task Run()
          {
            NuruApp filtered = NuruApp.CreateBuilder()
              .DiscoverEndpoints(typeof(ToolsGroup))
              .Build();

            NuruApp mapped = NuruApp.CreateBuilder()
              .DiscoverEndpoints()
              .Map<PingQuery>()
              .Build();

            await filtered.RunAsync([]);
            await mapped.RunAsync([]);
          }
        }
        """;

      (_, ImmutableArray<Diagnostic> diagnostics) = Run(
        ("scoped.cs", ScopedProgram),
        ("other-app.cs", OtherApp),
        ("endpoints.cs", Endpoints));

      diagnostics.ShouldBeEmpty();
      await Task.CompletedTask;
    }

    public static async Task Should_be_suppressed_by_pragma()
    {
      const string SuppressedProgram = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
        #pragma warning disable NURU_A003 // This app tests discovery itself.
          .DiscoverEndpoints()
        #pragma warning restore NURU_A003
          .Build();

        await app.RunAsync([]);
        """;

      (CSharpCompilation compilation, ImmutableArray<Diagnostic> reported) = Run(
        ("program.cs", SuppressedProgram),
        ("other-app.cs", OtherApp),
        ("endpoints.cs", Endpoints));

      // The generator reports it; the compiler drops it because the location is bound to the
      // pragma'd source tree. Replaying it through an analyzer applies the same pragma filter.
      reported.Length.ShouldBe(1);
      ImmutableArray<Diagnostic> afterPragma = await compilation
        .WithAnalyzers([new ReplayAnalyzer(reported)])
        .GetAnalyzerDiagnosticsAsync();

      afterPragma.ShouldBeEmpty();
    }

    public static async Task Should_survive_pragma_filter_without_pragma()
    {
      // Control for the pragma test: the same replay keeps an unsuppressed diagnostic.
      (CSharpCompilation compilation, ImmutableArray<Diagnostic> reported) = Run(
        ("program.cs", DiscoveringProgram),
        ("other-app.cs", OtherApp),
        ("endpoints.cs", Endpoints));

      ImmutableArray<Diagnostic> afterPragma = await compilation
        .WithAnalyzers([new ReplayAnalyzer(reported)])
        .GetAnalyzerDiagnosticsAsync();

      afterPragma.Length.ShouldBe(1);
      afterPragma[0].Id.ShouldBe("NURU_A003");
    }

    /// <summary>
    /// Re-reports generator diagnostics as analyzer diagnostics, so Roslyn's #pragma filter runs on them.
    /// </summary>
#pragma warning disable RS1001, RS1036 // Test-only analyzer, never shipped.
    private sealed class ReplayAnalyzer(ImmutableArray<Diagnostic> diagnostics) : DiagnosticAnalyzer
#pragma warning restore RS1001, RS1036
    {
      public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [.. diagnostics.Select(diagnostic => diagnostic.Descriptor).Distinct()];

      public override void Initialize(AnalysisContext context)
      {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(compilationContext =>
        {
          foreach (Diagnostic diagnostic in diagnostics)
            compilationContext.ReportDiagnostic(diagnostic);
        });
      }
    }
  }
}
