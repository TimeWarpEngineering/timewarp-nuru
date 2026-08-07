#region Purpose
// Classifies how far a release version has propagated across a repo's NuGet
// package set — pure function so the none/some/all matrix is unit-testable
// (kanban task 458-005, parent 458 finding F6). Also parses the comma-
// separated --package / dev.jsonc package-list input, so a delimiter-only
// or blank list is caught by the caller's "no packages specified" branch
// instead of reaching Classify with a zero package count (round-1 review
// finding #1: an unguarded Classify(0, 0) crashed with an unhandled
// ArgumentOutOfRangeException on input like "--package \",\"").
#endregion
#region Design
// None    -> no package has the version published yet: safe to release.
// Partial -> some but not all packages have it published: a prior release run
//            failed partway through the push loop. Resuming is safe from the
//            SAME commit that produced the earlier partial push — the release
//            pipeline's tag-pin gate (workflow-command.cs, 458-005 round-1
//            finding #3) enforces this whenever a tag for the version already
//            exists; if the source has changed since, bump the version
//            instead of resuming. --skip-duplicate only makes the re-push
//            idempotent at the HTTP layer — it does not verify byte identity.
// All     -> every package already has the version published: already
//            released: abort (bump the version).
// Guard rails (fail loud, matching ci-mode's unknown---mode precedent): a
// gate with zero packages is a config error, not a "None" result, so
// totalPackages <= 0 throws. alreadyPublishedCount outside [0, totalPackages]
// is an impossible state given how callers build the count (one increment
// per checked package) and also throws rather than silently clamping.
// Callers should therefore route ParsePackageList(...).Count == 0 to their own
// "no packages specified" error rather than calling Classify(0, ...).
#endregion

namespace DevCli;

public enum PublishState
{
  None,
  Partial,
  All
}

public static class PublishStateClassifier
{
  // Split on commas, trimming whitespace and dropping empty entries; null or
  // blank input yields an empty list. A delimiter-only input (e.g. ",")
  // therefore also yields an empty list — callers must check Count == 0
  // themselves before calling Classify, since a zero package count is a
  // config error there, not a valid Classify argument.
  public static IReadOnlyList<string> ParsePackageList(string? input)
  {
    if (string.IsNullOrWhiteSpace(input))
    {
      return [];
    }

    return input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
  }

  public static PublishState Classify(int totalPackages, int alreadyPublishedCount)
  {
    if (totalPackages <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(totalPackages), totalPackages, "totalPackages must be greater than zero — a release gate with no packages is a configuration error.");
    }

    if (alreadyPublishedCount < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(alreadyPublishedCount), alreadyPublishedCount, "alreadyPublishedCount cannot be negative.");
    }

    if (alreadyPublishedCount > totalPackages)
    {
      throw new ArgumentOutOfRangeException(nameof(alreadyPublishedCount), alreadyPublishedCount, $"alreadyPublishedCount cannot exceed totalPackages ({totalPackages}).");
    }

    if (alreadyPublishedCount == 0)
    {
      return PublishState.None;
    }

    if (alreadyPublishedCount >= totalPackages)
    {
      return PublishState.All;
    }

    return PublishState.Partial;
  }
}
