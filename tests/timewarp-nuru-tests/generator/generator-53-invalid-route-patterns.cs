#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// A route pattern the parser rejects must report a NURU_P diagnostic and must not be
// emitted as a route. Before task 482-006 the generator dropped InvalidIdentifierError,
// InvalidModifierCombinationError, and AdjacentParametersError silently and registered
// the raw pattern as a single fallback literal route.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen53InvalidRoutePatterns
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using System.Globalization;
  using System.Linq;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class InvalidPatternTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<InvalidPatternTests>();

    private static GeneratorDriverRunResult RunNuruGenerator(string pattern)
    {
      string source = $$""""
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("""{{pattern}}""").WithHandler(() => "bad").AsCommand().Done()
          .Map("greet {name}").WithHandler((string name) => name).AsCommand().Done()
          .Build();

        app.RunAsync([]);
        """";

      SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "NuruInvalidPatternRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    private static Diagnostic[] AllDiagnostics(GeneratorDriverRunResult result) =>
      [.. result.Diagnostics, .. result.Results.SelectMany(r => r.Diagnostics)];

    private static string GeneratedCode(GeneratorDriverRunResult result) =>
      string.Join("\n", result.Results.SelectMany(r => r.GeneratedSources).Select(static g => g.SourceText.ToString()));

    private static void AssertDiagnosedAndNotEmitted(string pattern, string expectedId)
    {
      GeneratorDriverRunResult result = RunNuruGenerator(pattern);

      Diagnostic? diagnostic = AllDiagnostics(result).FirstOrDefault(d => d.Id == expectedId);
      diagnostic.ShouldNotBeNull();
      diagnostic!.Severity.ShouldBe(DiagnosticSeverity.Error);
      diagnostic.Descriptor.Category.ShouldBe("RoutePattern.Syntax");
      diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldNotBeNullOrWhiteSpace();

      string generated = GeneratedCode(result);
      generated.ShouldContain("// Route: greet {name}");
      generated.ShouldNotContain($"// Route: {pattern}");
    }

    public static async Task Should_diagnose_adjacent_parameters_and_not_emit_route()
    {
      AssertDiagnosedAndNotEmitted("{a}{b}", "NURU_P010");
      await Task.CompletedTask;
    }

    public static async Task Should_diagnose_optional_catch_all_and_not_emit_route()
    {
      AssertDiagnosedAndNotEmitted("{*name?}", "NURU_P009");
      await Task.CompletedTask;
    }

    public static async Task Should_diagnose_invalid_identifier_and_not_emit_route()
    {
      AssertDiagnosedAndNotEmitted("{my-param}", "NURU_P008");
      await Task.CompletedTask;
    }

    public static async Task Should_still_diagnose_unbalanced_brace_and_not_emit_route()
    {
      GeneratorDriverRunResult result = RunNuruGenerator("greet {name");

      AllDiagnostics(result).ShouldContain(d => d.Id.StartsWith("NURU_P", StringComparison.Ordinal));
      GeneratedCode(result).ShouldNotContain("// Route: greet {name\n");
      await Task.CompletedTask;
    }

    public static async Task Should_still_diagnose_bad_option_and_not_emit_route()
    {
      GeneratorDriverRunResult result = RunNuruGenerator("deploy --{x}");

      AllDiagnostics(result).ShouldContain(d => d.Id.StartsWith("NURU_P", StringComparison.Ordinal));
      GeneratedCode(result).ShouldNotContain("// Route: deploy --{x}");
      await Task.CompletedTask;
    }
  }
}
