#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Kanban 528: an app that calls AddCheckUpdatesRoute() must compile against TimeWarp.Nuru's
// public surface. The generated FetchGitHubReleasesAsync named the internal
// CheckUpdatesJsonSerializerContext, so every consumer failed with CS0122. routing-21 never saw it
// because source/timewarp-nuru/internals-visible-to.g.cs grants InternalsVisibleTo to every test
// assembly name in the repo.
#endregion

#region Design
// The app compiles in-memory with Roslyn under an assembly name that no InternalsVisibleTo
// attribute on TimeWarp.Nuru lists, so internal access fails exactly as it does in a consumer.
// The test asserts that precondition first, then requires zero compile errors after generation.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen65CheckUpdatesConsumerNoIvt
{
  using System.Collections.Immutable;
  using System.Reflection;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class CheckUpdatesConsumerNoIvtTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<CheckUpdatesConsumerNoIvtTests>();

    private const string ConsumerAssemblyName = "Gen65ConsumerApp";

    private static ImmutableArray<Diagnostic> CompileWithGenerator(string source)
    {
      CSharpParseOptions parseOptions = CSharpParseOptions.Default
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "TimeWarp.Nuru.Generated")]);
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "app.cs");

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        // The analyzers assembly shares parsing types with timewarp-nuru (CS0433); a real app
        // loads it as an analyzer, not as a reference.
        if (Path.GetFileNameWithoutExtension(path) == "TimeWarp.Nuru.Analyzers")
          continue;
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: ConsumerAssemblyName,
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(
        generators: [new NuruGenerator().AsSourceGenerator()],
        parseOptions: parseOptions);
      _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);

      generatorDiagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
      output.SyntaxTrees.Count().ShouldBeGreaterThan(1);
      return output.GetDiagnostics();
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics) =>
      string.Join("\n", diagnostics.Select(static d => d.ToString()));

    public static async Task Consumer_assembly_has_no_internals_visible_to_grant()
    {
      string[] granted =
      [
        .. typeof(NuruApp).Assembly
          .GetCustomAttributes<System.Runtime.CompilerServices.InternalsVisibleToAttribute>()
          .Select(static attribute => attribute.AssemblyName)
      ];

      granted.ShouldNotBeEmpty();
      granted.ShouldNotContain(ConsumerAssemblyName);

      await Task.CompletedTask;
    }

    public static async Task Should_compile_check_updates_route_with_public_surface_only()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .AddCheckUpdatesRoute()
          .Map("gen65-ping").WithHandler(() => "pong").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      ImmutableArray<Diagnostic> diagnostics = CompileWithGenerator(Source);
      Diagnostic[] errors = [.. diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)];

      errors.ShouldBeEmpty(Describe(errors));

      await Task.CompletedTask;
    }

    public static async Task Parse_releases_reads_github_payload()
    {
      const string Json = """
        [
          { "tag_name": "v1.2.0", "prerelease": false, "published_at": "2026-10-01T12:00:00Z", "html_url": "https://github.com/o/r/releases/tag/v1.2.0" },
          { "tag_name": "v1.3.0-beta.1", "prerelease": true, "published_at": null, "html_url": "https://github.com/o/r/releases/tag/v1.3.0-beta.1" }
        ]
        """;

      CheckUpdatesGitHubRelease[] releases = CheckUpdatesGitHubRelease.ParseReleases(Json);

      releases.Length.ShouldBe(2);
      releases[0].TagName.ShouldBe("v1.2.0");
      releases[0].Prerelease.ShouldBeFalse();
      releases[0].PublishedAt.ShouldNotBeNull();
      releases[1].Prerelease.ShouldBeTrue();
      releases[1].PublishedAt.ShouldBeNull();
      CheckUpdatesGitHubRelease.ParseReleases("null").ShouldBeEmpty();

      await Task.CompletedTask;
    }
  }
}
