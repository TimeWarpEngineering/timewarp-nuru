#region Purpose
// Pure per-guard classifiers for `dev release` (kanban task 458-006, parent 458
// finding F7). Each guard turns already-gathered state (git/gh output the
// caller captured) into a pass/fail verdict with an operator-facing reason —
// no process execution here, so the guard matrix is unit-testable without
// invoking git or gh. release-command.cs owns the git/gh orchestration and the
// fail-fast order these are called in (D1: tree -> branch -> sync -> tag ->
// publish-state -> release target, the newest green commit with only kanban/
// commits after it).
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
//
// IsPrerelease / BuildReleaseCreateArguments (kanban task 486): the GitHub
// Release prerelease flag mirrors the version the same way NuGet does — a
// SemVer prerelease suffix (a `-` before any `+build` metadata) means
// prerelease. NuGet.Versioning is not referenced by this source-only package,
// so this is a plain string check, not NuGetVersion.IsPrerelease. The gh
// argument list is built in ONE place so the --dry-run / recovery string and
// the executed WithArguments(...) cannot drift.
//
// SelectReleaseTarget (kanban task 543): the CI path filters give a commit
// that changes only kanban/ no run at all, and board commits land on master
// at any moment. Requiring a green run at HEAD turned every release into a
// race against board traffic. The input is first-parent history from HEAD,
// newest first, as far as the caller walked it. The target is the newest
// commit with a successful run, provided every commit before it in the list
// changes only kanban/ paths. Such a commit cannot change <Version> or any
// packed file, so the tagged commit's artifact is the HEAD artifact.
// The first non-kanban commit found (walking newest first) ends the walk with
// today's refusal plus that commit and path: the code at HEAD is untested.
// An exhausted list (the caller's bounded walk, ReleaseTargetWalkLimit) is
// today's refusal unchanged. Only the kanban/ prefix is exempt. workflow.yml
// path filters list more paths (documentation/, skills/, ...), but kanban/
// is the only observed case and reading the filters is out of scope (543).
// A commit with no changed paths (empty commit) changes nothing outside
// kanban/, so it is skipped too.
//
// CheckTargetVersion: the tag and publish-state guards check the version
// read from the working tree (HEAD). When the target is behind HEAD, the
// props file is re-read at the target and must match. A kanban-only walk
// makes a mismatch impossible, so a mismatch is a caller bug or a
// rewritten history, and it refuses rather than tag a different version.
// A null target version means the props file did not parse or had no
// <Version>; that refusal names the props file, not a version mismatch.
// A failed `git show` never reaches here: the caller reports it as a git
// error with stderr (543 review M2).
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
/// One first-parent commit walked back from HEAD: its sha, the paths it changes
/// against its first parent, and the id of its preferred successful
/// <c>workflow.yml</c> run (<see langword="null"/> when it has none).
/// </summary>
public sealed record ReleaseTargetCommit(string Sha, IReadOnlyList<string> ChangedPaths, long? SuccessfulRunId);

/// <summary>
/// Outcome of <see cref="ReleaseGuard.SelectReleaseTarget"/>: the commit to tag, its
/// CI run, and how many kanban-only commits HEAD is past it. When it fails,
/// <see cref="Reason"/> holds the operator-facing refusal.
/// </summary>
public sealed record ReleaseTargetVerdict(bool Ok, string? TargetSha, long? RunId, int SkippedCommitCount, string? Reason)
{
  public static ReleaseTargetVerdict Target(string targetSha, long runId, int skippedCommitCount) =>
    new(true, targetSha, runId, skippedCommitCount, null);

  public static ReleaseTargetVerdict Fail(string reason) => new(false, null, null, 0, reason);
}

/// <summary>
/// Pure per-guard classifiers for the `dev release` pipeline. See the Design
/// region above for the rationale behind each guard's pass/fail boundary.
/// </summary>
public static class ReleaseGuard
{
  /// <summary>Paths under this prefix never ship, so commits that touch only them may sit past the tagged commit.</summary>
  public const string NonShippingPathPrefix = "kanban/";

