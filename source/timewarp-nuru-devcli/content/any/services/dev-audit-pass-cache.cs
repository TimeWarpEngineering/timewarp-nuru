#region Purpose
// The optional pass-cache hook for `dev audit` (kanban task 601): skip every check when
// these inputs already passed, and record a pass only when every check passed.
#endregion
#region Design
// DevAuditRunner asks for a key first. A null key means "these inputs cannot be named"
// (a dirty worktree, no git): the run goes ahead and records nothing. With a key and no
// --force, a recorded pass for that key skips the run. The runner records the key only
// after every check passed, so a failed run is never read back as a pass.
// A repo opts in through DevAuditChecks.ConfigurePassCache; DevAuditTreeStamp is the
// ready-made implementation. The in-process subset run (DevAuditChecks.Named) passes no
// cache, so it neither skips nor records.
#endregion

namespace DevCli;

/// <summary>
/// Remembers that <c>dev audit</c> passed for one set of inputs.
/// </summary>
internal interface IDevAuditPassCache
{
  /// <summary>
  /// A key for the current inputs, or <see langword="null"/> when they cannot be named
  /// (then the run goes ahead and nothing is recorded).
  /// </summary>
  ValueTask<string?> KeyAsync(DevAuditContext context, CancellationToken cancellationToken);

  /// <summary>True when a pass was recorded for <paramref name="key"/>.</summary>
  bool HasPass(DevAuditContext context, string key);

  /// <summary>Records a pass for <paramref name="key"/>. The runner calls this only after every check passed.</summary>
  void RecordPass(DevAuditContext context, string key);
}
