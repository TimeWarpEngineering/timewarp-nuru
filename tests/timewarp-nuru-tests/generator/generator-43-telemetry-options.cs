#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Kanban 470-001: Roslyn-hosted check that UseTelemetry(Action<>) options appear in generated setup.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen43TelemetryOptions
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  [TestTag("Telemetry")]
  public class TelemetryOptionsGeneratorTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TelemetryOptionsGeneratorTests>();

    private static GeneratorDriverRunResult RunNuruGenerator(string source)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "app.cs");

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Tel43App",
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
      return string.Join("\n", result.Results[0].GeneratedSources.Select(static g => g.SourceText.ToString()));
    }

    public static async Task Should_emit_service_name_and_otlp_endpoint_literals()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseTelemetry(options =>
          {
            options.ServiceName = "tel43-cli";
            options.OtlpEndpoint = "http://127.0.0.1:4318";
            options.ServiceVersion = "9.9.9";
          })
          .Map("tel43-ping").WithHandler(() => "pong").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      string generated = GeneratedSource(RunNuruGenerator(Source));

      generated.ShouldContain("tel43-cli");
      generated.ShouldContain("http://127.0.0.1:4318");
      generated.ShouldContain("9.9.9");
      generated.ShouldContain("app.TracerProvider");
      generated.ShouldContain("app.MeterProvider");

      await Task.CompletedTask;
    }

    public static async Task Should_omit_tracer_setup_when_tracing_disabled()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseTelemetry(options =>
          {
            options.OtlpEndpoint = "http://127.0.0.1:4318";
            options.EnableTracing = false;
            options.EnableMetrics = true;
            options.EnableLogging = false;
          })
          .Map("tel43-metrics").WithHandler(() => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      string generated = GeneratedSource(RunNuruGenerator(Source));

      generated.ShouldContain("app.MeterProvider");
      generated.Contains("app.TracerProvider").ShouldBeFalse();
      generated.Contains("ActivitySource").ShouldBeFalse();

      await Task.CompletedTask;
    }
  }
}
