#region Purpose
// Pure logic + DTOs for release-mode artifact promotion: locate the CI run that
// tested the tag's exact commit, pick its Packages-* artifact, and verify the
// downloaded .nupkg set against the derived packable set before anything is
// pushed (kanban task 458-002, Design B — build-once/promote). No process
// execution here — gh/git orchestration stays in workflow-command.cs so this
// file is testable without invoking gh.
#endregion
#region Design
// OrderCandidateRuns: `gh run list --json databaseId,event,headSha,createdAt`
// returns every recent run regardless of commit, so callers must filter to the
// tag's headSha first. Among matches, a push-event run (ordinary master-merge
// CI) is preferred over a release-event run at the SAME sha — a release-event
// run only re-runs the same pipeline and, pre-458-002, uploaded its own rebuilt
// (untested-by-a-separate-step) artifact; the push run is the one that ran the
// full pr pipeline (build -> verify-samples -> test) against this exact commit.
// createdAt is an RFC3339 string (`2026-06-15T14:34:37Z`); ordinal string
// comparison sorts these chronologically without a DateTime parse, so
// newest-first is a plain ordinal descending sort. databaseId desc is the final
// tiebreaker for any remaining tie (two runs can share a createdAt second).
//
// SelectPackagesArtifact: workflow.yml uploads the nupkg set as
// `Packages-{run_number}` (upload step, always-run). "Packages-" is an Ordinal
// prefix match — GitHub Actions artifacts expire (default/repo retention), and
// an expired artifact is still listed by the REST API with expired=true and
// cannot be downloaded. Multiple non-expired matches (should not normally
// happen for one run, but the type does not forbid it) resolve to the
// highest-Id artifact deterministically. If every "Packages-*" match is
// expired, that is a distinct outcome (Expired, with the names) from no match
// at all (NoneMatching) — the caller's abort message differs (expiry needs
// "re-run CI" guidance; NoneMatching means this run never uploaded one, so the
// caller should keep walking candidates).
//
// VerifyPackageSet: mirrors the push step's existing cross-check
// (PushPackagesAsync) but runs against the DOWNLOADED artifact contents rather
// than a fresh `dotnet pack` output. Expected file names are
// "{PackageId}.{version}.nupkg" per project in the derived packable set
// (Ordinal). Missing = expected names absent from the actual set (the CI run's
// artifact predates a since-added package, or something went wrong uploading
// it). Unexpected = ANY .nupkg in the actual set not exactly matching an
// expected name, regardless of why — a stray package, a leftover from a
// renamed project, or the same PackageId at a stale version (the CI run
// predates the version bump) all count; the caller does not need to
// distinguish which, since all of them mean "these exact bytes must not ship
// under this version."
#endregion

namespace DevCli;

using System.Text.Json.Serialization;

/// <summary>
/// One row of <c>gh run list --json databaseId,event,headSha,createdAt</c>.
/// </summary>
public sealed class CiRunSummary
{
  [JsonPropertyName("databaseId")]
  public long DatabaseId { get; init; }

  [JsonPropertyName("event")]
  public string Event { get; init; } = "";

  [JsonPropertyName("headSha")]
  public string HeadSha { get; init; } = "";

  [JsonPropertyName("createdAt")]
  public string CreatedAt { get; init; } = "";
}

/// <summary>
/// One artifact entry from <c>gh api repos/{owner}/{repo}/actions/runs/{id}/artifacts</c>.
/// </summary>
public sealed class RunArtifact
{
  [JsonPropertyName("id")]
  public long Id { get; init; }

  [JsonPropertyName("name")]
  public string Name { get; init; } = "";

  [JsonPropertyName("expired")]
  public bool Expired { get; init; }
}

/// <summary>
/// Response envelope for the run-artifacts REST endpoint.
/// </summary>
public sealed class RunArtifactListResponse
{
  [JsonPropertyName("artifacts")]
  public IReadOnlyList<RunArtifact> Artifacts { get; init; } = [];
}

/// <summary>
/// Outcome of selecting a run's "Packages-*" artifact.
/// </summary>
public enum PackagesArtifactStatus
{
  Found,
  NoneMatching,
  Expired
}

/// <summary>
/// Result of <see cref="CiRunPromotion.SelectPackagesArtifact"/>. <c>Artifact</c>
/// is set only when Status is Found; <c>ExpiredNames</c> is set only when Status
/// is Expired (the names of every matching-but-expired artifact, for the abort
/// message).
/// </summary>
public sealed record PackagesArtifactOutcome(PackagesArtifactStatus Status, RunArtifact? Artifact, IReadOnlyList<string> ExpiredNames);

