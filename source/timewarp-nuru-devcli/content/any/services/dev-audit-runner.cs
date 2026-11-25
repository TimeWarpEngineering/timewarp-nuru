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
// Kanban task 601: each check gets the DevAuditContext (repo root, --force, --verbose).
// With a pass cache, the runner asks for the inputs' key before any check runs. A recorded
// pass for that key (and no --force) prints SkippedLine and runs nothing. The key is
// recorded only when every check passed. A cache that throws counts as "no key": the run
// goes ahead and records nothing, so a broken cache never skips a run. With no cache (the
// route in a repo that set none, or an in-process subset run) nothing is read or recorded.
// An empty check list never touches the cache.
//
// This never calls `ganda repo audit`: ganda repo audit calls `dev audit`.
#endregion

namespace DevCli;

using System.Diagnostics;
using System.Globalization;
using TimeWarp.Terminal;

/// <summary>Counts from one <c>dev audit</c> run.</summary>
/// <param name="Passed">Checks that passed.</param>
/// <param name="Failed">Checks that failed.</param>
/// <param name="Skipped">True when the pass cache skipped the run because these inputs already passed.</param>
internal readonly record struct DevAuditSummary(int Passed, int Failed, bool Skipped = false)
{
  /// <summary>0 when no check failed, 1 otherwise.</summary>
  public int ExitCode => Failed > 0 ? 1 : 0;
}

/// <summary>Runs <see cref="IDevAuditCheck"/>s in order and prints one line each plus a summary.</summary>
internal static class DevAuditRunner
{
  public const string NothingToRunLine = "dev audit: no checks registered, nothing to run";

  /// <summary>The start of the line printed when the pass cache skips the run.</summary>
  public const string SkippedLine = "dev audit: these inputs already passed";

  /// <summary>
  /// Runs <paramref name="checks"/> in order with <paramref name="context"/>. With a
  /// <paramref name="passCache"/>, skips the run when these inputs already passed (unless
  /// <see cref="DevAuditContext.Force"/>) and records a pass when every check passes.
  /// </summary>
  public static async Task<DevAuditSummary> RunAsync
  (
    IReadOnlyList<IDevAuditCheck> checks,
    DevAuditContext context,
    ITerminal terminal,
    IDevAuditPassCache? passCache,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(checks);
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(terminal);

    if (checks.Count == 0)
    {
      terminal.WriteLine(NothingToRunLine);
      return new DevAuditSummary(0, 0);
    }

    string? key = passCache is null
      ? null
      : await KeyAsync(passCache, context, terminal, cancellationToken).ConfigureAwait(false);
    if (key is not null && !context.Force && passCache!.HasPass(context, key))
    {
      string shortKey = key.Length > 12 ? key[..12] : key;
      terminal.WriteLine($"{SkippedLine} ({shortKey}); nothing run. --force runs every check.".Green());
      return new DevAuditSummary(0, 0, Skipped: true);
    }

    int passed = 0;
    int failed = 0;
    foreach (IDevAuditCheck check in checks)
    {
      long start = Stopwatch.GetTimestamp();
      DevAuditCheckResult result = await RunOneAsync(check, context, cancellationToken).ConfigureAwait(false);
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

    if (failed == 0 && key is not null)
    {
      passCache!.RecordPass(context, key);
    }

    string summary = $"dev audit: {passed} passed, {failed} failed";
    terminal.WriteLine(failed > 0 ? summary.Red() : summary.Green());
    return new DevAuditSummary(passed, failed);
  }

  public static string FormatElapsed(TimeSpan elapsed) =>
    elapsed.TotalSeconds < 1
      ? string.Create(CultureInfo.InvariantCulture, $"{elapsed.TotalMilliseconds:0}ms")
      : string.Create(CultureInfo.InvariantCulture, $"{elapsed.TotalSeconds:0.0}s");

  private static async Task<string?> KeyAsync
  (
    IDevAuditPassCache passCache,
    DevAuditContext context,
    ITerminal terminal,
    CancellationToken cancellationToken
  )
  {
    try
    {
      return await passCache.KeyAsync(context, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      if (context.Verbose)
      {
        terminal.WriteLine($"dev audit: pass cache unavailable, running every check ({exception.GetType().Name}: {exception.Message})");
      }

      return null;
    }
  }

  private static async Task<DevAuditCheckResult> RunOneAsync
  (
    IDevAuditCheck check,
    DevAuditContext context,
    CancellationToken cancellationToken
  )
  {
    try
    {
      return await check.RunAsync(context, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      return DevAuditCheckResult.Fail($"{exception.GetType().Name}: {exception.Message}");
    }
  }
}
