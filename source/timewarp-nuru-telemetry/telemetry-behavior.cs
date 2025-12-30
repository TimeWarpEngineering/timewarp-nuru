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
/// <typeparam name="TMessage">The message/command type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class TelemetryBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  /// <summary>
  /// Delegate type for the next handler in the pipeline.
  /// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - Delegate suffix is conventional for delegate types
  public delegate ValueTask<TResponse> MessageHandlerDelegate(TMessage message, CancellationToken cancellationToken);
#pragma warning restore CA1711

  /// <summary>
  /// Handles the command with telemetry instrumentation.
  /// </summary>
#pragma warning disable RCS1046 // Add suffix 'Async' to asynchronous method name - Handle is conventional for handlers
  public async ValueTask<TResponse> Handle
#pragma warning restore RCS1046
  (
    TMessage message,
    MessageHandlerDelegate next,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(next);

    string commandName = typeof(TMessage).Name;

    // Start Activity span for distributed tracing
    using Activity? activity = NuruTelemetryExtensions.NuruActivitySource.StartActivity(commandName, ActivityKind.Internal);
    activity?.SetTag("command.type", typeof(TMessage).FullName);
    activity?.SetTag("command.name", commandName);

    Stopwatch stopwatch = Stopwatch.StartNew();

    try
    {
      TResponse response = await next(message, cancellationToken).ConfigureAwait(false);

      stopwatch.Stop();
      activity?.SetStatus(ActivityStatusCode.Ok);

      // Record success metrics
      NuruTelemetryExtensions.CommandsInvoked.Add(1, new KeyValuePair<string, object?>("command", commandName));
      NuruTelemetryExtensions.CommandDuration.Record(stopwatch.ElapsedMilliseconds,
        new KeyValuePair<string, object?>("command", commandName),
        new KeyValuePair<string, object?>("status", "ok"));

      // Flush telemetry so metrics appear immediately in dashboards
      await NuruTelemetryExtensions.FlushAsync().ConfigureAwait(false);

      return response;
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
        new KeyValuePair<string, object?>("command", commandName),
        new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

      NuruTelemetryExtensions.CommandDuration.Record(stopwatch.ElapsedMilliseconds,
        new KeyValuePair<string, object?>("command", commandName),
        new KeyValuePair<string, object?>("status", "error"));

      // Flush telemetry so metrics appear immediately in dashboards
      await NuruTelemetryExtensions.FlushAsync().ConfigureAwait(false);

      throw;
    }
  }
}
