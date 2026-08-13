#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 464 round-2 finding M5: DispatchWithExample's command argument
// must distinguish "not a string literal" from "literal but empty". A non-literal command
// (e.g. a const string reference, not a string literal token) must raise a diagnostic
// (NURU_S999, via the interpreter's generic exception-to-diagnostic fallback) rather than
// silently dropping the example - the fix for M2 (skip empty commands) must not also swallow
// this case. Mirrors generator-31's Roslyn-hosted harness for asserting generator diagnostics.
//
// This test also pins the fix for a related bug uncovered while writing it: an uncaught
// exception from ANY Dispatch* method (not just WithExample) aborts the whole Build() chain
// before .Build() itself is ever dispatched, so AppExtractor.ExtractFromBuildCall's "no model
// produced" fallback used to return ExtractionResult.Empty - silently discarding every
// diagnostic collected so far (including NURU_S999 here, and NURU_H005 from the sibling route
// below). The app then compiled with NO error/warning but crashed at runtime with
// "RunAsync was not intercepted" since no interceptor was ever emitted. Fixed by returning
// ExtractionResult.Failure(result.Diagnostics) instead - CreateGeneratorModelWithValidation
// (nuru-generator.cs) already collects and reports diagnostics from null-Model results, so this
// was the one broken link. See app-extractor.cs "No model produced" for the fix.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen40WithExampleNonLiteralCommand
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class WithExampleNonLiteralCommandTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<WithExampleNonLiteralCommandTests>();

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
        assemblyName: "WithExampleNonLiteralCommandRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    /// <summary>
    /// .WithExample(SomeHelper.ExampleCommand) where ExampleCommand is a const string reference
    /// (an IdentifierNameSyntax, not a LiteralExpressionSyntax) must raise NURU_S999 rather than
    /// being treated as an empty/absent command and silently skipped. A sibling route with an
    /// unrelated, locally-caught mismatch (NURU_H005, per generator-31's pattern) proves
    /// diagnostics from earlier in the chain survive the abort too - both must be reported.
    /// </summary>
    public static async Task Should_emit_nuru_s999_for_non_literal_example_command()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp.CreateBuilder(args)
          .Map("greet").WithHandler((string name) => $"Hello {name}").AsCommand().Done()
          .Map("status").WithHandler(() => "OK").WithExample(SomeHelper.ExampleCommand).AsCommand().Done()
          .Build();

        static class SomeHelper
        {
          public const string ExampleCommand = "status --verbose";
        }
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      Diagnostic[] s999Diagnostics = [.. result.Diagnostics.Where(d => d.Id == "NURU_S999")];
      s999Diagnostics.Length.ShouldBe(1);
      string message = s999Diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture);
      message.ShouldContain("WithExample");
      message.ShouldContain("command string literal");

      // The "greet" route's mismatch is caught locally (TryDoneRoute) before WithExample's
      // uncaught throw aborts the rest of the chain - its diagnostic must survive too.
      Diagnostic[] h005Diagnostics = [.. result.Diagnostics.Where(d => d.Id == "NURU_H005")];
      h005Diagnostics.Length.ShouldBe(1);

      await Task.CompletedTask;
    }
  }
}
