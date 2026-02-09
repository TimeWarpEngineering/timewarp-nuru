// ═══════════════════════════════════════════════════════════════════════════════
// EXCEPTION HANDLING BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// Catches and categorizes exceptions with user-friendly messages.

namespace PipelineException.Behaviors;

using TimeWarp.Nuru;
using static System.Console;

public sealed class ExceptionHandlingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    try
    {
      await proceed();
    }
    catch (ArgumentException ex)
    {
      WriteLine($"[ERROR] Invalid input: {ex.Message}");
      throw;
    }
    catch (InvalidOperationException ex)
    {
      WriteLine($"[ERROR] Operation failed: {ex.Message}");
      throw;
    }
    catch (UnauthorizedAccessException ex)
    {
      WriteLine($"[ERROR] Access denied: {ex.Message}");
      throw;
    }
    catch (Exception ex)
    {
      WriteLine($"[ERROR] Unexpected error: {ex.Message}");
      throw;
    }
  }
}
