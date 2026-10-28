#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 551: OTLP http/protobuf export posts each signal to {base}/v1/{signal}. Before the fix
// the generated setup set o.Endpoint = <base> on every exporter, which turns off the SDK's
// signal-path append, so every export posted to "/" and the collector returned 404.
// Covers NuruOtlpEndpoint resolution (http/protobuf and grpc, code and env endpoints) and an
// end-to-end run against a local HTTP sink that records the request paths.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Telemetry
{
  using System.Collections.Concurrent;
  using System.Diagnostics;
  using System.Net;
  using System.Net.Sockets;
  using Microsoft.Extensions.Logging;
  using OpenTelemetry.Exporter;

  [TestTag("Telemetry")]
  public class OtlpEndpointPathTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<OtlpEndpointPathTests>();

    public static async Task Should_append_signal_path_for_http_protobuf()
    {
      NuruOtlpEndpoint.ForSignal(new Uri("http://collector:4318"), OtlpExportProtocol.HttpProtobuf, NuruOtlpEndpoint.Traces)
        .AbsoluteUri.ShouldBe("http://collector:4318/v1/traces");
      NuruOtlpEndpoint.ForSignal(new Uri("https://otlp.timewarp.work/"), OtlpExportProtocol.HttpProtobuf, NuruOtlpEndpoint.Metrics)
        .AbsoluteUri.ShouldBe("https://otlp.timewarp.work/v1/metrics");
      NuruOtlpEndpoint.ForSignal(new Uri("http://collector:4318/otlp/"), OtlpExportProtocol.HttpProtobuf, NuruOtlpEndpoint.Logs)
        .AbsoluteUri.ShouldBe("http://collector:4318/otlp/v1/logs");

      await Task.CompletedTask;
    }

    public static async Task Should_keep_http_endpoint_that_already_ends_with_signal_path()
    {
      NuruOtlpEndpoint.ForSignal(new Uri("http://collector:4318/v1/traces"), OtlpExportProtocol.HttpProtobuf, NuruOtlpEndpoint.Traces)
        .AbsoluteUri.ShouldBe("http://collector:4318/v1/traces");
      NuruOtlpEndpoint.ForSignal(new Uri("http://collector:4318/v1/logs/"), OtlpExportProtocol.HttpProtobuf, NuruOtlpEndpoint.Logs)
        .AbsoluteUri.ShouldBe("http://collector:4318/v1/logs/");

      await Task.CompletedTask;
    }

    public static async Task Should_leave_grpc_endpoint_unchanged()
    {
      Uri endpoint = new("http://collector:4317");
      NuruOtlpEndpoint.ForSignal(endpoint, OtlpExportProtocol.Grpc, NuruOtlpEndpoint.Traces).ShouldBe(endpoint);
      NuruOtlpEndpoint.ForSignal(endpoint, OtlpExportProtocol.Grpc, NuruOtlpEndpoint.Metrics).ShouldBe(endpoint);
      NuruOtlpEndpoint.ForSignal(endpoint, OtlpExportProtocol.Grpc, NuruOtlpEndpoint.Logs).ShouldBe(endpoint);

      await Task.CompletedTask;
    }

    public static async Task Should_resolve_code_endpoint_as_base_url()
    {
      using Tel04Environment environment = new();

      OtlpExporterOptions http = new() { Protocol = OtlpExportProtocol.HttpProtobuf };
      NuruOtlpEndpoint.Configure(http, "https://otlp.timewarp.work/", NuruOtlpEndpoint.Metrics);
      http.Endpoint.AbsoluteUri.ShouldBe("https://otlp.timewarp.work/v1/metrics");

      OtlpExporterOptions grpc = new() { Protocol = OtlpExportProtocol.Grpc };
      NuruOtlpEndpoint.Configure(grpc, "http://collector:4317", NuruOtlpEndpoint.Metrics);
      grpc.Endpoint.AbsoluteUri.ShouldBe("http://collector:4317/");

      await Task.CompletedTask;
    }

    public static async Task Should_prefer_code_endpoint_over_signal_env_endpoint()
    {
      using Tel04Environment environment = new();
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://env-collector:4318/custom/traces");

      OtlpExporterOptions options = new() { Protocol = OtlpExportProtocol.HttpProtobuf };
      NuruOtlpEndpoint.Configure(options, "http://code-collector:4318", NuruOtlpEndpoint.Traces);

      options.Endpoint.AbsoluteUri.ShouldBe("http://code-collector:4318/v1/traces");

      await Task.CompletedTask;
    }

    public static async Task Should_apply_signal_env_endpoint_and_protocol_as_is()
    {
      using Tel04Environment environment = new();
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://base:4317");
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "http://logs:4318/custom/logs");
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_PROTOCOL", "http/protobuf");

      OtlpExporterOptions logs = new();
      NuruOtlpEndpoint.Configure(logs, codeEndpoint: null, NuruOtlpEndpoint.Logs);
      logs.Endpoint.AbsoluteUri.ShouldBe("http://logs:4318/custom/logs");
      logs.Protocol.ShouldBe(OtlpExportProtocol.HttpProtobuf);

      // Traces has no per-signal variables: the SDK's base-env values stay untouched.
      OtlpExporterOptions traces = new();
      NuruOtlpEndpoint.Configure(traces, codeEndpoint: null, NuruOtlpEndpoint.Traces);
      traces.Endpoint.AbsoluteUri.ShouldBe("http://base:4317/");
      traces.Protocol.ShouldBe(OtlpExportProtocol.Grpc);

      await Task.CompletedTask;
    }

    public static async Task Should_export_http_signals_to_v1_paths_for_base_env_endpoint()
    {
      using Tel04Environment environment = new();
      using Tel04OtlpSink sink = new();
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", sink.Prefix);
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");

      await Tel04Capture.RunProbeAsync();

      string[] paths = await sink.WaitForPathsAsync("/v1/traces", "/v1/metrics", "/v1/logs");
      paths.ShouldContain("/v1/traces");
      paths.ShouldContain("/v1/metrics");
      paths.ShouldContain("/v1/logs");
      paths.ShouldNotContain("/");
    }

    public static async Task Should_export_to_signal_env_endpoint_when_set()
    {
      using Tel04Environment environment = new();
      using Tel04OtlpSink sink = new();
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", sink.Prefix);
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", sink.Prefix + "custom/traces");

      await Tel04Capture.RunProbeAsync();

      string[] paths = await sink.WaitForPathsAsync("/custom/traces", "/v1/metrics", "/v1/logs");
      paths.ShouldContain("/custom/traces");
      paths.ShouldContain("/v1/metrics");
      paths.ShouldContain("/v1/logs");
      paths.ShouldNotContain("/v1/traces");
    }
  }

  /// <summary>
  /// Snapshots every OTLP endpoint and protocol variable and restores them on dispose.
  /// Starts with all of them cleared.
  /// </summary>
  public sealed class Tel04Environment : IDisposable
  {
    private static readonly string[] Names =
    [
      "OTEL_EXPORTER_OTLP_ENDPOINT",
      "OTEL_EXPORTER_OTLP_PROTOCOL",
      "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT",
      "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL",
      "OTEL_EXPORTER_OTLP_METRICS_ENDPOINT",
      "OTEL_EXPORTER_OTLP_METRICS_PROTOCOL",
      "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT",
      "OTEL_EXPORTER_OTLP_LOGS_PROTOCOL",
    ];

    private readonly Dictionary<string, string?> Saved = [];

    public Tel04Environment()
    {
      foreach (string name in Names)
      {
        Saved[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, null);
      }
    }

    public void Dispose()
    {
      foreach (KeyValuePair<string, string?> entry in Saved)
      {
        Environment.SetEnvironmentVariable(entry.Key, entry.Value);
      }
    }
  }

  /// <summary>
  /// Local OTLP/HTTP stand-in: answers 200 to every POST and records the request path.
  /// </summary>
  public sealed class Tel04OtlpSink : IDisposable
  {
    private readonly HttpListener Listener = new();
    private readonly ConcurrentQueue<string> Paths = new();
    private readonly Task Loop;

    public Tel04OtlpSink()
    {
      int port = FreePort();
      Prefix = $"http://127.0.0.1:{port}/";
      Listener.Prefixes.Add(Prefix);
      Listener.Start();
      Loop = Task.Run(ServeAsync);
    }

    public string Prefix { get; }

    /// <summary>
    /// Waits up to 10 seconds for every expected path, then returns all recorded paths.
    /// </summary>
    public async Task<string[]> WaitForPathsAsync(params string[] expected)
    {
      for (int attempt = 0; attempt < 200 && !expected.All(Paths.Contains); attempt++)
      {
        await Task.Delay(50);
      }

      return [.. Paths];
    }

    public void Dispose()
    {
      Listener.Close();
      try
      {
        Loop.Wait(TimeSpan.FromSeconds(5));
      }
      catch (AggregateException)
      {
        // The listener loop ends with an exception when the listener closes.
      }
    }

    private async Task ServeAsync()
    {
      while (Listener.IsListening)
      {
        HttpListenerContext context;
        try
        {
          context = await Listener.GetContextAsync();
        }
        catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException)
        {
          return;
        }

        Paths.Enqueue(context.Request.Url!.AbsolutePath);
        await context.Request.InputStream.CopyToAsync(Stream.Null);
        context.Response.StatusCode = 200;
        context.Response.Close();
      }
    }

    private static int FreePort()
    {
      using TcpListener probe = new(IPAddress.Loopback, 0);
      probe.Start();
      int port = ((IPEndPoint)probe.LocalEndpoint).Port;
      probe.Stop();
      return port;
    }
  }

  public static class Tel04Capture
  {
    // Generated UseTelemetry() setup listens to this source (TelemetryBehavior uses it), so a
    // span started here exports through the generated TracerProvider.
    private static readonly ActivitySource BehaviorSource = new("TimeWarp.Nuru.Behavior");

    private static readonly Action<ILogger, Exception?> LogProbe =
      LoggerMessage.Define(LogLevel.Information, new EventId(1, "Tel04Probe"), "tel04 probe ran");

    public static NuruApp? App { get; set; }

    /// <summary>
    /// Runs one command through UseTelemetry() so a span, metrics, and a log record export.
    /// </summary>
    public static async Task RunProbeAsync()
    {
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseTelemetry()
        .Map("tel04-probe")
          .WithHandler(Probe)
          .AsQuery()
          .Done()
        .Build();

      App = app;
      try
      {
        int exitCode = await app.RunAsync(["tel04-probe"]);
        exitCode.ShouldBe(0);
        terminal.OutputContains("ok").ShouldBeTrue();
      }
      finally
      {
        App = null;
      }
    }

    public static string Probe()
    {
      using Activity? activity = BehaviorSource.StartActivity("tel04-probe");
      ILogger? logger = App?.LoggerFactory?.CreateLogger("Tel04");
      if (logger is not null)
        LogProbe(logger, null);
      return "ok";
    }
  }
}
