// ═══════════════════════════════════════════════════════════════════════════════
// PERFORMANCE BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// Times execution and warns on slow commands.

namespace PipelineBasic.Behaviors;

using System.Diagnostics;
using TimeWarp.Nuru;
using static System.Console;

public sealed class PerformanceBehavior : INuruBehavior
{
  private const int SlowThresholdMs = 500;

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    Stopwatch stopwatch = Stopwatch.StartNew();

    try
    {
      await proceed();
    }
    finally
    {
      stopwatch.Stop();
      long elapsed = stopwatch.ElapsedMilliseconds;

      if (elapsed > SlowThresholdMs)
      {
        WriteLine($"[PERFORMANCE] {context.CommandName} took {elapsed}ms (threshold: {SlowThresholdMs}ms) - SLOW!");
      }
      else
      {
        WriteLine($"[PERFORMANCE] {context.CommandName} completed in {elapsed}ms");
      }
    }
  }
}
