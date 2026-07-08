#!/usr/bin/dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 454-011 (M9): Unrelated fluent calls on non-Nuru types
// (e.g., x.WithDescription("...")) must be ignored, not turned into NURU_S999.
// The IsDslBuilderMethod guard prevents unrelated builder methods from being dispatched.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen32UnrelatedFluent
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class UnrelatedFluentTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<UnrelatedFluentTests>();

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
        assemblyName: "UnrelatedFluentRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    /// <summary>
    /// Unrelated WithDescription/WithAlias/WithGroupPrefix on a non-Nuru object
    /// must not produce NURU_S999 and must not affect real routes.
    /// </summary>
    public static async Task Should_ignore_unrelated_fluent_calls()
    {
      const string Source = """
        using TimeWarp.Nuru;

        var x = new object();

        // These are NOT Nuru DSL calls — should be silently ignored
        x.WithDescription("ignored");
        x.WithAlias("ignored");
        x.WithGroupPrefix("ignored");

        NuruApp.CreateBuilder(args)
          .Map("status").WithHandler(() => "OK").AsCommand().Done()
          .Build();
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      // No NURU_S999 (unrecognized DSL method) should be emitted
      result.Diagnostics.Any(d => d.Id == "NURU_S999").ShouldBeFalse();

      await Task.CompletedTask;
    }
  }
}
