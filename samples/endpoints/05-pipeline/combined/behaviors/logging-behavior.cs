// ═══════════════════════════════════════════════════════════════════════════════
// LOGGING BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// Request/response logging.

namespace PipelineCombined.Behaviors;

using TimeWarp.Nuru;
using static System.Console;

public sealed class LoggingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    WriteLine($"[LOG] [{context.CorrelationId[..8]}] {context.CommandName} started");
    await proceed();
    WriteLine($"[LOG] [{context.CorrelationId[..8]}] {context.CommandName} completed");
  }
}
