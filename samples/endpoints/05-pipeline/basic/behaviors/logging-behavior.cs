// ═══════════════════════════════════════════════════════════════════════════════
// LOGGING BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// Logs request entry and exit. Demonstrates before/after pattern.

namespace PipelineBasic.Behaviors;

using TimeWarp.Nuru;
using static System.Console;

public sealed class LoggingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Handling {context.CommandName}");

    try
    {
      await proceed();
      WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Completed {context.CommandName}");
    }
    catch (Exception ex)
    {
      WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Error handling {context.CommandName}: {ex.Message}");
      throw;
    }
  }
}
