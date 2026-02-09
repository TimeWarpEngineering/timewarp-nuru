// ═══════════════════════════════════════════════════════════════════════════════
// TELEMETRY BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// OpenTelemetry-compatible distributed tracing with Activity spans.

namespace PipelineTelemetry.Behaviors;

using System.Diagnostics;
using TimeWarp.Nuru;

public sealed class TelemetryBehavior : INuruBehavior
{
  private static readonly ActivitySource ActivitySource = new("TimeWarp.Nuru.Samples");

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    string activityName = $"nuru.command.{context.CommandName}";

    using Activity? activity = ActivitySource.StartActivity(activityName, ActivityKind.Server);

    if (activity != null)
    {
      activity.SetTag("nuru.command.name", context.CommandName);
      activity.SetTag("nuru.correlation.id", context.CorrelationId);
      activity.SetTag("nuru.command.type", context.Command?.GetType().Name ?? "unknown");
    }

    try
    {
      await proceed();
      activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      throw;
    }
  }
}
