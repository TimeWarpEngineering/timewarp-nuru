namespace TimeWarp.Nuru;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Extension methods for configuring OpenTelemetry in Nuru applications.
/// </summary>
public static class NuruTelemetryExtensions
{
  /// <summary>
  /// ActivitySource for Nuru command tracing.
  /// </summary>
  public static readonly ActivitySource NuruActivitySource = new("TimeWarp.Nuru", "1.0.0");

  /// <summary>
  /// Meter for Nuru metrics.
  /// </summary>
  public static readonly Meter NuruMeter = new("TimeWarp.Nuru", "1.0.0");

  // Metrics instruments - public for use by TelemetryBehavior
  /// <summary>Counter for commands invoked.</summary>
  public static readonly Counter<int> CommandsInvoked = NuruMeter.CreateCounter<int>(
    name: "nuru.commands.invoked",
    unit: "{commands}",
    description: "Number of commands executed");

  /// <summary>Counter for command errors.</summary>
  public static readonly Counter<int> CommandsErrored = NuruMeter.CreateCounter<int>(
    name: "nuru.commands.errors",
    unit: "{errors}",
    description: "Number of failed commands");

  /// <summary>Histogram for command duration.</summary>
  public static readonly Histogram<double> CommandDuration = NuruMeter.CreateHistogram<double>(
    name: "nuru.commands.duration",
    unit: "ms",
    description: "Command execution duration in milliseconds");

  /// <summary>Counter for REPL sessions.</summary>
  public static readonly Counter<int> ReplSessions = NuruMeter.CreateCounter<int>(
    name: "nuru.repl.sessions",
    unit: "{sessions}",
    description: "Number of REPL sessions started");

  /// <summary>Counter for REPL commands.</summary>
  public static readonly Counter<int> ReplCommands = NuruMeter.CreateCounter<int>(
    name: "nuru.repl.commands",
    unit: "{commands}",
    description: "Commands executed in REPL mode");

  // Track configured providers for cleanup
  private static TracerProvider? tracerProvider;
  private static MeterProvider? meterProvider;

  /// <summary>
  /// Configures OpenTelemetry with OTLP export for any compatible backend (Aspire, Jaeger, Zipkin, etc.).
  /// Uses OTEL_EXPORTER_OTLP_ENDPOINT environment variable for OTLP endpoint.
  /// When the environment variable is not set, telemetry export is disabled with zero overhead.
  /// </summary>
  /// <remarks>
  /// This method configures:
  /// <list type="bullet">
  /// <item>Tracing via ActivitySource with OTLP export</item>
  /// <item>Metrics via Meter with OTLP export</item>
  /// <item>Structured logging with OTLP export (no console logging)</item>
  /// </list>
  /// </remarks>
  public static TBuilder UseTelemetry<TBuilder>(this TBuilder builder)
    where TBuilder : NuruCoreAppBuilder
  {
    return builder.UseTelemetry(_ => { });
  }

  /// <summary>
  /// Configures OpenTelemetry with OTLP export and custom options.
  /// </summary>
  /// <remarks>
  /// This method configures:
  /// <list type="bullet">
  /// <item>Tracing via ActivitySource with OTLP export</item>
  /// <item>Metrics via Meter with OTLP export</item>
  /// <item>Structured logging with OTLP export (no console logging)</item>
  /// </list>
  /// </remarks>
  public static TBuilder UseTelemetry<TBuilder>(
    this TBuilder builder,
    Action<NuruTelemetryOptions> configure)
    where TBuilder : NuruCoreAppBuilder
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(configure);

    NuruTelemetryOptions options = new();
    configure(options);

    // Build resource for all telemetry types
    ResourceBuilder resourceBuilder = ResourceBuilder.CreateDefault()
      .AddService(
        serviceName: options.EffectiveServiceName,
        serviceVersion: options.ServiceVersion);

