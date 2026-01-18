namespace TimeWarp.Nuru;

using System.Diagnostics;

/// <summary>
/// Pipeline behavior that automatically instruments all commands with OpenTelemetry.
/// This is the recommended pattern - telemetry is applied consistently without
/// manual instrumentation in each command handler.
/// </summary>
/// <remarks>
/// <para>
/// This is a standalone telemetry behavior that works with any message type.
/// It uses the shared <see cref="NuruTelemetryExtensions.NuruActivitySource"/>
/// and metrics from <see cref="NuruTelemetryExtensions"/>.
/// </para>
/// </remarks>

public sealed class TelemetryBehavior : INuruBehavior
{

  /// <summary>
  /// Handles the command with telemetry instrumentation.
  /// </summary>
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    ArgumentNullException.ThrowIfNull(proceed);
    ArgumentNullException.ThrowIfNull(context);

    // Start Activity span for distributed tracing
    using Activity? activity = NuruTelemetryExtensions.NuruActivitySource.StartActivity(context.CommandName, ActivityKind.Internal);
    activity?.SetTag("command.type", context.CommandTypeName);
    activity?.SetTag("command.name", context.CommandName);

    Stopwatch stopwatch = Stopwatch.StartNew();

    try
    {
      await proceed().ConfigureAwait(false);

      stopwatch.Stop();
      activity?.SetStatus(ActivityStatusCode.Ok);

      // Record success metrics
      NuruTelemetryExtensions.CommandsInvoked.Add(1, new KeyValuePair<string, object?>("command", context.CommandName));
      NuruTelemetryExtensions.CommandDuration.Record(stopwatch.ElapsedMilliseconds,
        new KeyValuePair<string, object?>("command", context.CommandName),
        new KeyValuePair<string, object?>("status", "ok"));

      // Flush telemetry so metrics appear immediately in dashboards
      await NuruTelemetryExtensions.FlushAsync().ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();

      // Record error in trace
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      activity?.SetTag("error.type", ex.GetType().Name);
      activity?.SetTag("error.message", ex.Message);

      // Record error metrics
      NuruTelemetryExtensions.CommandsErrored.Add(1,
        new KeyValuePair<string, object?>("command", context.CommandName),
        new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

      NuruTelemetryExtensions.CommandDuration.Record(stopwatch.ElapsedMilliseconds,
        new KeyValuePair<string, object?>("command", context.CommandName),
        new KeyValuePair<string, object?>("status", "error"));

      // Flush telemetry so metrics appear immediately in dashboards
      await NuruTelemetryExtensions.FlushAsync().ConfigureAwait(false);

      throw;
    }
  }
}
