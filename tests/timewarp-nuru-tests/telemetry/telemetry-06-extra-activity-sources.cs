#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 561: UseTelemetry(o => o.ActivitySources.Add(..) / o.Meters.Add(..)) exports spans and
// metrics a library puts on its own source, such as TimeWarp.Taratibu walk spans. Runs an app
// against an in-process OTLP/HTTP collector and looks for the span and instrument names in the
// exported payloads. Without the option the same span is not exported.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Telemetry
{
  using System.Collections.Concurrent;
  using System.Diagnostics;
  using System.Diagnostics.Metrics;
  using System.Net;
  using System.Net.Sockets;
  using System.Text;

  [TestTag("Telemetry")]
  public class ExtraActivitySourceTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ExtraActivitySourceTests>();

    public static async Task Should_export_spans_and_metrics_from_configured_sources()
    {
      using Tel06Environment environment = new();
      using Tel06OtlpCollector collector = new();
      Tel06Environment.PointAt(collector.Prefix);

      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseTelemetry(options =>
        {
          options.EnableLogging = false;
          options.ActivitySources.Add(Tel06Library.SourceName);
          options.Meters.Add(Tel06Library.MeterName);
        })
        .Map("tel06-walk")
          .WithHandler(Tel06Library.Walk)
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["tel06-walk"]);

      exitCode.ShouldBe(0);
      string traces = await collector.WaitForBodyAsync("/v1/traces", Tel06Library.SpanName);
      traces.ShouldContain(Tel06Library.SpanName);
      traces.ShouldContain(Tel06Library.SourceName);
      string metrics = await collector.WaitForBodyAsync("/v1/metrics", Tel06Library.CounterName);
      metrics.ShouldContain(Tel06Library.CounterName);
    }

    public static async Task Should_not_export_library_span_without_option()
    {
      using Tel06Environment environment = new();
      using Tel06OtlpCollector collector = new();
      Tel06Environment.PointAt(collector.Prefix);

      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseTelemetry(options => options.EnableLogging = false)
        .Map("tel06-plain")
          .WithHandler(Tel06Library.Walk)
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["tel06-plain"]);

      exitCode.ShouldBe(0);
      // The command span from TelemetryBehavior still exports; the library span does not.
      string traces = await collector.WaitForBodyAsync("/v1/traces", "tel06-plain");
      traces.ShouldContain("tel06-plain");
      traces.Contains(Tel06Library.SpanName, StringComparison.Ordinal).ShouldBeFalse();
    }
  }

  /// <summary>
  /// Stands in for a library (such as TimeWarp.Taratibu) with its own source and meter.
  /// </summary>
  public static class Tel06Library
  {
    public const string SourceName = "TimeWarp.Nuru.Tests.Tel06Library";
    public const string MeterName = "TimeWarp.Nuru.Tests.Tel06Library.Meter";
    public const string SpanName = "tel06-library-walk-node";
    public const string CounterName = "tel06_library_walk_nodes";

    private static readonly ActivitySource Source = new(SourceName);
    private static readonly Meter LibraryMeter = new(MeterName);
    private static readonly Counter<long> WalkNodes = LibraryMeter.CreateCounter<long>(CounterName);

    public static string Walk()
    {
      using Activity? activity = Source.StartActivity(SpanName);
      WalkNodes.Add(1);
      return "walked";
    }
  }

  /// <summary>
  /// Clears the OTLP endpoint and protocol variables, then restores them on dispose.
  /// </summary>
  public sealed class Tel06Environment : IDisposable
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

    public Tel06Environment()
    {
      foreach (string name in Names)
      {
        Saved[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, null);
      }
    }

    public static void PointAt(string endpoint)
    {
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", endpoint);
      Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
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
  /// Local OTLP/HTTP collector: answers 200 to every POST and keeps each body by path.
  /// </summary>
  /// <remarks>
  /// OTLP protobuf stores span, scope, and instrument names as plain UTF-8, so a Latin-1 view
  /// of the body is enough to look for them without a protobuf decoder.
  /// </remarks>
  public sealed class Tel06OtlpCollector : IDisposable
  {
    private readonly HttpListener Listener = new();
    private readonly ConcurrentQueue<(string Path, string Body)> Requests = new();
    private readonly Task Loop;

    public Tel06OtlpCollector()
    {
      int port = FreePort();
      Prefix = $"http://127.0.0.1:{port}/";
      Listener.Prefixes.Add(Prefix);
      Listener.Start();
      Loop = Task.Run(ServeAsync);
    }

    public string Prefix { get; }

    /// <summary>
    /// Waits up to 10 seconds for a body on <paramref name="path"/> that contains
    /// <paramref name="marker"/>, then returns every body received on that path.
    /// </summary>
    public async Task<string> WaitForBodyAsync(string path, string marker)
    {
      for (int attempt = 0; attempt < 200 && !Bodies(path).Contains(marker, StringComparison.Ordinal); attempt++)
      {
        await Task.Delay(50);
      }

      return Bodies(path);
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

    private string Bodies(string path) =>
      string.Join("\n", Requests.Where(r => r.Path == path).Select(static r => r.Body));

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

        using MemoryStream body = new();
        await context.Request.InputStream.CopyToAsync(body);
        Requests.Enqueue((context.Request.Url!.AbsolutePath, Encoding.Latin1.GetString(body.ToArray())));
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
}
