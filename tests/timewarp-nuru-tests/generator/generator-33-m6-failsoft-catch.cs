#!/usr/bin/dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 454-011 (M6): Fail-soft catch blocks must swallow
// arbitrary exceptions (not just InvalidOperationException) so that partial/broken
// syntax that would throw NullReferenceException/ArgumentException does not crash
// the analyzer host with AD0001.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen33FailsoftCatch
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class FailsoftCatchTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<FailsoftCatchTests>();

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
        assemblyName: "FailsoftCatchRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    /// <summary>
    /// Broken syntax that would previously throw inside evaluation should now
    /// produce a soft diagnostic instead of an AD0001 analyzer crash.
    /// </summary>
    public static async Task Should_swallow_arbitrary_exceptions()
    {
      // Intentionally malformed: .Map() without pattern, then a handler reference
      // that cannot be resolved — triggers evaluation path that throws ArgumentException.
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp.CreateBuilder(args)
          .Map().WithHandler(() => "broken").AsCommand().Done()
          .Build();
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results.Length.ShouldBe(1);

      // Generator must complete without throwing (no AD0001)
      result.Results[0].Exception.ShouldBeNull();

      await Task.CompletedTask;
    }
  }
}
