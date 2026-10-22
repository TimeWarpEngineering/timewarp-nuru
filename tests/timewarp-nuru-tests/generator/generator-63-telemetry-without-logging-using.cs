#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Kanban 521: a UseTelemetry() app must compile with no Microsoft.Extensions.Logging using in scope.
// The generated logging setup used to call ILoggingBuilder extension methods by instance syntax,
// which only the app's own usings could resolve (CS1061). tests/Directory.Build.props adds a global
// Microsoft.Extensions.Logging using to every test project and hid the bug, so this test compiles
// the app in-memory with Roslyn, where only the usings in the source text below are in scope.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen63TelemetryWithoutLoggingUsing
{
  using System.Collections.Immutable;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  [TestTag("Telemetry")]
  public class TelemetryWithoutLoggingUsingTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TelemetryWithoutLoggingUsingTests>();

    private static ImmutableArray<Diagnostic> CompileWithGenerator(string source)
    {
      CSharpParseOptions parseOptions = CSharpParseOptions.Default
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "TimeWarp.Nuru.Generated")]);
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "app.cs");

      // The TPA list carries the OpenTelemetry and Microsoft.Extensions.Logging assemblies that
      // timewarp-nuru references, so the only thing missing is a using, as in a user's app.
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
        assemblyName: "Tel63App",
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

    public static async Task Should_compile_logging_setup_without_a_logging_using()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseTelemetry(options =>
          {
            options.ServiceName = "tel63-cli";
            options.OtlpEndpoint = "http://127.0.0.1:4318";
          })
          .Map("tel63-ping").WithHandler(() => "pong").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      ImmutableArray<Diagnostic> diagnostics = CompileWithGenerator(Source);
      Diagnostic[] errors = [.. diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)];

      errors.ShouldBeEmpty(Describe(errors));

      await Task.CompletedTask;
    }

    public static async Task Should_compile_default_telemetry_setup_without_a_logging_using()
    {
      // No options: the endpoint comes from OTEL_EXPORTER_OTLP_ENDPOINT, and tracing, metrics
      // and logging setup are all emitted.
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseTelemetry()
          .Map("tel63-default").WithHandler(() => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      ImmutableArray<Diagnostic> diagnostics = CompileWithGenerator(Source);
      Diagnostic[] errors = [.. diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)];

      errors.ShouldBeEmpty(Describe(errors));

      await Task.CompletedTask;
    }
  }
}
