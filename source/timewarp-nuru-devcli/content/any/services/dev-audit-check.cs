#region Purpose
// The extension point for `dev audit` (kanban task 600): one repo-specific pre-test
// check, and the pass or fail result it returns.
#endregion
#region Design
// A repo implements IDevAuditCheck in its own tools/dev-cli and registers it in
// DevAuditChecks.AddRepoChecks (dev-audit-checks.cs). The shared AuditCommand owns the
// `audit` route, so a repo never defines it. Checks must be fast: `ganda repo audit`
// runs `dev audit` before every walk, merge and manual audit. No full test suite here.
#endregion

namespace DevCli;

/// <summary>
/// One repo-specific pre-test check that <c>dev audit</c> runs.
/// </summary>
internal interface IDevAuditCheck
{
  /// <summary>Short name printed on the check's result line.</summary>
  string Name { get; }

  /// <summary>Runs the check. Return <see cref="DevAuditCheckResult.Fail"/> to fail <c>dev audit</c>.</summary>
  ValueTask<DevAuditCheckResult> RunAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The result of one <see cref="IDevAuditCheck"/>. A failing result's message is printed in full.
/// </summary>
internal readonly record struct DevAuditCheckResult(bool Passed, string? Message)
{
  /// <summary>The check passed. An optional message is printed under the result line.</summary>
  public static DevAuditCheckResult Pass(string? message = null) => new(true, message);

  /// <summary>The check failed. The message says what is wrong and how to fix it.</summary>
  public static DevAuditCheckResult Fail(string message) => new(false, message);
}
