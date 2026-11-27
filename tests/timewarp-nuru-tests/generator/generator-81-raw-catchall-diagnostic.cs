#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Pins NURU_S011 for a raw catch-all that is not alone, and that a valid raw route
// is emitted with {**args} and the unstripped argv (__rawRouteArgs).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen81RawCatchAllDiagnostic
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using System.Globalization;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class RawCatchAllDiagnosticTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<RawCatchAllDiagnosticTests>();

    public static async Task Should_diagnose_fluent_raw_catchall_with_an_option_and_not_emit_it()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("tail {**args} --verbose").WithHandler((string[] args, bool verbose) => args.Length).AsCommand().Done()
          .Map("greet {name}").WithHandler((string name) => name).AsCommand().Done()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = Run(Source);
      Diagnostic? diagnostic = DiagnosticWithId(result, "NURU_S011");
      diagnostic.ShouldNotBeNull();
      diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
      diagnostic.Descriptor.Category.ShouldBe("RoutePattern.Semantic");
      diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("args");

      string generated = Generated(result);
      generated.ShouldContain("// Route: greet {name}");
      generated.ShouldNotContain("// Route: tail {**args} --verbose");

      await Task.CompletedTask;
    }

    public static async Task Should_emit_raw_route_args_for_a_fluent_raw_catchall()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("tail {**args}").WithHandler((string[] args) => args.Length).AsCommand().Done()
          .Build();

        app.RunAsync([]);
        """;

      string generated = Generated(Run(Source));
      generated.ShouldContain("// Route: tail {**args}");
      generated.ShouldContain("routeArgs = __rawRouteArgs;");
      generated.ShouldContain("{**args}");

      await Task.CompletedTask;
    }

    public static async Task Should_emit_raw_route_args_for_an_endpoint()
    {
      const string Source = """
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("raw-tail")]
        public sealed class RawTailCommand : ICommand<Unit>
        {
          [Parameter(IsCatchAll = true, IsRaw = true)]
          public string[] Args { get; set; } = [];

          public sealed class Handler : IRequestHandler<RawTailCommand, Unit>
          {
            public ValueTask<Unit> Handle(RawTailCommand command, CancellationToken cancellationToken) => Unit.ValueTask;
          }
        }

        NuruApp app = NuruApp.CreateBuilder().DiscoverEndpoints().Build();
        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = Run(Source);
      string generated = Generated(result);
      generated.ShouldContain("// Route: raw-tail");
      generated.ShouldContain("routeArgs = __rawRouteArgs;");
      generated.ShouldContain("{**args}");
      DiagnosticWithId(result, "NURU_S011").ShouldBeNull();

      await Task.CompletedTask;
    }

    public static async Task Should_diagnose_endpoint_raw_catchall_with_another_parameter()
    {
      const string Source = """
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("raw-tail")]
        public sealed class RawTailCommand : ICommand<Unit>
        {
          [Parameter(Order = 0)]
          public string File { get; set; } = "";

          [Parameter(Order = 1, IsCatchAll = true, IsRaw = true)]
          public string[] Args { get; set; } = [];

          public sealed class Handler : IRequestHandler<RawTailCommand, Unit>
          {
            public ValueTask<Unit> Handle(RawTailCommand command, CancellationToken cancellationToken) => Unit.ValueTask;
          }
        }

        NuruApp app = NuruApp.CreateBuilder().DiscoverEndpoints().Build();
        app.RunAsync([]);
        """;

      Diagnostic? diagnostic = DiagnosticWithId(Run(Source), "NURU_S011");
      diagnostic.ShouldNotBeNull();
      diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
      diagnostic.Descriptor.Category.ShouldBe("RoutePattern.Semantic");

      await Task.CompletedTask;
    }

    public static async Task Should_diagnose_israw_without_iscatchall()
    {
      const string Source = """
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("raw-tail")]
        public sealed class RawTailCommand : ICommand<Unit>
        {
          [Parameter(IsRaw = true)]
          public string Args { get; set; } = "";

          public sealed class Handler : IRequestHandler<RawTailCommand, Unit>
          {
            public ValueTask<Unit> Handle(RawTailCommand command, CancellationToken cancellationToken) => Unit.ValueTask;
          }
        }

        NuruApp app = NuruApp.CreateBuilder().DiscoverEndpoints().Build();
        app.RunAsync([]);
        """;

      DiagnosticWithId(Run(Source), "NURU_S011").ShouldNotBeNull();

      await Task.CompletedTask;
    }

    public static async Task Should_diagnose_raw_catchall_that_inherits_a_group_option()
    {
      const string Source = """
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRouteGroup("tool")]
        public abstract class ToolGroup
        {
          [GroupOption("verbose", "v")]
          public bool Verbose { get; set; }
        }

        [NuruRoute("raw-tail")]
        public sealed class RawTailCommand : ToolGroup, ICommand<Unit>
        {
          [Parameter(IsCatchAll = true, IsRaw = true)]
          public string[] Args { get; set; } = [];

          public sealed class Handler : IRequestHandler<RawTailCommand, Unit>
          {
            public ValueTask<Unit> Handle(RawTailCommand command, CancellationToken cancellationToken) => Unit.ValueTask;
          }
        }

        NuruApp app = NuruApp.CreateBuilder().DiscoverEndpoints().Build();
        app.RunAsync([]);
        """;

      DiagnosticWithId(Run(Source), "NURU_S011").ShouldNotBeNull();

      await Task.CompletedTask;
    }

    private static Diagnostic? DiagnosticWithId(GeneratorDriverRunResult result, string id)
    {
      foreach (Diagnostic diagnostic in result.Diagnostics)
      {
        if (diagnostic.Id == id)
          return diagnostic;
      }

      return null;
    }

    private static string Generated(GeneratorDriverRunResult result) =>
      string.Concat(result.Results.SelectMany(run => run.GeneratedSources).Select(source => source.SourceText.ToString()));

    private static GeneratorDriverRunResult Run(string source)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
      string trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in trusted.Split(Path.PathSeparator))
      {
        if (path.Length > 0)
          references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "NuruRawCatchAll",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      return driver.RunGenerators(compilation).GetRunResult();
    }
  }
}
