#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 491: HandlerValidator.DetectClosures skipped parameters by
// matching the handler lambda's own parameter NAMES, so a nested lambda's parameter —
// `i` in `(int[] ids) => ids.Sum(i => (long)i)` — resolved to an IParameterSymbol not in
// that set and was reported as a false NURU_H002 closure. The validator now uses symbol
// containment: a local or parameter declared inside the handler is never a capture.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen54H002NestedLambdaParameter
{
  using System.Collections.Immutable;
  using System.Globalization;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class H002NestedLambdaParameterTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<H002NestedLambdaParameterTests>();

    private static ImmutableArray<Diagnostic> RunNuruGenerator(string handler)
    {
      string source = $$"""
        using System.Linq;
        using TimeWarp.Nuru;

        long offset = 10;

        NuruApp.CreateBuilder(args)
          .Map("sum {*ids:int}").WithHandler({{handler}}).AsCommand().Done()
          .Build();
        """;

      SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "H002NestedLambdaRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      return CSharpGeneratorDriver.Create(new NuruGenerator())
        .RunGenerators(compilation)
        .GetRunResult()
        .Results[0]
        .Diagnostics;
    }

    /// <summary>
    /// The regression: a nested lambda's own parameter is not a capture.
    /// </summary>
    public static async Task Should_not_report_H002_for_nested_lambda_parameter()
    {
      ImmutableArray<Diagnostic> diagnostics = RunNuruGenerator("(int[] ids) => ids.Sum(i => (long)i)");

      diagnostics.Any(d => d.Id == "NURU_H002").ShouldBeFalse();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Locals declared inside the handler (including pattern variables) are not captures.
    /// </summary>
    public static async Task Should_not_report_H002_for_handler_locals_and_pattern_variables()
    {
      ImmutableArray<Diagnostic> diagnostics = RunNuruGenerator(
        "(int[] ids) => { long total = 0; foreach (int id in ids) { if (id is > 0 and int positive) total += positive; } return total; }");

      diagnostics.Any(d => d.Id == "NURU_H002").ShouldBeFalse();

      await Task.CompletedTask;
    }

    /// <summary>
    /// A genuine capture from the enclosing scope, even inside a nested lambda, still reports.
    /// </summary>
    public static async Task Should_still_report_H002_for_capture_inside_nested_lambda()
    {
      ImmutableArray<Diagnostic> diagnostics = RunNuruGenerator("(int[] ids) => ids.Sum(i => i + offset)");

      Diagnostic? h002 = diagnostics.FirstOrDefault(d => d.Id == "NURU_H002");
      h002.ShouldNotBeNull();
      h002.GetMessage(CultureInfo.InvariantCulture).ShouldContain("variable(s): offset.");

      await Task.CompletedTask;
    }
  }
}