  /// <summary>How many first-parent commits back from HEAD the release-target walk looks for a green run.</summary>
  public const int ReleaseTargetWalkLimit = 50;

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

  /// <summary>
  /// The first path in <paramref name="changedPaths"/> outside
  /// <see cref="NonShippingPathPrefix"/>, or <see langword="null"/> when every path is under it.
  /// </summary>
  public static string? FindShippingPath(IReadOnlyList<string> changedPaths)
  {
    ArgumentNullException.ThrowIfNull(changedPaths);

    return changedPaths.FirstOrDefault(path => !path.StartsWith(NonShippingPathPrefix, StringComparison.Ordinal));
  }

  public static ReleaseTargetVerdict SelectReleaseTarget(string headSha, IReadOnlyList<ReleaseTargetCommit> commitsNewestFirst)
  {
    ArgumentNullException.ThrowIfNull(headSha);
    ArgumentNullException.ThrowIfNull(commitsNewestFirst);

    string noRunReason = FormatNoSuccessfulRun(headSha);

    for (int index = 0; index < commitsNewestFirst.Count; index++)
    {
      ReleaseTargetCommit commit = commitsNewestFirst[index];

      if (commit.SuccessfulRunId is long runId)
      {
        return ReleaseTargetVerdict.Target(commit.Sha, runId, index);
      }

      string? shippingPath = FindShippingPath(commit.ChangedPaths);
      if (shippingPath is not null)
      {
        int otherPaths = commit.ChangedPaths.Count(path => !path.StartsWith(NonShippingPathPrefix, StringComparison.Ordinal)) - 1;
        string more = otherPaths > 0 ? $" (and {otherPaths} more)" : "";
        return ReleaseTargetVerdict.Fail($"{noRunReason} Commit {commit.Sha} changes {shippingPath}{more}, outside {NonShippingPathPrefix}, so no earlier green run can stand in for it.");
      }
    }

    return ReleaseTargetVerdict.Fail(noRunReason);
  }

  public static GuardVerdict CheckTargetVersion(string headVersion, string? targetVersion, string targetSha)
  {
    ArgumentNullException.ThrowIfNull(headVersion);
    ArgumentNullException.ThrowIfNull(targetSha);

    if (string.Equals(headVersion, targetVersion, StringComparison.Ordinal))
    {
      return GuardVerdict.Pass();
    }

    if (targetVersion is null)
    {
      return GuardVerdict.Fail($"source/Directory.Build.props at release target {targetSha} is unreadable: it does not parse or has no <Version>. Fix the props file and wait for CI on the fix.");
    }

    return GuardVerdict.Fail($"<Version> at release target {targetSha} is {targetVersion}, but HEAD has {headVersion} — the commits between them are not kanban-only. Wait for CI on HEAD.");
  }

  public static bool IsPrerelease(string version)
  {
    ArgumentNullException.ThrowIfNull(version);

    int buildMetadataStart = version.IndexOf('+', StringComparison.Ordinal);
    string withoutBuildMetadata = buildMetadataStart >= 0 ? version[..buildMetadataStart] : version;
    return withoutBuildMetadata.Contains('-', StringComparison.Ordinal);
  }

  public static IReadOnlyList<string> BuildReleaseCreateArguments(string tag, string version)
  {
    ArgumentNullException.ThrowIfNull(tag);
    ArgumentNullException.ThrowIfNull(version);

    List<string> arguments = ["release", "create", tag, "--title", tag, "--generate-notes", "--verify-tag"];
    if (IsPrerelease(version))
    {
      arguments.Add("--prerelease");
    }

    return arguments;
  }

  public static string FormatReleaseCreateCommand(IReadOnlyList<string> arguments)
  {
    ArgumentNullException.ThrowIfNull(arguments);

    return $"gh {string.Join(' ', arguments)}";
  }

  private static string FormatNoSuccessfulRun(string headSha) =>
    $"no successful CI run of workflow.yml exists for {headSha}. The Release event would fail at locate-run — wait for CI on this commit or rerun it (gh run rerun <id>).";
}
