#region Purpose
// CI mode detection for the dev CLI workflow command — pure function so the
// event-name -> mode matrix is unit-testable (kanban task 458-001).
#endregion
#region Design
// Explicit --mode always wins (break-glass release via workflow_dispatch relies
// on this). Auto-detection maps GITHUB_EVENT_NAME:
//   pull_request      -> Pr
//   push              -> Merge
//   release           -> Release
//   workflow_dispatch -> Merge   (manual dispatch must never publish by default)
//   anything else     -> Pr      (local dev default)
#endregion

namespace DevCli;

public enum CiMode
{
  Pr,
  Merge,
  Release
}

public static class CiModeDetector
{
  public static CiMode DetermineMode(string? explicitMode, string? eventName)
  {
    if (!string.IsNullOrEmpty(explicitMode))
    {
      return explicitMode.ToLowerInvariant() switch
      {
        "pr" => CiMode.Pr,
        "merge" => CiMode.Merge,
        "release" => CiMode.Release,
        _ => CiMode.Pr
      };
    }

    return eventName switch
    {
      "pull_request" => CiMode.Pr,
      "push" => CiMode.Merge,
      "release" => CiMode.Release,
      "workflow_dispatch" => CiMode.Merge,
      _ => CiMode.Pr
    };
  }
}
