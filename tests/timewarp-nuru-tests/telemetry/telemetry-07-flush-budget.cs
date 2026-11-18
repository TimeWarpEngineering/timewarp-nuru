#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 590: FlushTelemetryAsync has no grace delay after a successful export, and a hanging
// export (a blackholed endpoint) costs at most the flush budget: an explicit timeoutMs, or
// OTEL_EXPORTER_OTLP_TIMEOUT when no timeout is passed.
#endregion

#region Design
// Tel07HangingExporter blocks Export on a gate and ignores shutdown, the worst case of a dropped
// endpoint. Each test releases the gate in finally so the export thread ends before the next test.
// Tel07HangingLoggerProvider blocks in Dispose: ILoggerFactory has no timed shutdown, so only the
// outer wait in FlushTelemetryAsync bounds it.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Telemetry
{
  using System.Diagnostics;
  using Microsoft.Extensions.Logging;
  using OpenTelemetry;
  using OpenTelemetry.Trace;

  [TestTag("Telemetry")]
  public class FlushBudgetTests
  {
    internal const string SourceName = "TimeWarp.Nuru.Tests.Tel07";
    internal const string TimeoutVariable = "OTEL_EXPORTER_OTLP_TIMEOUT";

    [ModuleInitializer]
    internal static void Register() => RegisterTests<FlushBudgetTests>();

    public static async Task Should_export_and_return_without_grace_delay_when_export_succeeds()
    {
      using Tel07TimeoutVariable variable = new(null);
      Tel07CountingExporter exporter = new();
      using ActivitySource activitySource = new(SourceName);
      NuruApp app = new() { TracerProvider = BuildTracerProvider(exporter) };

      using (activitySource.StartActivity("tel07-span"))
      {
      }

      exporter.ExportedCount.ShouldBe(0);

      Stopwatch stopwatch = Stopwatch.StartNew();
      await app.FlushTelemetryAsync();
      stopwatch.Stop();

      exporter.ExportedCount.ShouldBe(1);
      exporter.ShutdownCalled.ShouldBeTrue();
      app.TracerProvider.ShouldBeNull();
      // Before kanban 590 every flush waited a fixed 1000 ms after ForceFlush.
      stopwatch.ElapsedMilliseconds.ShouldBeLessThan(500);
    }

    public static async Task Should_return_after_timeout_when_export_hangs()
    {
      using Tel07TimeoutVariable variable = new(null);
      using Tel07HangingExporter exporter = new();
      using ActivitySource activitySource = new(SourceName);
      NuruApp app = new() { TracerProvider = BuildTracerProvider(exporter) };

      using (activitySource.StartActivity("tel07-span"))
      {
      }

      Stopwatch stopwatch = Stopwatch.StartNew();
      await app.FlushTelemetryAsync(timeoutMs: 300);
      stopwatch.Stop();

      exporter.ExportStarted.ShouldBeTrue();
      app.TracerProvider.ShouldBeNull();
      stopwatch.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(250);
      stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1500);
    }

    public static async Task Should_use_otlp_timeout_variable_when_no_timeout_is_passed()
    {
      // 400 ms, well under the 2000 ms default, so the timing tells the two apart.
      using Tel07TimeoutVariable variable = new("400");
      using Tel07HangingExporter exporter = new();
      using ActivitySource activitySource = new(SourceName);
      NuruApp app = new() { TracerProvider = BuildTracerProvider(exporter) };

      using (activitySource.StartActivity("tel07-span"))
      {
      }

      Stopwatch stopwatch = Stopwatch.StartNew();
      await app.FlushTelemetryAsync();
      stopwatch.Stop();

      exporter.ExportStarted.ShouldBeTrue();
      stopwatch.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(350);
      stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1500);
    }

    public static async Task Should_bound_logger_factory_dispose_by_timeout()
    {
      using Tel07HangingLoggerProvider provider = new();
      // A factory registration, so the logger factory's service provider disposes it (an instance
      // passed to AddProvider is not disposed).
      NuruApp app = new()
      {
        LoggerFactory = LoggerFactory.Create(builder => builder.Services.AddSingleton<ILoggerProvider>(_ => provider))
      };

      Stopwatch stopwatch = Stopwatch.StartNew();
      await app.FlushTelemetryAsync(timeoutMs: 300);
      stopwatch.Stop();

      app.LoggerFactory.ShouldBeNull();
      stopwatch.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(250);
      stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1500);
    }

    public static async Task Should_reject_timeout_below_infinite()
    {
      NuruApp app = new();

      await Should.ThrowAsync<ArgumentOutOfRangeException>(() => app.FlushTelemetryAsync(timeoutMs: -2));
    }

    private static TracerProvider BuildTracerProvider(BaseExporter<Activity> exporter) =>
      Sdk.CreateTracerProviderBuilder()
        .AddSource(SourceName)
        // A long scheduled delay keeps the span buffered until the flush runs.
        .AddProcessor(new BatchActivityExportProcessor(exporter, scheduledDelayMilliseconds: 60_000))
        .Build();
  }

  /// <summary>
  /// Sets OTEL_EXPORTER_OTLP_TIMEOUT for one test and restores it on dispose.
  /// </summary>
  public sealed class Tel07TimeoutVariable : IDisposable
  {
    private readonly string? Saved;

    public Tel07TimeoutVariable(string? value)
    {
      Saved = Environment.GetEnvironmentVariable(FlushBudgetTests.TimeoutVariable);
      Environment.SetEnvironmentVariable(FlushBudgetTests.TimeoutVariable, value);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(FlushBudgetTests.TimeoutVariable, Saved);
  }
}

public sealed class Tel07CountingExporter : OpenTelemetry.BaseExporter<System.Diagnostics.Activity>
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

/// <summary>
/// An exporter whose Export blocks until the test releases it (at most 10 s), like an export to
/// an endpoint that drops packets. Shutdown does not unblock it.
/// </summary>
public sealed class Tel07HangingExporter : OpenTelemetry.BaseExporter<System.Diagnostics.Activity>
{
  private readonly TaskCompletionSource Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private int Started;

  public bool ExportStarted => Volatile.Read(ref Started) == 1;

  public override OpenTelemetry.ExportResult Export(in OpenTelemetry.Batch<System.Diagnostics.Activity> batch)
  {
    Volatile.Write(ref Started, 1);
    Gate.Task.Wait(TimeSpan.FromSeconds(10));
    return OpenTelemetry.ExportResult.Failure;
  }

  protected override void Dispose(bool disposing)
  {
    // Release the export thread if it is still waiting.
    Gate.TrySetResult();
    base.Dispose(disposing);
  }
}

/// <summary>
/// A logger provider whose Dispose blocks until the test disposes it a second time (at most 10 s).
/// </summary>
public sealed class Tel07HangingLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider
{
  private readonly TaskCompletionSource Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private int DisposeCount;

  public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) =>
    Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

  public void Dispose()
  {
    // The first Dispose comes from the logger factory and hangs; the test's using releases it.
    if (Interlocked.Increment(ref DisposeCount) == 1)
      Gate.Task.Wait(TimeSpan.FromSeconds(10));
    else
      Gate.TrySetResult();
  }
}
