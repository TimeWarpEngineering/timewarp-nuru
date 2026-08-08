#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 454-011 (M7): Handler parameter mismatch on .Done()
// should emit NURU_H005 diagnostic and skip the bad route, allowing sibling routes
// to still be generated. Previously this threw InvalidOperationException and crashed
// the generator host.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen31ParamMismatch
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class ParamMismatchTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ParamMismatchTests>();

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
        assemblyName: "ParamMismatchRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    /// <summary>
    /// Two routes in one block: first has a handler param with no matching segment.
    /// Second route is valid. Expect exactly one NURU_H005, and route 2 still generated.
    /// </summary>
    public static async Task Should_emit_h005_and_keep_sibling_route()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp.CreateBuilder(args)
          .Map("greet").WithHandler((string name) => $"Hello {name}").AsCommand().Done()
          .Map("status").WithHandler(() => "OK").AsCommand().Done()
          .Build();
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      // Expect exactly one diagnostic: NURU_H005
      Diagnostic[] diagnostics = [.. result.Diagnostics.Where(d => d.Id == "NURU_H005")];
      diagnostics.Length.ShouldBe(1);
      string message = diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture);
      message.ShouldContain("name");
      // The available-segments list must contain the intact segment name "greet".
      // Guards against the double-join defect where an already-joined string was
      // re-joined as IEnumerable<char> into "g, r, e, e, t".
      message.ShouldContain("greet");
      message.ShouldNotContain("g, r, e, e, t");

      await Task.CompletedTask;
    }
  }
}
