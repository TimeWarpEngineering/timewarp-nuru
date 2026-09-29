#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// NURU_R005: a user option whose long form is json-args is reserved for the
// --json-args built-in. Routes that do not use that long form must not report it.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen50JsonArgsReserved
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using System.Linq;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class JsonArgsReservedTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<JsonArgsReservedTests>();

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
        assemblyName: "NuruR005JsonArgsRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    private static bool HasR005(GeneratorDriverRunResult result) =>
      result.Diagnostics.Any(d => d.Id == "NURU_R005")
      || result.Results.SelectMany(r => r.Diagnostics).Any(d => d.Id == "NURU_R005");

    public static async Task Should_emit_nuru_r005_when_an_option_long_form_is_json_args()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("tool --json-args {value}").WithHandler((string value) => value).AsCommand().Done()
          .Build();

        app.RunAsync([]);
        """;

      HasR005(RunNuruGenerator(Source)).ShouldBeTrue();
      await Task.CompletedTask;
    }

    public static async Task Should_not_emit_nuru_r005_for_an_ordinary_option()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("tool --title {value}").WithHandler((string value) => value).AsCommand().Done()
          .Build();

        app.RunAsync([]);
        """;

      HasR005(RunNuruGenerator(Source)).ShouldBeFalse();
      await Task.CompletedTask;
    }
  }
}
