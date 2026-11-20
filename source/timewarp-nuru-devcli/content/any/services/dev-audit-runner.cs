#region Purpose
// Runs the `dev audit` checks and prints their results (kanban task 600).
#endregion
#region Design
// Takes the check list as a parameter so tests drive it with fake checks; AuditCommand
// passes DevAuditChecks.All(). Checks run in order, one at a time, and every check runs
// even after one fails, so one run shows every problem. Each check gets one line:
// PASS or FAIL, the name, and its wall time. A failing check's message follows in full.
// A check that throws counts as failed with the exception text; cancellation is not
// caught. The last line is the passed/failed count. Everything goes to stdout so the
// order holds in a captured log.
//
// This never calls `ganda repo audit`: ganda repo audit calls `dev audit`.
#endregion

namespace DevCli;

using System.Diagnostics;
using System.Globalization;
using TimeWarp.Terminal;

/// <summary>Counts from one <c>dev audit</c> run.</summary>
internal readonly record struct DevAuditSummary(int Passed, int Failed)
{
  /// <summary>0 when no check failed, 1 otherwise.</summary>
  public int ExitCode => Failed > 0 ? 1 : 0;
}

/// <summary>Runs <see cref="IDevAuditCheck"/>s in order and prints one line each plus a summary.</summary>
internal static class DevAuditRunner
{
  public const string NothingToRunLine = "dev audit: no checks registered, nothing to run";

  public static async Task<DevAuditSummary> RunAsync
  (
    IReadOnlyList<IDevAuditCheck> checks,
    ITerminal terminal,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(checks);
    ArgumentNullException.ThrowIfNull(terminal);

    if (checks.Count == 0)
    {
      terminal.WriteLine(NothingToRunLine);
      return new DevAuditSummary(0, 0);
    }

    int passed = 0;
    int failed = 0;
    foreach (IDevAuditCheck check in checks)
    {
      long start = Stopwatch.GetTimestamp();
      DevAuditCheckResult result = await RunOneAsync(check, cancellationToken).ConfigureAwait(false);
      TimeSpan elapsed = Stopwatch.GetElapsedTime(start);

      string line = $"{(result.Passed ? "PASS" : "FAIL")} {check.Name} ({FormatElapsed(elapsed)})";
      terminal.WriteLine(result.Passed ? line.Green() : line.Red());
      if (!string.IsNullOrEmpty(result.Message))
      {
        terminal.WriteLine(result.Message);
      }

      if (result.Passed)
      {
        passed++;
      }
      else
      {
        failed++;
      }
    }

    string summary = $"dev audit: {passed} passed, {failed} failed";
    terminal.WriteLine(failed > 0 ? summary.Red() : summary.Green());
    return new DevAuditSummary(passed, failed);
  }

  public static string FormatElapsed(TimeSpan elapsed) =>
    elapsed.TotalSeconds < 1
      ? string.Create(CultureInfo.InvariantCulture, $"{elapsed.TotalMilliseconds:0}ms")
      : string.Create(CultureInfo.InvariantCulture, $"{elapsed.TotalSeconds:0.0}s");

  private static async Task<DevAuditCheckResult> RunOneAsync(IDevAuditCheck check, CancellationToken cancellationToken)
  {
    try
    {
      return await check.RunAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      return DevAuditCheckResult.Fail($"{exception.GetType().Name}: {exception.Message}");
    }
  }
}
