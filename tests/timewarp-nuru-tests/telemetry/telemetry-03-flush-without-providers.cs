#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 517: FlushTelemetryAsync returns at once when no provider exists (UseTelemetry() with
// no OTLP endpoint), and exports and disposes when one does. Kanban 590 removed the grace delay
// and added the flush budget; telemetry-07 covers the timing.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Telemetry
{
  using System.Diagnostics;
  using OpenTelemetry;
  using OpenTelemetry.Trace;

  [TestTag("Telemetry")]
  public class FlushWithoutProvidersTests
  {
    internal const string EndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    [ModuleInitializer]
    internal static void Register() => RegisterTests<FlushWithoutProvidersTests>();

    public static async Task Should_run_command_without_grace_delay_when_endpoint_unset()
    {
      string? savedEndpoint = Environment.GetEnvironmentVariable(EndpointVariable);
      Environment.SetEnvironmentVariable(EndpointVariable, null);
      try
      {
        using TestTerminal terminal = new();
        Tel03Capture.Reset();

        NuruApp app = NuruApp.CreateBuilder()
          .UseTerminal(terminal)
          .UseTelemetry()
          .Map("tel03-probe")
            .WithHandler(Tel03Capture.Probe)
            .AsQuery()
            .Done()
          .Build();

        Tel03Capture.App = app;

        // Warm-up run so JIT and first-call costs do not count against the timing below.
        int warmupExitCode = await app.RunAsync(["tel03-probe"]);
        warmupExitCode.ShouldBe(0);

        Stopwatch stopwatch = Stopwatch.StartNew();
        int exitCode = await app.RunAsync(["tel03-probe"]);
        stopwatch.Stop();

        exitCode.ShouldBe(0);
        terminal.OutputContains("ok").ShouldBeTrue();
        Tel03Capture.TracerWasSet.ShouldBe(false);
        Tel03Capture.MeterWasSet.ShouldBe(false);
        Tel03Capture.LoggerFactoryWasSet.ShouldBe(false);
        // Before kanban 517 every run waited the fixed 1000 ms grace delay.
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(500);
      }
      finally
      {
        Environment.SetEnvironmentVariable(EndpointVariable, savedEndpoint);
      }
    }

    public static async Task Should_return_immediately_when_no_provider_exists()
    {
      NuruApp app = new();

      Stopwatch stopwatch = Stopwatch.StartNew();
      await app.FlushTelemetryAsync(timeoutMs: 5000);
      stopwatch.Stop();

      stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000);
      app.TracerProvider.ShouldBeNull();
      app.MeterProvider.ShouldBeNull();
      app.LoggerFactory.ShouldBeNull();
    }

    public static async Task Should_flush_and_dispose_when_tracer_provider_exists()
    {
      const string SourceName = "TimeWarp.Nuru.Tests.Tel03";
      Tel03CountingExporter exporter = new();

      using ActivitySource activitySource = new(SourceName);
      NuruApp app = new()
      {
        // A long scheduled delay keeps the span buffered until ForceFlush runs.
        TracerProvider = Sdk.CreateTracerProviderBuilder()
          .AddSource(SourceName)
          .AddProcessor(new BatchActivityExportProcessor(exporter, scheduledDelayMilliseconds: 60_000))
          .Build()
      };

      using (activitySource.StartActivity("tel03-span"))
      {
      }

      exporter.ExportedCount.ShouldBe(0);

      await app.FlushTelemetryAsync();

      exporter.ExportedCount.ShouldBe(1);
      app.TracerProvider.ShouldBeNull();
      exporter.ShutdownCalled.ShouldBeTrue();
    }
  }
}

public sealed class Tel03CountingExporter : OpenTelemetry.BaseExporter<System.Diagnostics.Activity>
{
  private int Exported;

  public int ExportedCount => Volatile.Read(ref Exported);
  public bool ShutdownCalled { get; private set; }

  public override OpenTelemetry.ExportResult Export(in OpenTelemetry.Batch<System.Diagnostics.Activity> batch)
  {
    Interlocked.Add(ref Exported, (int)batch.Count);
    return OpenTelemetry.ExportResult.Success;
  }

  protected override bool OnShutdown(int timeoutMilliseconds)
  {
    ShutdownCalled = true;
    return true;
  }
}

public static class Tel03Capture
{
  public static NuruApp? App { get; set; }
  public static bool? TracerWasSet { get; set; }
  public static bool? MeterWasSet { get; set; }
  public static bool? LoggerFactoryWasSet { get; set; }

  public static void Reset()
  {
    App = null;
    TracerWasSet = null;
    MeterWasSet = null;
    LoggerFactoryWasSet = null;
  }

  public static string Probe()
  {
    TracerWasSet = App?.TracerProvider is not null;
    MeterWasSet = App?.MeterProvider is not null;
    LoggerFactoryWasSet = App?.LoggerFactory is not null;
    return "ok";
  }
}
