// ═══════════════════════════════════════════════════════════════════════════════
// RETRY BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// Exponential backoff with jitter for transient failures.

namespace PipelineRetry.Behaviors;

using TimeWarp.Nuru;
using static System.Console;

public sealed class RetryBehavior : INuruBehavior<IRetryable>
{
  public async ValueTask HandleAsync(BehaviorContext<IRetryable> context, Func<ValueTask> proceed)
  {
    int maxRetries = context.Command.MaxRetries;
    int attempt = 0;

    while (true)
    {
      attempt++;

      try
      {
        await proceed();
        return; // Success!
      }
      catch (Exception ex) when (attempt <= maxRetries && IsRetryable(ex))
      {
        int delayMs = CalculateBackoff(attempt);
        WriteLine($"[RETRY] Attempt {attempt}/{maxRetries + 1} failed: {ex.Message}");
        WriteLine($"[RETRY] Waiting {delayMs}ms before retry...");
        await Task.Delay(delayMs);
      }
    }
  }

  private static bool IsRetryable(Exception ex)
  {
    return ex is TimeoutException
        || ex is IOException
        || ex is HttpRequestException
        || ex is TaskCanceledException;
  }

  private static int CalculateBackoff(int attempt)
  {
    int baseDelay = (int)Math.Pow(2, attempt - 1) * 100;
    int jitter = Random.Shared.Next(0, 100);
    return Math.Min(baseDelay + jitter, 5000);
  }
}
