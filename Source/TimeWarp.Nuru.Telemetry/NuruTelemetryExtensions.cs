namespace TimeWarp.Nuru;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
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

  // Metrics instruments
  private static readonly Counter<int> CommandsInvoked = NuruMeter.CreateCounter<int>(
    name: "nuru.commands.invoked",
    unit: "{commands}",
    description: "Number of commands executed");

  private static readonly Counter<int> CommandsErrored = NuruMeter.CreateCounter<int>(
    name: "nuru.commands.errors",
    unit: "{errors}",
    description: "Number of failed commands");

  private static readonly Histogram<double> CommandDuration = NuruMeter.CreateHistogram<double>(
    name: "nuru.commands.duration",
    unit: "ms",
    description: "Command execution duration in milliseconds");

  private static readonly Counter<int> ReplSessions = NuruMeter.CreateCounter<int>(
    name: "nuru.repl.sessions",
    unit: "{sessions}",
    description: "Number of REPL sessions started");

  private static readonly Counter<int> ReplCommands = NuruMeter.CreateCounter<int>(
    name: "nuru.repl.commands",
    unit: "{commands}",
    description: "Commands executed in REPL mode");

  // Track configured providers for cleanup
  private static TracerProvider? tracerProvider;
  private static MeterProvider? meterProvider;

  /// <summary>
  /// Configures OpenTelemetry for Aspire Dashboard integration.
  /// Uses OTEL_EXPORTER_OTLP_ENDPOINT environment variable for OTLP endpoint.
  /// When the environment variable is not set, telemetry export is disabled with zero overhead.
  /// </summary>
  public static NuruAppBuilder UseAspireTelemetry(this NuruAppBuilder builder)
  {
    return builder.UseAspireTelemetry(_ => { });
  }

  /// <summary>
  /// Configures OpenTelemetry for Aspire Dashboard integration with custom options.
  /// </summary>
  public static NuruAppBuilder UseAspireTelemetry(
    this NuruAppBuilder builder,
    Action<NuruTelemetryOptions> configure)
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(configure);

    NuruTelemetryOptions options = new();
    configure(options);

    if (!options.ShouldExportTelemetry)
    {
      // No OTLP endpoint configured - telemetry disabled with zero overhead
      return builder;
    }

    Uri otlpEndpoint = new(options.EffectiveOtlpEndpoint!);

    ResourceBuilder resourceBuilder = ResourceBuilder.CreateDefault()
      .AddService(
        serviceName: options.EffectiveServiceName,
        serviceVersion: options.ServiceVersion);

    // Configure tracing
    if (options.EnableTracing)
    {
      tracerProvider = Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(resourceBuilder)
        .AddSource(NuruActivitySource.Name)
        .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = otlpEndpoint)
        .Build();
    }

    // Configure metrics
    if (options.EnableMetrics)
    {
      meterProvider = Sdk.CreateMeterProviderBuilder()
        .SetResourceBuilder(resourceBuilder)
        .AddMeter(NuruMeter.Name)
        .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = otlpEndpoint)
        .Build();
    }

    // Note: Logging integration requires the user to configure ILoggerFactory with OpenTelemetry
    // separately, as NuruAppBuilder.UseLogging() takes an ILoggerFactory instance.
    // See documentation for logging configuration examples.

    return builder;
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

  /// <summary>
  /// Disposes telemetry providers. Call during application shutdown.
  /// </summary>
  public static void Shutdown()
  {
    tracerProvider?.Dispose();
    meterProvider?.Dispose();
    tracerProvider = null;
    meterProvider = null;
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
