#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// NURU_R004: REPL AutoStartWhenEmpty makes a top-level default route unreachable.
// The generated interceptor starts the REPL when routeArgs.Length == 0 before user
// routes are matched, so Map("") / [NuruRoute("")] at the top level can never run.
// A "" route inside a group does not conflict. AutoStartWhenEmpty false, and apps
// that never call AddRepl(), do not report NURU_R004. A [NuruRoute("")] with a
// required positional parameter does not match an empty argument list, so it does
// not conflict. An optional parameter still matches that empty list and does.
// An endpoint [NuruRoute("")] with an optional parameter keeps its own location when a
// fluent Map("") is registered in the same app. The attribute span and the Map literal
// are different keys.
//
// Hosts NuruGenerator in a CSharpGeneratorDriver over in-memory source and inspects
// GeneratorDriverRunResult for NURU_R004. RunAsync is required so AppExtractor builds
// an AppModel and validation runs.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen49NuruR004ReplDefaultRoute
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using System.Globalization;
  using System.Linq;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class NuruR004ReplDefaultRouteTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<NuruR004ReplDefaultRouteTests>();

    /// <summary>
    /// Runs NuruGenerator over the given source with the test process's own
    /// assemblies as metadata references (includes TimeWarp.Nuru).
    /// </summary>
    private static GeneratorDriverRunResult RunNuruGenerator(string source)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "NuruR004ReplDefaultRouteRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    private static bool HasR004(GeneratorDriverRunResult result) =>
      result.Diagnostics.Any(d => d.Id == "NURU_R004")
      || result.Results.SelectMany(r => r.Diagnostics).Any(d => d.Id == "NURU_R004");

    private static Diagnostic R004(GeneratorDriverRunResult result) =>
      result.Diagnostics.FirstOrDefault(d => d.Id == "NURU_R004")
      ?? result.Results.SelectMany(r => r.Diagnostics).First(d => d.Id == "NURU_R004");

    private static string GeneratedSource(GeneratorDriverRunResult result) =>
      string.Join("\n", result.Results.SelectMany(r => r.GeneratedSources).Select(g => g.SourceText.ToString()));

    /// <summary>
    /// Fluent Map("") plus AutoStartWhenEmpty is NURU_R004, anchored at the empty pattern.
    /// </summary>
    public static async Task Should_emit_nuru_r004_for_fluent_default_route()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .AddRepl(options => options.AutoStartWhenEmpty = true)
          .Map("").WithHandler(() => "fluent-default").AsCommand().Done()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      Diagnostic diagnostic = R004(result);

      diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
      diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("AutoStartWhenEmpty");
      diagnostic.Location.IsInSource.ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Map&lt;T&gt;() of a [NuruRoute("")] endpoint plus AutoStartWhenEmpty is NURU_R004.
    /// </summary>
    public static async Task Should_emit_nuru_r004_for_attributed_default_route()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("", Description = "Say hello")]
        public sealed class R004AttributedDefault : IQuery<string>
        {
          public sealed class Handler : IQueryHandler<R004AttributedDefault, string>
          {
            public Task<string> Handle(R004AttributedDefault query, CancellationToken cancellationToken)
            {
              return Task.FromResult("attributed-default");
            }
          }
        }

        NuruApp app = NuruApp.CreateBuilder([])
          .Map<R004AttributedDefault>()
          .AddRepl(options => { options.AutoStartWhenEmpty = true; })
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      HasR004(result).ShouldBeTrue();
      GeneratedSource(result).ShouldContain("R004AttributedDefault");

      await Task.CompletedTask;
    }

    /// <summary>
    /// DiscoverEndpoints() pulls in a top-level [NuruRoute("")] and reports NURU_R004.
    /// </summary>
    public static async Task Should_emit_nuru_r004_for_discovered_default_route()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("")]
        public sealed class R004DiscoveredDefault : IQuery<string>
        {
          public sealed class Handler : IQueryHandler<R004DiscoveredDefault, string>
          {
            public Task<string> Handle(R004DiscoveredDefault query, CancellationToken cancellationToken)
            {
              return Task.FromResult("discovered-default");
            }
          }
        }

        NuruApp app = NuruApp.CreateBuilder([])
          .DiscoverEndpoints()
          .AddRepl(options => { options.AutoStartWhenEmpty = true; })
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      HasR004(result).ShouldBeTrue();
      GeneratedSource(result).ShouldContain("R004DiscoveredDefault");

      await Task.CompletedTask;
    }

    /// <summary>
    /// A "" route inside a fluent group still requires the prefix, so it does not conflict.
    /// </summary>
    public static async Task Should_not_emit_nuru_r004_for_empty_route_inside_fluent_group()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .AddRepl(options => options.AutoStartWhenEmpty = true)
          .WithGroupPrefix("git")
            .Map("")
              .WithHandler(() => "grouped-fluent-default")
              .AsCommand()
              .Done()
            .Done()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      HasR004(result).ShouldBeFalse();
      GeneratedSource(result).ShouldContain("grouped-fluent-default");

      await Task.CompletedTask;
    }

    /// <summary>
    /// A [NuruRoute("")] endpoint under [NuruRouteGroup] is not a top-level default route.
    /// </summary>
    public static async Task Should_not_emit_nuru_r004_for_empty_route_inside_attributed_group()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRouteGroup("git")]
        public class R004GitGroup { }

        [NuruRoute("")]
        public sealed class R004GroupedDefault : R004GitGroup, IQuery<string>
        {
          public sealed class Handler : IQueryHandler<R004GroupedDefault, string>
          {
            public Task<string> Handle(R004GroupedDefault query, CancellationToken cancellationToken)
            {
              return Task.FromResult("grouped-attributed-default");
            }
          }
        }

        NuruApp app = NuruApp.CreateBuilder([])
          .DiscoverEndpoints()
          .AddRepl(options => { options.AutoStartWhenEmpty = true; })
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      HasR004(result).ShouldBeFalse();
      GeneratedSource(result).ShouldContain("R004GroupedDefault");

      await Task.CompletedTask;
    }

    /// <summary>
    /// AutoStartWhenEmpty false leaves the default route reachable.
    /// </summary>
    public static async Task Should_not_emit_nuru_r004_when_autostart_is_false()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .AddRepl(options => options.AutoStartWhenEmpty = false)
          .Map("").WithHandler(() => "autostart-off-default").AsCommand().Done()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      HasR004(result).ShouldBeFalse();
      GeneratedSource(result).ShouldContain("autostart-off-default");

      await Task.CompletedTask;
    }

    /// <summary>
    /// A default route without AddRepl() is not a conflict.
    /// </summary>
    public static async Task Should_not_emit_nuru_r004_when_repl_is_not_added()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("").WithHandler(() => "no-repl-default").AsCommand().Done()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      HasR004(result).ShouldBeFalse();
      GeneratedSource(result).ShouldContain("no-repl-default");

      await Task.CompletedTask;
    }

    /// <summary>
    /// A required positional parameter does not match an empty argument list, so
    /// AutoStartWhenEmpty does not hide the route.
    /// </summary>
    public static async Task Should_not_emit_nuru_r004_for_required_parameter_on_empty_pattern()
    {
      const string Source = """
        #nullable enable
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("")]
        public sealed class R004RequiredParamDefault : IQuery<string>
        {
          [Parameter]
          public string Name { get; set; } = "";

          public sealed class Handler : IQueryHandler<R004RequiredParamDefault, string>
          {
            public Task<string> Handle(R004RequiredParamDefault query, CancellationToken cancellationToken)
            {
              return Task.FromResult(query.Name);
            }
          }
        }

        NuruApp app = NuruApp.CreateBuilder([])
          .Map<R004RequiredParamDefault>()
          .AddRepl(options => { options.AutoStartWhenEmpty = true; })
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      HasR004(result).ShouldBeFalse();
      GeneratedSource(result).ShouldContain("R004RequiredParamDefault");

      await Task.CompletedTask;
    }

    /// <summary>
    /// An optional parameter still matches an empty argument list, so AutoStartWhenEmpty
    /// hides that invocation and NURU_R004 still fires.
    /// </summary>
    public static async Task Should_emit_nuru_r004_for_optional_parameter_on_empty_pattern()
    {
      const string Source = """
        #nullable enable
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("")]
        public sealed class R004OptionalParamDefault : IQuery<string>
        {
          [Parameter]
          public string? Name { get; set; }

          public sealed class Handler : IQueryHandler<R004OptionalParamDefault, string>
          {
            public Task<string> Handle(R004OptionalParamDefault query, CancellationToken cancellationToken)
            {
              return Task.FromResult(query.Name ?? "");
            }
          }
        }

        NuruApp app = NuruApp.CreateBuilder([])
          .Map<R004OptionalParamDefault>()
          .AddRepl(options => { options.AutoStartWhenEmpty = true; })
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      HasR004(result).ShouldBeTrue();
      GeneratedSource(result).ShouldContain("R004OptionalParamDefault");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Endpoint locations are stored under EffectivePattern. A fluent Map("") owns the
    /// empty OriginalPattern key. NURU_R004 for [NuruRoute("")] with an optional parameter
    /// must still squiggle the attribute, and the fluent diagnostic must squiggle Map("").
    /// </summary>
    public static async Task Should_anchor_endpoint_nuru_r004_beside_fluent_empty_route()
    {
      const string Source = """
        #nullable enable
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("")]
        public sealed class R004EndpointBesideFluentEmpty : IQuery<string>
        {
          [Parameter]
          public string? Name { get; set; }

          public sealed class Handler : IQueryHandler<R004EndpointBesideFluentEmpty, string>
          {
            public Task<string> Handle(R004EndpointBesideFluentEmpty query, CancellationToken cancellationToken)
            {
              return Task.FromResult(query.Name ?? "");
            }
          }
        }

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("").WithHandler(() => "fluent-default").AsCommand().Done()
          .Map<R004EndpointBesideFluentEmpty>()
          .AddRepl(options => { options.AutoStartWhenEmpty = true; })
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      List<Diagnostic> r004 = DiagnosticsWithId(result, "NURU_R004");

      List<string> spans = [];
      foreach (Diagnostic diagnostic in r004)
      {
        diagnostic.Location.IsInSource.ShouldBeTrue();
        spans.Add((await diagnostic.Location.SourceTree!.GetTextAsync()).ToString(diagnostic.Location.SourceSpan));
      }

      r004.Count.ShouldBeGreaterThan(1);
      spans.ShouldContain("\"\"");
      spans.Any(span => span.Contains("NuruRoute", StringComparison.Ordinal)).ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Returns every diagnostic with the given id from the generator run.
    /// </summary>
    private static List<Diagnostic> DiagnosticsWithId(GeneratorDriverRunResult result, string id) =>
      [.. result.Diagnostics.Concat(result.Results.SelectMany(r => r.Diagnostics)).Where(d => d.Id == id).Distinct()];
  }
}