    // Configure OpenTelemetry logging when OTLP endpoint is configured
    if (options.EnableLogging && options.ShouldExportTelemetry)
    {
      Uri otlpEndpoint = new(options.EffectiveOtlpEndpoint!);
      builder.ConfigureLogging(logging =>
      {
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddOpenTelemetry(otelOptions =>
        {
          otelOptions.SetResourceBuilder(resourceBuilder);
          otelOptions.AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = otlpEndpoint);
        });
      });
    }

    if (!options.ShouldExportTelemetry)
    {
      // No OTLP endpoint configured - telemetry export disabled with zero overhead
      // Logging is still configured above (console only)
      return builder;
    }

    Uri endpoint = new(options.EffectiveOtlpEndpoint!);

    // Configure tracing
    if (options.EnableTracing)
    {
      tracerProvider = Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(resourceBuilder)
        .AddSource(NuruActivitySource.Name)
        .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpoint)
        .Build();
    }

    // Configure metrics
    if (options.EnableMetrics)
    {
      meterProvider = Sdk.CreateMeterProviderBuilder()
        .SetResourceBuilder(resourceBuilder)
        .AddMeter(NuruMeter.Name)
        .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpoint)
        .Build();
    }

    return builder;
  }

  /// <summary>
  /// Flushes all telemetry data without disposing providers.
  /// Use this in REPL mode after each command to ensure telemetry appears immediately.
  /// </summary>
  /// <param name="delayMs">Optional delay in milliseconds to allow export to complete. Default: 100ms.</param>
  public static async Task FlushAsync(int delayMs = 100)
  {
    // Force flush to ensure all data is sent
    tracerProvider?.ForceFlush();
    meterProvider?.ForceFlush();

    // Allow time for export to complete
    if (delayMs > 0)
    {
      await Task.Delay(delayMs).ConfigureAwait(false);
    }
  }

  /// <summary>
  /// Flushes all telemetry data and disposes providers.
  /// Call this before application exit to ensure all telemetry is exported.
  /// Critical for CLI applications which exit quickly.
  /// </summary>
  /// <param name="delayMs">Optional delay in milliseconds to allow export to complete. Default: 1000ms.</param>
  public static async Task FlushAndShutdownAsync(int delayMs = 1000)
  {
    // Force flush to ensure all data is sent
    tracerProvider?.ForceFlush();
    meterProvider?.ForceFlush();

    // Allow time for export to complete
    if (delayMs > 0)
    {
      await Task.Delay(delayMs).ConfigureAwait(false);
    }

    // Dispose providers
    tracerProvider?.Dispose();
    meterProvider?.Dispose();
    tracerProvider = null;
    meterProvider = null;
  }

  /// <summary>
  /// Synchronous version of FlushAndShutdown.
  /// Flushes all telemetry data and disposes providers.
  /// </summary>
  public static void Shutdown()
  {
    tracerProvider?.ForceFlush();
    meterProvider?.ForceFlush();
    tracerProvider?.Dispose();
    meterProvider?.Dispose();
    tracerProvider = null;
    meterProvider = null;
  }

  /// <summary>
  /// Records a command execution with telemetry.
  /// </summary>
  /// <param name="commandName">Name of the command being executed.</param>
  /// <param name="action">The command action to execute.</param>
  public static void ExecuteWithTelemetry(string commandName, Action action)
  {
    ArgumentNullException.ThrowIfNull(action);

    using Activity? activity = NuruActivitySource.StartActivity(commandName, ActivityKind.Internal);
    activity?.SetTag("command.name", commandName);

    Stopwatch stopwatch = Stopwatch.StartNew();

    try
    {
      action();

      stopwatch.Stop();
      activity?.SetStatus(ActivityStatusCode.Ok);

      CommandsInvoked.Add(1, new KeyValuePair<string, object?>("command", commandName));
      CommandDuration.Record(stopwatch.ElapsedMilliseconds,
        new KeyValuePair<string, object?>("command", commandName));
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      RecordError(activity, commandName, ex, stopwatch.ElapsedMilliseconds);
      throw;
    }
  }

  /// <summary>
  /// Records a command execution with telemetry (async version).
  /// </summary>
  /// <param name="commandName">Name of the command being executed.</param>
  /// <param name="action">The async command action to execute.</param>
  public static async Task ExecuteWithTelemetryAsync(string commandName, Func<Task> action)
  {
    ArgumentNullException.ThrowIfNull(action);

    using Activity? activity = NuruActivitySource.StartActivity(commandName, ActivityKind.Internal);
    activity?.SetTag("command.name", commandName);

    Stopwatch stopwatch = Stopwatch.StartNew();

    try
    {
      await action().ConfigureAwait(false);

      stopwatch.Stop();
      activity?.SetStatus(ActivityStatusCode.Ok);

      CommandsInvoked.Add(1, new KeyValuePair<string, object?>("command", commandName));
      CommandDuration.Record(stopwatch.ElapsedMilliseconds,
        new KeyValuePair<string, object?>("command", commandName));
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      RecordError(activity, commandName, ex, stopwatch.ElapsedMilliseconds);
      throw;
    }
  }

  /// <summary>
  /// Records a REPL session start.
  /// </summary>
  public static Activity? StartReplSession()
  {
    ReplSessions.Add(1);
    return NuruActivitySource.StartActivity("repl.session", ActivityKind.Internal);
  }

  /// <summary>
  /// Records a REPL command execution.
  /// </summary>
  public static void RecordReplCommand(string command)
  {
    ReplCommands.Add(1, new KeyValuePair<string, object?>("command", command));
  }

  private static void RecordError(Activity? activity, string commandName, Exception ex, long elapsedMs)
  {
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.SetTag("error.type", ex.GetType().Name);
    activity?.SetTag("error.message", ex.Message);

    CommandsErrored.Add(1,
      new KeyValuePair<string, object?>("command", commandName),
      new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

    CommandDuration.Record(elapsedMs,
      new KeyValuePair<string, object?>("command", commandName),
      new KeyValuePair<string, object?>("status", "error"));
  }
}
