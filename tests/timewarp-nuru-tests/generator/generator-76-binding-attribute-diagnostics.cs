#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Pins NURU_A002 (parameters omit Order), NURU_A004 (IsRepeated on a scalar),
// and NURU_A005 (Parameter Name is not an identifier).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen76BindingAttributeDiagnostics
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class BindingAttributeDiagnosticTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<BindingAttributeDiagnosticTests>();

    public static async Task Should_report_nuru_a002_when_several_parameters_omit_order()
    {
      const string Source = """
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("copy")]
        public sealed class CopyCommand : ICommand<Unit>
        {
          [Parameter]
          public string Source { get; set; } = "";

          [Parameter]
          public string Destination { get; set; } = "";

          public sealed class Handler : IRequestHandler<CopyCommand, Unit>
          {
            public ValueTask<Unit> Handle(CopyCommand command, CancellationToken cancellationToken) => Unit.ValueTask;
          }
        }

        NuruApp app = NuruApp.CreateBuilder().DiscoverEndpoints().Build();
        app.RunAsync([]);
        """;

      List<Diagnostic> diagnostics = [.. Diagnostics(Source, "NURU_A002")];
      diagnostics.Count.ShouldBe(2);
      await Task.CompletedTask;
    }

    public static async Task Should_not_report_nuru_a002_for_one_parameter()
    {
      const string Source = """
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("save")]
        public sealed class SaveCommand : ICommand<Unit>
        {
          [Parameter]
          public string Source { get; set; } = "";

          public sealed class Handler : IRequestHandler<SaveCommand, Unit>
          {
            public ValueTask<Unit> Handle(SaveCommand command, CancellationToken cancellationToken) => Unit.ValueTask;
          }
        }

        NuruApp app = NuruApp.CreateBuilder().DiscoverEndpoints().Build();
        app.RunAsync([]);
        """;

      Diagnostics(Source, "NURU_A002").Any().ShouldBeFalse();
      await Task.CompletedTask;
    }

    public static async Task Should_report_nuru_a004_when_is_repeated_is_set_on_a_scalar()
    {
      const string Source = """
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("env")]
        public sealed class EnvCommand : ICommand<Unit>
        {
          [Option("env", IsRepeated = true)]
          public string Env { get; set; } = "";

          public sealed class Handler : IRequestHandler<EnvCommand, Unit>
          {
            public ValueTask<Unit> Handle(EnvCommand command, CancellationToken cancellationToken) => Unit.ValueTask;
          }
        }

        NuruApp app = NuruApp.CreateBuilder().DiscoverEndpoints().Build();
        app.RunAsync([]);
        """;

      List<Diagnostic> diagnostics = [.. Diagnostics(Source, "NURU_A004")];
      diagnostics.Count.ShouldBe(1);
      diagnostics[0].Severity.ShouldBe(DiagnosticSeverity.Error);
      diagnostics[0].GetMessage(CultureInfo.InvariantCulture).ShouldContain("env");
      await Task.CompletedTask;
    }

    public static async Task Should_not_report_nuru_a004_when_is_repeated_is_set_on_a_collection()
    {
      const string Source = """
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("env")]
        public sealed class EnvCommand : ICommand<Unit>
        {
          [Option("env", IsRepeated = true)]
          public string[] Env { get; set; } = [];

          public sealed class Handler : IRequestHandler<EnvCommand, Unit>
          {
            public ValueTask<Unit> Handle(EnvCommand command, CancellationToken cancellationToken) => Unit.ValueTask;
          }
        }

        NuruApp app = NuruApp.CreateBuilder().DiscoverEndpoints().Build();
        app.RunAsync([]);
        """;

      Diagnostics(Source, "NURU_A004").Any().ShouldBeFalse();
      await Task.CompletedTask;
    }

    public static async Task Should_report_nuru_a005_when_parameter_name_is_not_an_identifier()
    {
      const string Source = """
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [NuruRoute("save")]
        public sealed class SaveCommand : ICommand<Unit>
        {
          [Parameter(Name = "source-file")]
          public string SourcePath { get; set; } = "";

          public sealed class Handler : IRequestHandler<SaveCommand, Unit>
          {
            public ValueTask<Unit> Handle(SaveCommand command, CancellationToken cancellationToken) => Unit.ValueTask;
          }
        }

        NuruApp app = NuruApp.CreateBuilder().DiscoverEndpoints().Build();
        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = Run(Source);
      List<Diagnostic> diagnostics = [.. result.Diagnostics.Where(diagnostic => diagnostic.Id == "NURU_A005")];
      diagnostics.Count.ShouldBe(1);
      diagnostics[0].Severity.ShouldBe(DiagnosticSeverity.Error);
      diagnostics[0].GetMessage(CultureInfo.InvariantCulture).ShouldContain("source-file");

      string generated = string.Concat(result.Results.SelectMany(run => run.GeneratedSources).Select(source => source.SourceText.ToString()));
      generated.ShouldNotContain("source-file");
      generated.ShouldContain("sourcepath");
      await Task.CompletedTask;
    }

    private static IEnumerable<Diagnostic> Diagnostics(string source, string id)
      => Run(source).Diagnostics.Where(diagnostic => diagnostic.Id == id);

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
        assemblyName: "NuruBindingAttributes",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      // RunResult.Diagnostics is already every generator diagnostic. Concatenating Results again doubles them.
      return driver.RunGenerators(compilation).GetRunResult();
    }
  }
}
