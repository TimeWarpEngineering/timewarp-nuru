#!/usr/bin/dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 454-012 (M8): isRepeated must be decided via ITypeSymbol
// inspection (endpoint-extractor.cs), not typeName.Contains("IList")/Contains("IEnumerable").
// A user-defined interface like IListManager (starts with "IList" but implements no
// collection interface) was previously misclassified as a repeated option.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen34IListManagerRepeated
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class IListManagerRepeatedTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<IListManagerRepeatedTests>();

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
        assemblyName: "IListManagerRepeatedRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    /// <summary>
    /// An [Option] property typed as a user interface named IListManager (not a
    /// collection) must NOT be classified as a repeated option, while a real string[]
    /// option on the same endpoint must still be classified as repeated.
    /// </summary>
    public static async Task Should_not_treat_user_ilistmanager_as_repeated()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder(args)
          .Map<Gen34App.IListManagerCommand>()
          .Build();

        return await app.RunAsync(args);

        namespace Gen34App
        {
          public interface IListManager
          {
            void Manage();
          }

          [NuruRoute("m8-ilistmanager", Description = "454-012 IListManager repro")]
          public sealed class IListManagerCommand : ICommand<Unit>
          {
            [Option("manager", Description = "User type that is NOT a collection")]
            public IListManager? Manager { get; set; }

            [Option("tags", Description = "A real repeated option")]
            public string[] Tags { get; set; } = [];

            public sealed class Handler : ICommandHandler<IListManagerCommand, Unit>
            {
              public ValueTask<Unit> Handle(IListManagerCommand command, CancellationToken ct) => default;
            }
          }
        }
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      // No error-severity diagnostics: extraction must succeed cleanly.
      Diagnostic[] errors = [.. result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)];
      errors.ShouldBeEmpty();

      string generatedSource = string.Join(
        "\n",
        result.Results[0].GeneratedSources.Select(g => g.SourceText.ToString()));

      // Real repeated option: emitter generates a "..._list_" accumulator for it.
      generatedSource.ShouldContain("tags_list");

      // Bug repro: IListManager must NOT trigger the repeated-option code path.
      generatedSource.ShouldNotContain("manager_list");

      await Task.CompletedTask;
    }
  }
}
