#region Purpose
// Classifies how far a release version has propagated across a repo's NuGet
// package set — pure function so the none/some/all matrix is unit-testable
// (kanban task 458-005, parent 458 finding F6).
#endregion
#region Design
// None    -> no package has the version published yet: safe to release.
// Partial -> some but not all packages have it published: a prior release run
//            failed partway through the push loop. Since the push uses
//            --skip-duplicate, resuming is safe — the caller should proceed
//            with a loud warning rather than abort.
// All     -> every package already has the version published: already
//            released: abort (bump the version).
// Guard rails (fail loud, matching ci-mode's unknown---mode precedent): a
// gate with zero packages is a config error, not a "None" result, so
// totalPackages <= 0 throws. alreadyPublishedCount outside [0, totalPackages]
// is an impossible state given how callers build the count (one increment
// per checked package) and also throws rather than silently clamping.
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
