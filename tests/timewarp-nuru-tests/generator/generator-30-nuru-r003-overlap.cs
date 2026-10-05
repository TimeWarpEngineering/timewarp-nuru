#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 454-013: unbound boolean flags are route discriminators
// (required to match), so "list --all" + "list {filter?}" must NOT produce a false
// NURU_R003 unreachable-route warning. Bound boolean flags remain optional at match
// time, so a genuine shadow such as "list --verbose" (bound bool) + "list" still
// correctly produces NURU_R003.
//
// The test hosts NuruGenerator in a CSharpGeneratorDriver over in-memory source and
// inspects GeneratorDriverRunResult.Diagnostics for the NURU_R003 diagnostic id.
// A RunAsync call is required so AppExtractor builds an AppModel and runs validation.
//
// Kanban 482-007: optional parameters render as {name?} / {name:type?} in diagnostic
// text (not [name]), and overlap/duplicate diagnostics on optional routes anchor at the
// Map(...) string instead of Location.None.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen30NuruR003Overlap
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using System.Linq;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class NuruR003OverlapTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<NuruR003OverlapTests>();

    /// <summary>
    /// Runs NuruGenerator over the given source with the test process's own
    /// assemblies as metadata references (includes TimeWarp.Nuru).
    /// </summary>
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
        assemblyName: "NuruR003OverlapRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    /// <summary>
    /// Unbound boolean flag discriminator: "list --all" and "list {filter?}" have
    /// different required signatures after 454-013, so the generator must not report
    /// NURU_R003.
    /// </summary>
    public static async Task Should_not_emit_nuru_r003_for_unbound_flag_discriminator()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("list --all").WithHandler(() => "all-items").AsQuery().Done()
          .Map("list {filter?}").WithHandler((string? filter) => $"filtered:{filter ?? "none"}").AsQuery().Done()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      bool hasR003 = result.Diagnostics.Any(d => d.Id == "NURU_R003")
        || result.Results.SelectMany(r => r.Diagnostics).Any(d => d.Id == "NURU_R003");
      hasR003.ShouldBeFalse();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Bound boolean flag remains optional at match time, so "list --verbose" (bool)
    /// and "list" reduce to the same required signature and NURU_R003 must still fire.
    /// </summary>
    public static async Task Should_still_emit_nuru_r003_for_genuine_shadow()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("list --verbose").WithHandler((bool verbose) => verbose ? "verbose-on" : "verbose-off").AsQuery().Done()
          .Map("list").WithHandler(() => "plain").AsQuery().Done()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      bool hasR003 = result.Diagnostics.Any(d => d.Id == "NURU_R003")
        || result.Results.SelectMany(r => r.Diagnostics).Any(d => d.Id == "NURU_R003");
      hasR003.ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Returns every diagnostic with the given id from the generator run.
    /// </summary>
    private static List<Diagnostic> DiagnosticsWithId(GeneratorDriverRunResult result, string id) =>
      [.. result.Diagnostics.Concat(result.Results.SelectMany(r => r.Diagnostics)).Where(d => d.Id == id).Distinct()];

    /// <summary>
    /// Duplicate optional routes: NURU_R002 must squiggle the Map(...) string and render
    /// the pattern as {tag?}, which PatternParser accepts.
    /// </summary>
    public static async Task Should_anchor_nuru_r002_at_optional_route_string_with_parseable_pattern()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => "first").AsCommand().Done()
          .Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => "second").AsCommand().Done()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      List<Diagnostic> r002 = DiagnosticsWithId(result, "NURU_R002");

      r002.Count.ShouldBeGreaterThan(0);
      Diagnostic diagnostic = r002[0];
      diagnostic.Location.IsInSource.ShouldBeTrue();
      (await diagnostic.Location.SourceTree!.GetTextAsync()).ToString(diagnostic.Location.SourceSpan)
        .ShouldContain("deploy {env} {tag?}");

      string message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
      message.ShouldContain("deploy {env} {tag?}");
      message.ShouldNotContain("[tag]");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Two routes that overlap on an optional parameter with different type constraints:
    /// NURU_R001 must anchor at the source route string and render {tag?} / {tag:int?}.
    /// </summary>
    public static async Task Should_anchor_nuru_r001_overlap_on_optional_tag()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder([])
          .Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => "text").AsCommand().Done()
          .Map("deploy {env} {tag:int?}").WithHandler((string env, int? tag) => "number").AsCommand().Done()
          .Build();

        app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);
      List<Diagnostic> overlap = [.. DiagnosticsWithId(result, "NURU_R001"), .. DiagnosticsWithId(result, "NURU_R003")];

      overlap.Count.ShouldBeGreaterThan(0);
      foreach (Diagnostic diagnostic in overlap)
      {
        diagnostic.Location.IsInSource.ShouldBeTrue();
        (await diagnostic.Location.SourceTree!.GetTextAsync()).ToString(diagnostic.Location.SourceSpan)
          .ShouldContain("deploy {env} {tag");

        string message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.ShouldNotContain("[tag");
        (message.Contains("{tag?}", StringComparison.Ordinal) || message.Contains("{tag:int?}", StringComparison.Ordinal))
          .ShouldBeTrue();
      }

      await Task.CompletedTask;
    }

    /// <summary>
    /// PatternSyntax renders optional parameters in PatternParser form ({name?} and
    /// {name:type?}, as covered by parser-03), never the rejected [name] form.
    /// </summary>
    public static async Task Should_render_optional_parameter_pattern_syntax_in_parser_form()
    {
      ParameterDefinition untyped = new(0, "tag", null, null, IsOptional: true, IsCatchAll: false, null);
      ParameterDefinition typedSuffix = new(0, "tag", "int?", null, IsOptional: true, IsCatchAll: false, null);
      ParameterDefinition typedPrefix = new(0, "tag", "int", null, IsOptional: true, IsCatchAll: false, null);

      untyped.PatternSyntax.ShouldBe("{tag?}");
      typedSuffix.PatternSyntax.ShouldBe("{tag:int?}");
      typedPrefix.PatternSyntax.ShouldBe("{tag:int?}");

      await Task.CompletedTask;
    }
  }
}
