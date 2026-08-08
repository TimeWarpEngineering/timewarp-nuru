#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 454-004: the NURU_H002 closure detector walked every
// IdentifierNameSyntax in a handler lambda but did not skip the name of a NameColon
// (named argument) or NameEquals (anonymous type member / attribute argument). In
// `(string name) => Console.WriteLine(format: name)` the identifier `format` resolves
// to WriteLine's parameter symbol, which the detector treated as a captured outer
// variable -> false NURU_H002 (Error severity) on valid code, blocking the build.
// Property-pattern names (`s is { Length: > 0 }`) hit the same hole via NameColon.
//
// Runs NuruGenerator in a CSharpGeneratorDriver (standalone CI phase; see
// generator-28 for why this cannot compile into the multi-mode assembly).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen29H002NamedArguments
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class H002NamedArgumentTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<H002NamedArgumentTests>();

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
        assemblyName: "H002Repro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    /// <summary>
    /// The review's exact repro: a named argument whose name matches the callee's
    /// parameter. Must NOT report NURU_H002.
    /// </summary>
    public static async Task Should_not_report_H002_for_named_argument()
    {
      const string Source = """
        using System;
        using TimeWarp.Nuru;

        NuruApp.CreateBuilder(args)
          .Map("show {name}").WithHandler((string name) => Console.WriteLine(format: name)).AsCommand().Done()
          .Build();
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results[0].Exception.ShouldBeNull();
      result.Diagnostics.Any(d => d.Id == "NURU_H002")
        .ShouldBeFalse("named argument 'format:' is not a closure");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Property-pattern names bind to the matched type's members via NameColon —
    /// also not captures.
    /// </summary>
    public static async Task Should_not_report_H002_for_property_pattern()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp.CreateBuilder(args)
          .Map("check {value}").WithHandler((string value) => value is { Length: > 3 } ? "long" : "short").AsQuery().Done()
          .Build();
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results[0].Exception.ShouldBeNull();
      result.Diagnostics.Any(d => d.Id == "NURU_H002")
        .ShouldBeFalse("property pattern name 'Length' is not a closure");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Anonymous-type member names (NameEquals) are not captures.
    /// </summary>
    public static async Task Should_not_report_H002_for_anonymous_type_member()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp.CreateBuilder(args)
          .Map("tag {name}").WithHandler((string name) => new { Tag = name }.ToString()).AsQuery().Done()
          .Build();
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results[0].Exception.ShouldBeNull();
      result.Diagnostics.Any(d => d.Id == "NURU_H002")
        .ShouldBeFalse("anonymous type member name 'Tag' is not a closure");

      await Task.CompletedTask;
    }

    /// <summary>
    /// A genuine capture must still be reported — the fix must not weaken detection.
    /// (The captured identifier is nested in an expression because DetectClosures walks
    /// Body.DescendantNodes(), which misses a body that IS a lone identifier — a known
    /// false negative, noted in kanban 454-004.)
    /// </summary>
    public static async Task Should_still_report_H002_for_genuine_closure()
    {
      const string Source = """
        using TimeWarp.Nuru;

        string greeting = "hello";

        NuruApp.CreateBuilder(args)
          .Map("greet").WithHandler(() => greeting + "!").AsQuery().Done()
          .Build();
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results[0].Exception.ShouldBeNull();
      result.Diagnostics.Any(d => d.Id == "NURU_H002")
        .ShouldBeTrue("capturing the outer local 'greeting' is a real closure");

      await Task.CompletedTask;
    }
  }
}
