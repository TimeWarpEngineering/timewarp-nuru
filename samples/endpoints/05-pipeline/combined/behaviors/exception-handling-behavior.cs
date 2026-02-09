// ═══════════════════════════════════════════════════════════════════════════════
// EXCEPTION HANDLING BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// Consistent error handling for all commands.

namespace PipelineCombined.Behaviors;

using TimeWarp.Nuru;
using static System.Console;

public sealed class ExceptionHandlingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    try { await proceed(); }
    catch (Exception ex)
    {
      WriteLine($"[ERROR] {context.CommandName} failed: {ex.Message}");
      throw;
    }
  }
}
