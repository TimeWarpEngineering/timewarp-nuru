// ═══════════════════════════════════════════════════════════════════════════════
// PERFORMANCE BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// Timing and slow command warnings.

namespace PipelineCombined.Behaviors;

using System.Diagnostics;
using TimeWarp.Nuru;
using static System.Console;

public sealed class PerformanceBehavior : INuruBehavior
{
  private const int Threshold = 500;

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    Stopwatch sw = Stopwatch.StartNew();
    await proceed();
    sw.Stop();

    if (sw.ElapsedMilliseconds > Threshold)
      WriteLine($"[PERF] SLOW: {context.CommandName} took {sw.ElapsedMilliseconds}ms");
    else
      WriteLine($"[PERF] {context.CommandName} completed in {sw.ElapsedMilliseconds}ms");
  }
}
