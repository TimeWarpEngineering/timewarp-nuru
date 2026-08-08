#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 454-028 item #7: HandlerValidator.DetectClosures walked
// lambda.Body.DescendantNodes(), which EXCLUDES the body node itself. So a handler whose
// entire body is a single captured identifier — `() => capturedLocal` — slipped through as
// a false NEGATIVE for NURU_H002 (closure detection), even though the same capture in any
// non-sole position (`() => capturedLocal + "z"`) was correctly flagged.
//
// Decision (see task 454-028 #7): the capture IS a closure the AOT path cannot emit (handler
// bodies are emitted verbatim), and constant LOCALS are already treated as closures in
// non-sole positions, so there is no "tolerated constant" behavior to preserve. Fix: walk
// DescendantNodesAndSelf so the sole-identifier body is analyzed consistently.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen38H002SoleIdentifierBody
{
  using System.Collections.Immutable;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class H002SoleIdentifierBodyTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<H002SoleIdentifierBodyTests>();

    private static ImmutableArray<Diagnostic> RunNuruGenerator(string handlerBody)
    {
      string source = $$"""
        using TimeWarp.Nuru;

        string runtimeLocal = "captured";

        NuruApp.CreateBuilder(args)
          .Map("greet").WithHandler(() => {{handlerBody}}).AsCommand().Done()
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
        assemblyName: "H002SoleBodyRepro",
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
    /// The regression: a handler whose entire body is a captured runtime local must now
    /// report NURU_H002 (previously silently missed).
    /// </summary>
    public static async Task Should_report_H002_for_sole_captured_identifier_body()
    {
      ImmutableArray<Diagnostic> diagnostics = RunNuruGenerator("runtimeLocal");

      diagnostics.Any(d => d.Id == "NURU_H002").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// A self-contained handler body (no capture) must NOT report NURU_H002.
    /// </summary>
    public static async Task Should_not_report_H002_for_sole_literal_body()
    {
      ImmutableArray<Diagnostic> diagnostics = RunNuruGenerator("\"hello\"");

      diagnostics.Any(d => d.Id == "NURU_H002").ShouldBeFalse();

      await Task.CompletedTask;
    }
  }
}
