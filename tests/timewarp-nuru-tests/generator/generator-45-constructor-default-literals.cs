#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression for kanban 470-006 (review 470 M8 and M21).
// Service constructor defaults must emit compiling literals: FormatLiteral for a
// string that contains a quote, a backslash, and a newline, and a fully-qualified
// enum member. FileInfo and DirectoryInfo conversions must catch Exception so
// PathTooLongException and NotSupportedException stay on the invalid-value path.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen45ConstructorDefaultLiterals
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class ConstructorDefaultLiteralTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ConstructorDefaultLiteralTests>();

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
        assemblyName: "ConstructorDefaultLiteralRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    private static string GeneratedSource(GeneratorDriverRunResult result)
    {
      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();
      return string.Join(
        "\n",
        result.Results[0].GeneratedSources.Select(g => g.SourceText.ToString()));
    }

    /// <summary>
    /// The inner C# source (parsed by the generator) is a regular string literal
    /// a\"b\\c\n. Raw-string content keeps those escapes for that parse.
    /// </summary>
    public static async Task Should_emit_escaped_string_and_qualified_enum_defaults()
    {
      const string Source = """
        using TimeWarp.Nuru;
        using Microsoft.Extensions.DependencyInjection;

        namespace Gen45Ns
        {
          public enum Gen45Mode
          {
            Dev = 0,
            Prod = 1
          }

          public sealed class Gen45Service
          {
            public Gen45Service(string label = "a\"b\\c\n", Gen45Mode mode = Gen45Mode.Prod)
            {
              Label = label;
              Mode = mode;
            }

            public string Label { get; }
            public Gen45Mode Mode { get; }

            public string Describe() => Label + ":" + Mode;
          }
        }

        NuruApp app = NuruApp.CreateBuilder(args)
          .ConfigureServices(services => services.AddSingleton<Gen45Ns.Gen45Service>())
          .Map("gen45")
            .WithHandler((Gen45Ns.Gen45Service svc) => svc.Describe())
            .AsQuery()
          .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      string generatedSource = GeneratedSource(result);

      // Value the constructor constant actually has, after the inner parse.
      string expectedLiteral = SymbolDisplay.FormatLiteral("a\"b\\c\n", quote: true);
      string expectedCall =
        $"new global::Gen45Ns.Gen45Service({expectedLiteral}, global::Gen45Ns.Gen45Mode.Prod)";
      generatedSource.ShouldContain(expectedCall);

      // An unqualified member name does not resolve in the generated file.
      generatedSource.ShouldNotContain("new global::Gen45Ns.Gen45Service(" + expectedLiteral + ", Prod)");

      await Task.CompletedTask;
    }

    public static async Task Should_catch_fileinfo_and_directoryinfo_constructor_exceptions()
    {
      const string Source = """
        using System.IO;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder(args)
          .Map("read {file:FileInfo}")
            .WithHandler((FileInfo file) => file.Name)
            .AsQuery()
          .Done()
          .Map("read-opt {file:FileInfo?}")
            .WithHandler((FileInfo? file) => file?.Name ?? "none")
            .AsQuery()
          .Done()
          .Map("list {dir:DirectoryInfo}")
            .WithHandler((DirectoryInfo dir) => dir.Name)
            .AsQuery()
          .Done()
          .Map("list-opt {dir:DirectoryInfo?}")
            .WithHandler((DirectoryInfo? dir) => dir?.Name ?? "none")
            .AsQuery()
          .Done()
          .Map("opt-file --input {file:FileInfo}")
            .WithHandler((FileInfo file) => file.Name)
            .AsQuery()
          .Done()
          .Map("opt-file-optional --input {file:FileInfo?}")
            .WithHandler((FileInfo? file) => file?.Name ?? "none")
            .AsQuery()
          .Done()
          .Map("opt-dir --folder {dir:DirectoryInfo}")
            .WithHandler((DirectoryInfo dir) => dir.Name)
            .AsQuery()
          .Done()
          .Map("opt-dir-optional --folder {dir:DirectoryInfo?}")
            .WithHandler((DirectoryInfo? dir) => dir?.Name ?? "none")
            .AsQuery()
          .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      string generatedSource = GeneratedSource(result);

      AssertConstructorCatch(generatedSource, "new global::System.IO.FileInfo(");
      AssertConstructorCatch(generatedSource, "new global::System.IO.DirectoryInfo(");

      await Task.CompletedTask;
    }

    private static void AssertConstructorCatch(string source, string construction)
    {
      int found = 0;
      int searchFrom = 0;
      while (true)
      {
        int at = source.IndexOf(construction, searchFrom, StringComparison.Ordinal);
        if (at < 0)
          break;

        found++;
        int windowEnd = Math.Min(source.Length, at + 500);
        string window = source[at..windowEnd];
        window.ShouldContain("catch (global::System.Exception)");
        window.ShouldNotContain("catch (global::System.ArgumentException)");
        searchFrom = at + construction.Length;
      }

      found.ShouldBe(4);
    }
  }
}
