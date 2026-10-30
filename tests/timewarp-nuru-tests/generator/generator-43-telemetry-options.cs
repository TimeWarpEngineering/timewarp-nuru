#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Kanban 470-001: Roslyn-hosted check that UseTelemetry(Action<>) options appear in generated setup.
// Kanban 551: every OTLP exporter resolves its endpoint through NuruOtlpEndpoint.Configure, so
// the generated code never sets o.Endpoint to the base URL (which broke /v1/{signal} for HTTP).
// Kanban 561: ActivitySources.Add / Meters.Add (literal or const) emit one AddSource / AddMeter
// each after Nuru's own; with none configured only Nuru's two of each are emitted.
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

    public static async Task Should_resolve_code_endpoint_per_signal()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseTelemetry(options => options.OtlpEndpoint = "https://otlp.example.test/")
          .Map("tel43-code").WithHandler(() => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      string generated = GeneratedSource(RunNuruGenerator(Source));

      generated.ShouldContain("string? __otlpEndpoint = \"https://otlp.example.test/\";");
      generated.ShouldContain("string? __otlpCodeEndpoint = __otlpEndpoint;");
      AssertConfigurePerSignal(generated);

      await Task.CompletedTask;
    }

    public static async Task Should_leave_env_endpoint_to_sdk_when_not_set_in_code()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseTelemetry()
          .Map("tel43-env").WithHandler(() => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      string generated = GeneratedSource(RunNuruGenerator(Source));

      generated.ShouldContain("GetEnvironmentVariable(\"OTEL_EXPORTER_OTLP_ENDPOINT\")");
      generated.ShouldContain("string? __otlpCodeEndpoint = null;");
      AssertConfigurePerSignal(generated);

      await Task.CompletedTask;
    }

    private static void AssertConfigurePerSignal(string generated)
    {
      // Before kanban 551 every exporter got o => o.Endpoint = __endpoint (the base URL).
      generated.Contains("o.Endpoint = __endpoint").ShouldBeFalse();
      generated.Contains("__endpoint = new(").ShouldBeFalse();
      foreach (string signal in new[] { "Traces", "Metrics", "Logs" })
      {
        generated.ShouldContain(
          "o => global::TimeWarp.Nuru.NuruOtlpEndpoint.Configure(o, __otlpCodeEndpoint, "
          + $"global::TimeWarp.Nuru.NuruOtlpEndpoint.{signal})");
      }
    }

    public static async Task Should_emit_add_source_and_add_meter_per_configured_name()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseTelemetry(options =>
          {
            options.ActivitySources.Add("TimeWarp.Taratibu");
            options.ActivitySources.Add(Tel43Names.WalkSource);
            options.ActivitySources.Add("TimeWarp.Taratibu");
            options.Meters.Add("Tel43.Meter");
          })
          .Map("tel43-sources").WithHandler(() => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);

        static class Tel43Names
        {
          public const string WalkSource = "Tel43.Walk";
        }
        """;

      string generated = GeneratedSource(RunNuruGenerator(Source));

      AddCalls(generated, "AddSource").ShouldBe(
      [
        "__tracerBuilder.AddSource(__activitySource.Name)",
        "__tracerBuilder.AddSource(\"TimeWarp.Nuru.Behavior\")",
        "__tracerBuilder.AddSource(\"TimeWarp.Taratibu\")",
        "__tracerBuilder.AddSource(\"Tel43.Walk\")",
      ]);
      AddCalls(generated, "AddMeter").ShouldBe(
      [
        "__meterBuilder.AddMeter(__meter.Name)",
        "__meterBuilder.AddMeter(\"TimeWarp.Nuru.Behavior\")",
        "__meterBuilder.AddMeter(\"Tel43.Meter\")",
      ]);

      await Task.CompletedTask;
    }

    public static async Task Should_emit_only_nuru_sources_when_none_configured()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseTelemetry(options => options.ServiceName = "tel43-plain")
          .Map("tel43-plain").WithHandler(() => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      string generated = GeneratedSource(RunNuruGenerator(Source));

      AddCalls(generated, "AddSource").ShouldBe(
      [
        "__tracerBuilder.AddSource(__activitySource.Name)",
        "__tracerBuilder.AddSource(\"TimeWarp.Nuru.Behavior\")",
      ]);
      AddCalls(generated, "AddMeter").ShouldBe(
      [
        "__meterBuilder.AddMeter(__meter.Name)",
        "__meterBuilder.AddMeter(\"TimeWarp.Nuru.Behavior\")",
      ]);

      await Task.CompletedTask;
    }

    public static async Task Should_skip_extra_sources_when_tracing_disabled()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseTelemetry(options =>
          {
            options.EnableTracing = false;
            options.ActivitySources.Add("Tel43.Skipped");
            options.Meters.Add("Tel43.Kept");
          })
          .Map("tel43-skip").WithHandler(() => "ok").AsQuery().Done()
          .Build();
        return await app.RunAsync([]);
        """;

      string generated = GeneratedSource(RunNuruGenerator(Source));

      generated.Contains("Tel43.Skipped").ShouldBeFalse();
      generated.ShouldContain("__meterBuilder.AddMeter(\"Tel43.Kept\")");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Returns each "&lt;builder&gt;.AddSource(...)" or ".AddMeter(...)" call, in emit order.
    /// </summary>
    private static string[] AddCalls(string generated, string method) =>
    [
      .. System.Text.RegularExpressions.Regex.Matches(generated, $@"__\w+Builder\.{method}\([^)]*\)")
        .Select(static match => match.Value)
    ];

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
