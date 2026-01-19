namespace TimeWarp.Nuru;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Pipeline behavior that automatically instruments all commands with OpenTelemetry.
/// This is the recommended pattern - telemetry is applied consistently without
/// manual instrumentation in each command handler.
/// </summary>
/// <remarks>
/// This behavior creates its own ActivitySource and Meter instances for telemetry.
/// When used with UseTelemetry(), the generated code will configure OTLP exporters
/// that automatically pick up these sources.
/// </remarks>
public sealed class TelemetryBehavior : INuruBehavior
{
  // Self-contained telemetry infrastructure
  private static readonly ActivitySource ActivitySource = new("TimeWarp.Nuru.Behavior", "1.0.0");
  private static readonly Meter Meter = new("TimeWarp.Nuru.Behavior", "1.0.0");
  private static readonly Counter<int> CommandsInvoked = Meter.CreateCounter<int>("nuru.behavior.commands.invoked", "{commands}", "Number of commands executed");
  private static readonly Counter<int> CommandsErrored = Meter.CreateCounter<int>("nuru.behavior.commands.errors", "{errors}", "Number of failed commands");
  private static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>("nuru.behavior.commands.duration", "ms", "Command execution duration in milliseconds");

  /// <summary>
  /// Handles the command with telemetry instrumentation.
  /// </summary>
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    ArgumentNullException.ThrowIfNull(proceed);
    ArgumentNullException.ThrowIfNull(context);

    // Start Activity span for distributed tracing
    using Activity? activity = ActivitySource.StartActivity(context.CommandName, ActivityKind.Internal);
    activity?.SetTag("command.type", context.CommandTypeName);
    activity?.SetTag("command.name", context.CommandName);

    Stopwatch stopwatch = Stopwatch.StartNew();

    try
    {
      await proceed().ConfigureAwait(false);

      stopwatch.Stop();
      activity?.SetStatus(ActivityStatusCode.Ok);

      // Record success metrics
      CommandsInvoked.Add(1, new KeyValuePair<string, object?>("command", context.CommandName));
      CommandDuration.Record(stopwatch.ElapsedMilliseconds,
        new KeyValuePair<string, object?>("command", context.CommandName),
        new KeyValuePair<string, object?>("status", "ok"));
    }
    catch (Exception ex)
    {
      stopwatch.Stop();

      // Record error in trace
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      activity?.SetTag("error.type", ex.GetType().Name);
      activity?.SetTag("error.message", ex.Message);

      // Record error metrics
      CommandsErrored.Add(1,
        new KeyValuePair<string, object?>("command", context.CommandName),
        new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

      CommandDuration.Record(stopwatch.ElapsedMilliseconds,
        new KeyValuePair<string, object?>("command", context.CommandName),
        new KeyValuePair<string, object?>("status", "error"));

      throw;
    }
  }
}
