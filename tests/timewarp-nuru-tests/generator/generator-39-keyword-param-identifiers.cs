#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 460: route params named C# keywords/contextual keywords
// ({event}, {when:DateTime}, {class}, {ref}, {value}) must be emitted with @-escaped
// identifiers so generated interceptor code compiles.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen39KeywordParamIdentifiers
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class KeywordParamIdentifierTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<KeywordParamIdentifierTests>();

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
        assemblyName: "KeywordParamIdentifiersRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result)
    {
      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();
      return string.Join(
        "\n",
        result.Results[0].GeneratedSources.Select(g => g.SourceText.ToString()));
    }

    /// <summary>
    /// schedule {event} {when:DateTime} with (string @event, DateTime when) must emit
    /// escaped identifiers: string @event and DateTime @when — never bare "event =" or
    /// bare type-like "when" misuse that fails to compile.
    /// </summary>
    public static async Task Should_escape_event_and_when_route_params()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder(args)
          .Map("schedule {event} {when:DateTime}")
            .WithHandler((string @event, DateTime when) => $"{@event}@{when:o}")
            .AsCommand()
          .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      string generatedSource = GetGeneratedSource(RunNuruGenerator(Source));

      // Untyped keyword param: capture variable must be @event
      generatedSource.ShouldContain("string @event =");
      generatedSource.ShouldNotContain("string event =");

      // Typed contextual keyword: converted variable must be @when
      generatedSource.ShouldContain("DateTime @when");
      // Local function signature should also escape
      generatedSource.ShouldContain("string @event");
      generatedSource.ShouldContain("DateTime @when");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Reserved keywords class/ref/for must be @-escaped when used as untyped
    /// positional param names. (SyntaxFacts does not treat "value" as always-keyword;
    /// bare "value" is a legal identifier and need not be escaped.)
    /// </summary>
    public static async Task Should_escape_class_ref_and_for_route_params()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder(args)
          .Map("demo {class} {ref} {for}")
            .WithHandler((string @class, string @ref, string @for) => $"{@class}:{@ref}:{@for}")
            .AsQuery()
          .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      string generatedSource = GetGeneratedSource(RunNuruGenerator(Source));

      generatedSource.ShouldContain("string @class =");
      generatedSource.ShouldContain("string @ref =");
      generatedSource.ShouldContain("string @for =");

      generatedSource.ShouldNotContain("string class =");
      generatedSource.ShouldNotContain("string ref =");
      generatedSource.ShouldNotContain("string for =");

      await Task.CompletedTask;
    }
  }
}
