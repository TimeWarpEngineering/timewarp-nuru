#region Purpose
// Pure per-guard classifiers for `dev release` (kanban task 458-006, parent 458
// finding F7). Each guard turns already-gathered state (git/gh output the
// caller captured) into a pass/fail verdict with an operator-facing reason —
// no process execution here, so the guard matrix is unit-testable without
// invoking git or gh. release-command.cs owns the git/gh orchestration and the
// fail-fast order these are called in (D1: tree -> branch -> sync -> tag ->
// publish-state -> CI run).
#endregion
#region Design
// CheckWorkingTree: `git status --porcelain` is empty (or whitespace-only) iff
// the tree is clean — any other output is dirty, regardless of content.
//
// CheckBranch: releases are cut from master only (convention.md). Ordinal
// comparison; a null/empty/"HEAD" (detached) or any other branch name fails,
// naming the actual value so the operator does not have to go look.
//
// CheckSync: local master vs origin/master ahead/behind counts (`git rev-list
// --count`). 0/0 is in sync. Ahead-only means unpushed local commits (push
// first); behind-only means origin has moved (pull first); both nonzero is a
// genuine divergence needing manual reconciliation — three distinct messages,
// matching the repo's fail-loud precedent of not collapsing distinct verdicts
// into one message (ci-run-promotion.cs LocateRunStatus, workflow-command.cs
// TagPinStatus/AncestorCheckStatus). Negative counts are impossible from a
// real `git rev-list --count` and indicate a caller bug, not a guard outcome
// — both throw ArgumentOutOfRangeException (publish-state.cs precedent).
//
// CheckTagAvailability: a release must create tag v{Version} from nothing —
// if it already exists anywhere (local clone, origin, or both), a NEW tag
// here would either fail outright or (worse) succeed pointing at a different
// commit than the earlier tag, minting exactly the mixed-commit hazard this
// task exists to close. Only "exists nowhere" passes; the reason names WHERE
// it already exists and gives the same resume-vs-bump guidance as
// publish-state.cs's Partial case: resume from the commit that tag already
// points at via break-glass, or bump the version.
//
// CheckPublishState: reuses PublishStateClassifier's None/Partial/All output
// at the point release runs BEFORE any tag exists (guard order: tag-
// availability precedes this). That ordering means Partial here can only mean
// an untagged break-glass push — a prior release attempt reached NuGet
// without ever getting this far in `dev release` (e.g. an old manual
// tag-then-publish, or a break-glass run under the old two-act process) — so
// the guidance differs from check-version-command.cs's Partial message (which
// assumes a resumable, tag-pinned commit): here there is no tag to resume
// from, so a NEW tag at this commit could pin a different source than the
// partial push used. All means already fully released — bump. None passes.
#endregion

namespace DevCli;

/// <summary>
/// Outcome of a single release guard: <see cref="Ok"/> plus, when failed, an
/// operator-facing <see cref="Reason"/>.
/// </summary>
public sealed record GuardVerdict(bool Ok, string? Reason)
{
  public static GuardVerdict Pass() => new(true, null);

  public static GuardVerdict Fail(string reason) => new(false, reason);
}

/// <summary>
/// Pure per-guard classifiers for the `dev release` pipeline. See the Design
/// region above for the rationale behind each guard's pass/fail boundary.
/// </summary>
public static class ReleaseGuard
{
  public static GuardVerdict CheckWorkingTree(string statusPorcelainOutput)
  {
    ArgumentNullException.ThrowIfNull(statusPorcelainOutput);

    return string.IsNullOrWhiteSpace(statusPorcelainOutput)
      ? GuardVerdict.Pass()
      : GuardVerdict.Fail("working tree is dirty — commit or stash first.");
  }

  public static GuardVerdict CheckBranch(string? currentBranch)
  {
    if (string.Equals(currentBranch, "master", StringComparison.Ordinal))
    {
      return GuardVerdict.Pass();
    }

    string display = currentBranch switch
    {
      null => "(unknown)",
      "" => "(unknown)",
      "HEAD" => "HEAD (detached)",
      _ => currentBranch
    };

    return GuardVerdict.Fail($"release must be cut from master — current branch is {display}.");
  }

  public static GuardVerdict CheckSync(int aheadCount, int behindCount)
  {
    if (aheadCount < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(aheadCount), aheadCount, "aheadCount cannot be negative.");
    }

    if (behindCount < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(behindCount), behindCount, "behindCount cannot be negative.");
    }

    if (aheadCount == 0 && behindCount == 0)
    {
      return GuardVerdict.Pass();
    }

    if (aheadCount > 0 && behindCount == 0)
    {
      return GuardVerdict.Fail($"local master is {aheadCount} commit(s) ahead of origin/master — push first.");
    }

    if (behindCount > 0 && aheadCount == 0)
    {
      return GuardVerdict.Fail($"local master is {behindCount} commit(s) behind origin/master — pull first.");
    }

    return GuardVerdict.Fail($"local master and origin/master have diverged ({aheadCount} ahead, {behindCount} behind) — reconcile before releasing.");
  }

  public static GuardVerdict CheckTagAvailability(string tag, bool existsLocally, bool existsOnRemote)
  {
    ArgumentNullException.ThrowIfNull(tag);

    if (!existsLocally && !existsOnRemote)
    {
      return GuardVerdict.Pass();
    }

    string where = (existsLocally, existsOnRemote) switch
    {
      (true, true) => "locally and on origin",
      (true, false) => "locally (not on origin)",
      (false, true) => "on origin (not locally)",
      _ => "somewhere unexpected"
    };

    return GuardVerdict.Fail($"tag {tag} already exists {where} — resume from that commit via break-glass, or bump the version.");
  }

  public static GuardVerdict CheckPublishState(PublishState state, string version)
  {
    ArgumentNullException.ThrowIfNull(version);

    switch (state)
    {
      case PublishState.None:
        return GuardVerdict.Pass();

      case PublishState.All:
        return GuardVerdict.Fail($"version {version} was already released — bump the version.");

      case PublishState.Partial:
        return GuardVerdict.Fail($"version {version} is partially published but was never tagged — resume via break-glass from the ORIGINAL commit, or bump the version.");

      default:
        throw new InvalidOperationException($"Unknown publish state '{state}'.");
    }
  }
}
