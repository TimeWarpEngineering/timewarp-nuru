// ═══════════════════════════════════════════════════════════════════════════════
// DISTRIBUTED TELEMETRY BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// OpenTelemetry-compatible distributed tracing.

namespace PipelineCombined.Behaviors;

using System.Diagnostics;
using TimeWarp.Nuru;

public sealed class DistributedTelemetryBehavior : INuruBehavior
{
  private static readonly ActivitySource Source = new("Nuru.CompletePipeline");

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    using Activity? activity = Source.StartActivity($"nuru.{context.CommandName}");
    activity?.SetTag("command", context.CommandName);
    activity?.SetTag("correlation", context.CorrelationId);

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
