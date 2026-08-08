#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 454-013: unbound boolean flags are route discriminators
// (required to match), so "list --all" + "list {filter?}" must NOT produce a false
// NURU_R003 unreachable-route warning. Bound boolean flags remain optional at match
// time, so a genuine shadow such as "list --verbose" (bound bool) + "list" still
// correctly produces NURU_R003.
//
// The test hosts NuruGenerator in a CSharpGeneratorDriver over in-memory source and
// inspects GeneratorDriverRunResult.Diagnostics for the NURU_R003 diagnostic id.
// A RunAsync call is required so AppExtractor builds an AppModel and runs validation.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen30NuruR003Overlap
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using System.Linq;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class NuruR003OverlapTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<NuruR003OverlapTests>();

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
        assemblyName: "NuruR003OverlapRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    /// <summary>
    /// Unbound boolean flag discriminator: "list --all" and "list {filter?}" have
    /// different required signatures after 454-013, so the generator must not report
    /// NURU_R003.
    /// </summary>
    public static async Task Should_not_emit_nuru_r003_for_unbound_flag_discriminator()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("list --all").WithHandler(() => "all-items").AsQuery().Done()
          .Map("list {filter?}").WithHandler((string? filter) => $"filtered:{filter ?? "none"}").AsQuery().Done()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      bool hasR003 = result.Diagnostics.Any(d => d.Id == "NURU_R003")
        || result.Results.SelectMany(r => r.Diagnostics).Any(d => d.Id == "NURU_R003");
      hasR003.ShouldBeFalse();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Bound boolean flag remains optional at match time, so "list --verbose" (bool)
    /// and "list" reduce to the same required signature and NURU_R003 must still fire.
    /// </summary>
    public static async Task Should_still_emit_nuru_r003_for_genuine_shadow()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("list --verbose").WithHandler((bool verbose) => verbose ? "verbose-on" : "verbose-off").AsQuery().Done()
          .Map("list").WithHandler(() => "plain").AsQuery().Done()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      bool hasR003 = result.Diagnostics.Any(d => d.Id == "NURU_R003")
        || result.Results.SelectMany(r => r.Diagnostics).Any(d => d.Id == "NURU_R003");
      hasR003.ShouldBeTrue();

      await Task.CompletedTask;
    }
  }
}