/// <summary>
/// Result of <see cref="CiRunPromotion.VerifyPackageSet"/>. <see cref="IsMatch"/>
/// is true only when both lists are empty.
/// </summary>
public sealed record PackageSetVerification(IReadOnlyList<string> Missing, IReadOnlyList<string> Unexpected)
{
  public bool IsMatch => Missing.Count == 0 && Unexpected.Count == 0;
}

/// <summary>
/// Pure logic for locating and verifying the CI-tested artifact set to promote
/// during release. No process execution — see workflow-command.cs for the
/// gh/git orchestration that calls into this.
/// </summary>
public static class CiRunPromotion
{
  /// <summary>
  /// Filters <paramref name="runs"/> to those at <paramref name="headSha"/>
  /// (Ordinal), ordered push-event first, then by CreatedAt descending
  /// (ordinal string compare — RFC3339 timestamps sort chronologically),
  /// then by DatabaseId descending.
  /// </summary>
  public static IReadOnlyList<CiRunSummary> OrderCandidateRuns(IReadOnlyList<CiRunSummary> runs, string headSha)
  {
    ArgumentNullException.ThrowIfNull(runs);
    ArgumentNullException.ThrowIfNull(headSha);

    return
    [
      .. runs
        .Where(run => string.Equals(run.HeadSha, headSha, StringComparison.Ordinal))
        .OrderBy(run => string.Equals(run.Event, "push", StringComparison.Ordinal) ? 0 : 1)
        .ThenByDescending(run => run.CreatedAt, StringComparer.Ordinal)
        .ThenByDescending(run => run.DatabaseId)
    ];
  }

  /// <summary>
  /// Selects the "Packages-*" artifact to download from a run's artifact list.
  /// Non-expired matches win on highest Id; all-expired matches yield
  /// <see cref="PackagesArtifactStatus.Expired"/> with their names; no match
  /// yields <see cref="PackagesArtifactStatus.NoneMatching"/>.
  /// </summary>
  public static PackagesArtifactOutcome SelectPackagesArtifact(IReadOnlyList<RunArtifact> artifacts)
  {
    ArgumentNullException.ThrowIfNull(artifacts);

    List<RunArtifact> matching = [.. artifacts.Where(artifact => artifact.Name.StartsWith("Packages-", StringComparison.Ordinal))];

    if (matching.Count == 0)
    {
      return new PackagesArtifactOutcome(PackagesArtifactStatus.NoneMatching, null, []);
    }

    List<RunArtifact> live = [.. matching.Where(artifact => !artifact.Expired)];

    if (live.Count > 0)
    {
      RunArtifact best = live.OrderByDescending(artifact => artifact.Id).First();
      return new PackagesArtifactOutcome(PackagesArtifactStatus.Found, best, []);
    }

    return new PackagesArtifactOutcome(PackagesArtifactStatus.Expired, null, [.. matching.Select(artifact => artifact.Name)]);
  }

  /// <summary>
  /// Verifies a downloaded artifact's .nupkg file names against the derived
  /// packable set at <paramref name="version"/>. Expected name per project is
  /// <c>{PackageId}.{version}.nupkg</c> (Ordinal). Missing lists expected names
  /// absent from <paramref name="actualFileNames"/>; Unexpected lists ANY
  /// actual .nupkg name that is not exactly an expected name, regardless of
  /// version or PackageId.
  /// </summary>
  public static PackageSetVerification VerifyPackageSet(IReadOnlyList<string> actualFileNames, IReadOnlyList<PackableProject> projects, string version)
  {
    ArgumentNullException.ThrowIfNull(actualFileNames);
    ArgumentNullException.ThrowIfNull(projects);
    ArgumentNullException.ThrowIfNull(version);

    HashSet<string> actualSet = new(actualFileNames, StringComparer.Ordinal);
    HashSet<string> expectedSet = new(StringComparer.Ordinal);

    List<string> missing = [];
    foreach (PackableProject project in projects)
    {
      string expectedFileName = $"{project.PackageId}.{version}.nupkg";
      expectedSet.Add(expectedFileName);

      if (!actualSet.Contains(expectedFileName))
      {
        missing.Add(expectedFileName);
      }
    }

    List<string> unexpected = [.. actualFileNames.Where(fileName => !expectedSet.Contains(fileName))];

    return new PackageSetVerification(missing, unexpected);
  }
}
