#!/usr/bin/dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Cacheability test for kanban 454-010 (M4/M5 + Location-stripping): prove the heavy
// emit stage no longer re-runs on every keystroke.
//
// The generator pipeline is instrumented with WithTrackingName markers:
//   - "NuruGeneratorModel" — the equatable GeneratorModel that feeds emit.
//   - "NuruEnumInfo"       — the precomputed, equatable enum member set (replaces the
//                            live Compilation that used to force emit every edit).
//
// This hosts NuruGenerator in a CSharpGeneratorDriver with
// trackIncrementalGeneratorSteps enabled, runs it twice over a two-file compilation
// (A = a Nuru app with an enum route parameter, B = unrelated code), edits ONLY the
// unrelated file B between runs, and asserts every output of those two steps is
// Cached or Unchanged. Before 454-010 these reported Modified/New on every edit
// because the model carried ImmutableArray reference-equality (M5), a raw
// CompilationProvider (M4), and live Roslyn Location objects.
//
// CI cannot observe this via generated output (the output is identical either way);
// only the step-reason assert catches a caching regression.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen37IncrementalityCaching
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class IncrementalityCachingTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<IncrementalityCachingTests>();

    // A: a Nuru app whose handler binds an enum parameter, so the enum resolution path
    // (NuruEnumInfo) is genuinely exercised.
    private const string AppSource = """
      using TimeWarp.Nuru;

      NuruApp.CreateBuilder(args)
        .Map("deploy {env}").WithHandler((DeployEnv env) => $"deploy {env}").AsCommand().Done()
        .Build();

      enum DeployEnv { Dev, Staging, Prod }
      """;

    // B: unrelated code with no Nuru surface. The literal is swapped between runs so the
    // second run is a real edit to B that changes nothing about the app in A.
    private static string UnrelatedSource(string marker) => $$"""
      namespace Unrelated
      {
        public static class Helper
        {
          public static string Tag => "{{marker}}";
        }
      }
      """;

    private static List<MetadataReference> BuildReferences()
    {
      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        references.Add(MetadataReference.CreateFromFile(path));
      }

      return references;
    }

    private static CSharpGeneratorDriver CreateTrackingDriver() =>
      CSharpGeneratorDriver.Create(
        generators: [new NuruGenerator().AsSourceGenerator()],
        driverOptions: new GeneratorDriverOptions(
          IncrementalGeneratorOutputKind.None,
          trackIncrementalGeneratorSteps: true));

    /// <summary>
    /// Editing an unrelated file must not re-run the emit model or enum-info steps.
    /// </summary>
    public static async Task Model_and_enum_info_cache_on_unrelated_edit()
    {
      SyntaxTree appTree = CSharpSyntaxTree.ParseText(AppSource);
      SyntaxTree unrelatedV1 = CSharpSyntaxTree.ParseText(UnrelatedSource("v1"));

      CSharpCompilation compilation1 = CSharpCompilation.Create(
        assemblyName: "NuruIncrementalRepro",
        syntaxTrees: [appTree, unrelatedV1],
        references: BuildReferences(),
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      GeneratorDriver driver = CreateTrackingDriver();

      // Run 1 — populates the incremental caches.
      driver = driver.RunGenerators(compilation1);

      // Run 2 — edit ONLY the unrelated tree; the app in A is byte-identical.
      SyntaxTree unrelatedV2 = CSharpSyntaxTree.ParseText(UnrelatedSource("v2"));
      Compilation compilation2 = compilation1.ReplaceSyntaxTree(unrelatedV1, unrelatedV2);
      driver = driver.RunGenerators(compilation2);

      GeneratorRunResult runResult = driver.GetRunResult().Results[0];
      runResult.Exception.ShouldBeNull();

      AssertStepsCachedOrUnchanged(runResult, "NuruGeneratorModel");
      AssertStepsCachedOrUnchanged(runResult, "NuruEnumInfo");

      await Task.CompletedTask;
    }

    /// <summary>
    /// A cosmetic edit AFTER all Nuru code in the app file re-parses tree A (so its node
    /// transforms re-run) but leaves every emit-relevant value — including registration
    /// spans — unchanged. This exercises the EquatableArray value equality (M5) and the
    /// Location-stripping (3c): the model must still compare equal and cache.
    /// </summary>
    public static async Task Model_caches_on_cosmetic_edit_to_app_file()
    {
      SyntaxTree unrelatedTree = CSharpSyntaxTree.ParseText(UnrelatedSource("stable"));
      SyntaxTree appV1 = CSharpSyntaxTree.ParseText(AppSource);

      CSharpCompilation compilation1 = CSharpCompilation.Create(
        assemblyName: "NuruIncrementalReproCosmetic",
        syntaxTrees: [appV1, unrelatedTree],
        references: BuildReferences(),
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      GeneratorDriver driver = CreateTrackingDriver();
      driver = driver.RunGenerators(compilation1);

      // Append a trailing comment line AFTER all app code — spans of Build()/Map()/handler
      // are unchanged, so the emit model should be value-equal to run 1.
      SyntaxTree appV2 = CSharpSyntaxTree.ParseText(AppSource + "\n// cosmetic trailing edit\n");
      Compilation compilation2 = compilation1.ReplaceSyntaxTree(appV1, appV2);
      driver = driver.RunGenerators(compilation2);

      GeneratorRunResult runResult = driver.GetRunResult().Results[0];
      runResult.Exception.ShouldBeNull();

      AssertStepsCachedOrUnchanged(runResult, "NuruGeneratorModel");
      AssertStepsCachedOrUnchanged(runResult, "NuruEnumInfo");

      await Task.CompletedTask;
    }

    private static void AssertStepsCachedOrUnchanged(GeneratorRunResult runResult, string trackingName)
    {
      runResult.TrackedSteps.ContainsKey(trackingName).ShouldBeTrue();

      List<string> offenders = [];
      foreach (IncrementalGeneratorRunStep step in runResult.TrackedSteps[trackingName])
      {
        foreach ((object Value, IncrementalStepRunReason Reason) output in step.Outputs)
        {
          if (output.Reason is not (IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged))
            offenders.Add($"{trackingName} -> {output.Reason}");
        }
      }

      // Empty string on success; on failure the diff surfaces the offending step reasons.
      string.Join(", ", offenders).ShouldBe(string.Empty);
    }
  }
}
