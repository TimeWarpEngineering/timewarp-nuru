#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

// NURU_H008: an [Operation] with no visible handler is an error, and the app
// fails at startup when that handler is still unregistered.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen55MissingOperationHandler
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using System.Linq;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class MissingOperationHandlerTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MissingOperationHandlerTests>();

    private static GeneratorDriverRunResult RunNuruGenerator(string source)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        if (path.Length > 0)
          references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "NuruH008MissingHandler",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    private static bool HasH008(GeneratorDriverRunResult result) =>
      result.Diagnostics.Any(diagnostic => diagnostic.Id == "NURU_H008")
      || result.Results.SelectMany(generatorResult => generatorResult.Diagnostics).Any(diagnostic => diagnostic.Id == "NURU_H008");

    private static string GeneratedText(GeneratorDriverRunResult result) =>
      string.Join("\n", result.Results.SelectMany(generator => generator.GeneratedSources).Select(source => source.SourceText.ToString()));

    public static async Task Should_emit_nuru_h008_when_an_operation_has_no_visible_handler()
    {
      const string Source = """
        using TimeWarp.Contracts;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [Operation("t508missing")]
        public sealed class T508MissingQuery : IQuery<string>
        {
        }

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      HasH008(result).ShouldBeTrue();
      GeneratedText(result).ShouldContain("No handler is registered for");
      await Task.CompletedTask;
    }

    public static async Task Should_not_emit_nuru_h008_when_a_handler_is_visible()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Contracts;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        [Operation("t508present")]
        public sealed class T508PresentQuery : IQuery<string>
        {
        }

        public sealed class T508PresentHandler : IQueryHandler<T508PresentQuery, string>
        {
          public Task<string> Handle(T508PresentQuery query, CancellationToken cancellationToken) =>
            Task.FromResult("ok");
        }

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        app.RunAsync([]);
        """;

      HasH008(RunNuruGenerator(Source)).ShouldBeFalse();
      await Task.CompletedTask;
    }
  }
}
