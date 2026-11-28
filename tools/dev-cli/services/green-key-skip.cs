#region Purpose
// Phase-2 skip rule: whether a run may skip or pack only, the force trailer, and which green runs can carry Packages-*.
#endregion

#region Design
// A run may skip only when it is a same-repo candidate, the key hit, and the head commit does not carry
// `Green-Key: full-run`. On a master push the carry is required: skip only when a green run for the key
// still has a live Packages-* artifact. Otherwise the master run packs only (it builds this commit and
// does not retest). Pull request runs never upload Packages-*, so a pull request hit skips with nothing
// to carry. Shadow mode (GreenKeyShadow.Enabled) logs this decision and does not act on it.
// The key does not hash the attestation git note, so an enabled skip still runs attestation.
#endregion

namespace DevCli;

/// <summary>The phase-2 skip rule, the force trailer, and carry-forward candidate order.</summary>
internal static class GreenKeySkip
{
  /// <summary>Commit trailer key that forces a full pipeline run.</summary>
  internal const string ForceTrailerKey = "Green-Key";

  /// <summary>Trailer value that forces a full pipeline run.</summary>
  internal const string ForceTrailerValue = "full-run";

  internal const string PackagesPrefix = "Packages-";

  /// <summary>True for a same-repo candidate whose key hit, unless the force trailer is set.</summary>
  internal static bool ShouldSkipPipeline(GreenKeyDisposition disposition, bool hit, bool forceFullRun) =>
    disposition == GreenKeyDisposition.Candidate && hit && !forceFullRun;

  /// <summary>
  /// What phase 2 would run. A miss, a non-candidate, or the force trailer is <see cref="GreenKeyRunKind.Full"/>.
  /// A candidate hit off master is <see cref="GreenKeyRunKind.Skip"/>. On a master push,
  /// <paramref name="carried"/> true is <see cref="GreenKeyRunKind.Skip"/> and false is
  /// <see cref="GreenKeyRunKind.PackOnly"/>.
  /// </summary>
  internal static GreenKeyRunKind RunKind(
    GreenKeyDisposition disposition,
    bool hit,
    bool forceFullRun,
    bool masterPush,
    bool carried)
  {
    if (!ShouldSkipPipeline(disposition, hit, forceFullRun))
    {
      return GreenKeyRunKind.Full;
    }

    if (!masterPush || carried)
    {
      return GreenKeyRunKind.Skip;
    }

    return GreenKeyRunKind.PackOnly;
  }

  /// <summary>True when any trailer value is <see cref="ForceTrailerValue"/>.</summary>
  internal static bool IsForceFullRun(string? trailerValues)
  {
    if (string.IsNullOrWhiteSpace(trailerValues))
    {
      return false;
    }

    foreach (string line in trailerValues.Split('\n'))
    {
      if (string.Equals(line.Trim(), ForceTrailerValue, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>True for a push to <c>refs/heads/master</c>, the only event that uploads Packages-*.</summary>
  internal static bool IsMasterPush(string? eventName, string? githubRef) =>
    string.Equals(eventName, "push", StringComparison.Ordinal)
    && string.Equals(githubRef, "refs/heads/master", StringComparison.Ordinal);

  /// <summary>Run ids of live green markers, newest first, each once.</summary>
  internal static IReadOnlyList<long> CarryCandidateRuns(IEnumerable<GreenKeyArtifactRow> rows)
  {
    ArgumentNullException.ThrowIfNull(rows);
    List<(long RunId, DateTimeOffset Created)> live = [];
    foreach (GreenKeyArtifactRow row in rows)
    {
      if (row.Expired || row.RunId is not long runId || runId <= 0)
      {
        continue;
      }

      DateTimeOffset created = DateTimeOffset.MinValue;
      if (row.CreatedAt is not null
        && DateTimeOffset.TryParse(row.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed))
      {
        created = parsed;
      }

      live.Add((runId, created));
    }

    return
    [
      .. live
        .OrderByDescending(entry => entry.Created)
        .ThenByDescending(entry => entry.RunId)
        .Select(entry => entry.RunId)
        .Distinct()
    ];
  }

  /// <summary>True when <paramref name="artifactsJson"/> lists a live <c>Packages-*</c> artifact.</summary>
  internal static bool HasLivePackagesArtifact(string? artifactsJson)
  {
    if (string.IsNullOrWhiteSpace(artifactsJson))
    {
      return false;
    }

    try
    {
      using JsonDocument document = JsonDocument.Parse(artifactsJson);
      if (!document.RootElement.TryGetProperty("artifacts", out JsonElement artifacts)
        || artifacts.ValueKind != JsonValueKind.Array)
      {
        return false;
      }

      foreach (JsonElement artifact in artifacts.EnumerateArray())
      {
        string? name = GreenKeyEvent.JsonText(artifact, "name");
        bool expired = artifact.TryGetProperty("expired", out JsonElement expiredElement)
          && expiredElement.ValueKind == JsonValueKind.True;
        if (!expired && name?.StartsWith(PackagesPrefix, StringComparison.Ordinal) == true)
        {
          return true;
        }
      }
    }
    catch (JsonException)
    {
      return false;
    }

    return false;
  }
}

/// <summary>What phase 2 would run after a green-key lookup. Shadow mode does not act on this.</summary>
internal enum GreenKeyRunKind
{
  /// <summary>The whole pipeline.</summary>
  Full,

  /// <summary>Attestation only. On master the carried Packages-* are republished.</summary>
  Skip,

  /// <summary>Master hit with nothing to carry: build and pack, no test.</summary>
  PackOnly
}

/// <summary>
/// Shadow mode logs <see cref="GreenKeyRunKind"/> and still runs the full pipeline.
/// The enable step, after the shadow window shows no key defect, is this constant plus the
/// workflow branch that acts on it. While it is true, <see cref="Effective"/> is always
/// <see cref="GreenKeyRunKind.Full"/>.
/// </summary>
internal static class GreenKeyShadow
{
  internal const bool Enabled = true;

  internal static GreenKeyRunKind Effective(GreenKeyRunKind kind) =>
    Enabled ? GreenKeyRunKind.Full : kind;
}
