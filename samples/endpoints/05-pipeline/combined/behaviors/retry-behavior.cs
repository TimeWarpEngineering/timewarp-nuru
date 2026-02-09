// ═══════════════════════════════════════════════════════════════════════════════
// RETRY BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// Filtered retry with exponential backoff.

namespace PipelineCombined.Behaviors;

using TimeWarp.Nuru;
using static System.Console;

public sealed class RetryBehavior : INuruBehavior<IRetryable>
{
  public async ValueTask HandleAsync(BehaviorContext<IRetryable> context, Func<ValueTask> proceed)
  {
    int maxRetries = context.Command.MaxRetries;

    for (int i = 1; i <= maxRetries + 1; i++)
    {
      try { await proceed(); return; }
      catch (Exception ex) when (i <= maxRetries && IsTransient(ex))
      {
        int delay = Math.Min((int)Math.Pow(2, i) * 50 + Random.Shared.Next(50), 3000);
        WriteLine($"[RETRY] Attempt {i} failed, waiting {delay}ms...");
        await Task.Delay(delay);
      }
    }
  }

  private static bool IsTransient(Exception ex) =>
    ex is TimeoutException or IOException or HttpRequestException;
}
