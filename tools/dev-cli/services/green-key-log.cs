#region Purpose
// Log lines and the job summary for the green-key ledger.
#endregion

#region Design
// Lines say mode=skip. A run that skips says skip=yes; a master hit that only builds and packs says
// skip=pack-only. A tally must not read the old shadow=log-only lines as skips. false_skip=true is a
// hit whose full run then failed. key-skipped, key-skip-failed, pack-only, path-skipped, and cancelled
// are rate=out and false_skip=false. rate=in is a run that spent build and test minutes (succeeded or failed).
#endregion

namespace DevCli;

/// <summary>Green-key log lines for <c>dev workflow</c>.</summary>
internal static class GreenKeyLog
{
  internal const string Succeeded = "succeeded";

  internal const string Failed = "failed";

  internal const string PathSkipped = "path-skipped";

  /// <summary>A cancelled stage. Rate-out, and not a false skip.</summary>
  internal const string Cancelled = "cancelled";

  /// <summary>A green-key hit that skipped build and test.</summary>
  internal const string KeySkipped = "key-skipped";

  /// <summary>A green-key skip whose attestation or package carry failed.</summary>
  internal const string KeySkipFailed = "key-skip-failed";

  /// <summary>A pack-only run whose build failed. Rate-out, and not a false skip.</summary>
  internal const string PackOnlyFailed = "pack-only-failed";

  /// <summary>Workflow output for a full pipeline.</summary>
  internal const string RunFull = "full";

  /// <summary>Workflow output for a skip.</summary>
  internal const string RunSkip = "skip";

  /// <summary>Workflow output and pipeline value for a pack-only run.</summary>
  internal const string RunPackOnly = "pack-only";

  private const string FormatId = GreenKeyManifest.FormatId;

  private static string Mode => GreenKeyShadow.Enabled ? " shadow=log-only" : " mode=skip";

  private static readonly Regex Secret = new(
    @"(sig|token|password|Bearer)(\s*[=: ]\s*)([^&\s]+)",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
    TimeSpan.FromSeconds(1));

  /// <summary>One log line for hit, miss, or excluded. The would-line says what phase 2 would do.</summary>
  internal static string FormatObservation(
    GreenKeyDisposition disposition,
    string reason,
    string key,
    string runId,
    string commit,
    string? lookup,
    GreenKeyMarker? marker)
  {
    ArgumentNullException.ThrowIfNull(reason);
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(runId);
    ArgumentNullException.ThrowIfNull(commit);
    string writer = disposition == GreenKeyDisposition.Candidate ? "yes" : "no";
    if (disposition == GreenKeyDisposition.Excluded)
    {
      return FormatId + Mode + " outcome=excluded reason=" + One(reason)
        + " key=" + One(key) + " run=" + One(runId) + " commit=" + One(commit);
    }

    if (marker is not null)
    {
      return FormatId + Mode + " outcome=hit key=" + One(key)
        + " run=" + One(runId) + " commit=" + One(commit)
        + " marker_run=" + marker.RunId.ToString(CultureInfo.InvariantCulture)
        + " marker_commit=" + One(marker.Commit)
        + " marker_artifact=" + marker.ArtifactId.ToString(CultureInfo.InvariantCulture)
        + " writer=" + writer;
    }

    string lookupText = string.IsNullOrWhiteSpace(lookup) ? "ok" : One(lookup);
    return FormatId + Mode + " outcome=miss key=" + One(key)
      + " run=" + One(runId) + " commit=" + One(commit)
      + " lookup=" + lookupText + " writer=" + writer;
  }

  /// <summary>Second log line: the inputs that explain a later false skip.</summary>
  internal static string FormatDetail(string sdk, string runnerImage, int imageCount, int treeCount)
  {
    ArgumentNullException.ThrowIfNull(sdk);
    ArgumentNullException.ThrowIfNull(runnerImage);
    return FormatId + " detail sdk=" + One(sdk)
      + " runner=" + One(runnerImage)
      + " images=" + imageCount.ToString(CultureInfo.InvariantCulture)
      + " tree_entries=" + treeCount.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>Observation line when the key could not be computed.</summary>
  internal static string FormatError(string? error, string runId, string commit)
  {
    ArgumentNullException.ThrowIfNull(runId);
    ArgumentNullException.ThrowIfNull(commit);
    return FormatId + Mode + " outcome=error error=" + One(Redact(error))
      + " run=" + One(runId) + " commit=" + One(commit);
  }

  /// <summary>
  /// Phase-2 decision line. <paramref name="would"/> is <c>full</c>, <c>skip</c>, or <c>pack-only</c>.
  /// A hit the workflow acts on uses <see cref="FormatActed"/> instead.
  /// </summary>
  internal static string FormatWould(string would, string reason, string key, string runId, string commit, long? carryRunId)
  {
    ArgumentNullException.ThrowIfNull(would);
    ArgumentNullException.ThrowIfNull(reason);
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(runId);
    ArgumentNullException.ThrowIfNull(commit);
    string line = FormatId + Mode + " would=" + One(would) + " reason=" + One(reason)
      + " key=" + One(key) + " run=" + One(runId) + " commit=" + One(commit);
    if (carryRunId is long carry)
    {
      line += " carry_run=" + carry.ToString(CultureInfo.InvariantCulture);
    }

    return line;
  }

  /// <summary>Maps the phase-2 would string onto a run kind.</summary>
  internal static GreenKeyRunKind RunKindFromWould(string would) => would switch
  {
    RunSkip => GreenKeyRunKind.Skip,
    RunPackOnly => GreenKeyRunKind.PackOnly,
    _ => GreenKeyRunKind.Full
  };

  /// <summary>Workflow output value for <paramref name="kind"/>.</summary>
  internal static string RunKindName(GreenKeyRunKind kind) => kind switch
  {
    GreenKeyRunKind.Skip => RunSkip,
    GreenKeyRunKind.PackOnly => RunPackOnly,
    _ => RunFull
  };

  /// <summary>
  /// The line a tally counts, for a hit. Null when <paramref name="earlier"/> is not a hit:
  /// the caller logs <see cref="FormatWould"/> instead. A skip names the trusted marker and,
  /// on master, the run whose <c>Packages-*</c> it carries.
  /// </summary>
  internal static string? FormatActed(
    GreenKeyRunKind kind,
    string reason,
    string key,
    string runId,
    string commit,
    GreenKeyMarker? marker,
    long? carryRunId,
    string earlier)
  {
    ArgumentNullException.ThrowIfNull(reason);
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(runId);
    ArgumentNullException.ThrowIfNull(commit);
    ArgumentNullException.ThrowIfNull(earlier);
    if (!string.Equals(earlier, "hit", StringComparison.Ordinal))
    {
      return null;
    }

    if (kind == GreenKeyRunKind.Skip && marker is not null)
    {
      return FormatSkip(key, runId, commit, marker, carryRunId, null);
    }

    if (kind == GreenKeyRunKind.PackOnly && marker is not null)
    {
      return FormatPackOnly(key, runId, commit, marker, reason);
    }

    return FormatSkipDeclined(key, runId, commit, reason);
  }

  /// <summary>Skip line. <paramref name="carryRunId"/> is set when a master skip carries <c>Packages-*</c>.</summary>
  internal static string FormatSkip(
    string key,
    string runId,
    string commit,
    GreenKeyMarker trusted,
    long? carryRunId,
    string? carryArtifact)
  {
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(runId);
    ArgumentNullException.ThrowIfNull(commit);
    ArgumentNullException.ThrowIfNull(trusted);
    string line = FormatId + " skip=yes key=" + One(key)
      + " run=" + One(runId) + " commit=" + One(commit)
      + " trusted_run=" + trusted.RunId.ToString(CultureInfo.InvariantCulture)
      + " trusted_commit=" + One(trusted.Commit);
    if (carryRunId is long carry)
    {
      line += " carry_run=" + carry.ToString(CultureInfo.InvariantCulture);
      if (!string.IsNullOrWhiteSpace(carryArtifact))
      {
        line += " carry_artifact=" + One(carryArtifact);
      }
    }

    return line;
  }

  /// <summary>Pack-only line. <paramref name="carry"/> is why nothing was carried.</summary>
  internal static string FormatPackOnly(string key, string runId, string commit, GreenKeyMarker trusted, string carry)
  {
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(runId);
    ArgumentNullException.ThrowIfNull(commit);
    ArgumentNullException.ThrowIfNull(trusted);
    ArgumentNullException.ThrowIfNull(carry);
    return FormatId + " skip=pack-only key=" + One(key)
      + " run=" + One(runId) + " commit=" + One(commit)
      + " trusted_run=" + trusted.RunId.ToString(CultureInfo.InvariantCulture)
      + " trusted_commit=" + One(trusted.Commit)
      + " carry=" + One(carry);
  }

  /// <summary>A hit that still runs the full pipeline, and why.</summary>
  internal static string FormatSkipDeclined(string key, string runId, string commit, string reason)
  {
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(runId);
    ArgumentNullException.ThrowIfNull(commit);
    ArgumentNullException.ThrowIfNull(reason);
    return FormatId + " skip=no reason=" + One(reason)
      + " key=" + One(key) + " run=" + One(runId) + " commit=" + One(commit);
  }

  /// <summary>
  /// End-of-run pipeline value. <paramref name="stageResult"/> and <paramref name="carryResult"/>
  /// are GitHub Actions results (<c>success</c>, <c>failure</c>, <c>cancelled</c>, <c>skipped</c>).
  /// A cancelled stage wins. A skip whose carry step failed is <see cref="KeySkipFailed"/>.
  /// </summary>
  internal static string PipelineFor(string? runKind, string? stageResult, string? carryResult)
  {
    if (string.Equals(stageResult, Cancelled, StringComparison.Ordinal))
    {
      return Cancelled;
    }

    bool stageOk = string.Equals(stageResult, "success", StringComparison.Ordinal)
      || string.Equals(stageResult, Succeeded, StringComparison.Ordinal);
    if (string.Equals(runKind, RunSkip, StringComparison.Ordinal))
    {
      return string.Equals(carryResult, "failure", StringComparison.Ordinal) ? KeySkipFailed : KeySkipped;
    }

    if (string.Equals(runKind, RunPackOnly, StringComparison.Ordinal))
    {
      return stageOk ? RunPackOnly : PackOnlyFailed;
    }

    return stageOk ? Succeeded : Failed;
  }

  /// <summary>
  /// End-of-run line. <c>false_skip=true</c> means a hit whose full run then failed.
  /// <c>rate=in</c> is a run that spent pipeline minutes.
  /// </summary>
  internal static string FormatPipelineEnd(string earlier, string pipeline, string key, string runId, string commit)
  {
    ArgumentNullException.ThrowIfNull(earlier);
    ArgumentNullException.ThrowIfNull(pipeline);
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(runId);
    ArgumentNullException.ThrowIfNull(commit);
    bool falseSkip = string.Equals(earlier, "hit", StringComparison.Ordinal)
      && string.Equals(pipeline, Failed, StringComparison.Ordinal);
    bool counts = CountsTowardHitRate(pipeline);
    return FormatId + " pipeline=" + One(pipeline)
      + " earlier=" + One(earlier)
      + " false_skip=" + (falseSkip ? "true" : "false")
      + " rate=" + (counts ? "in" : "out")
      + " key=" + One(key)
      + " run=" + One(runId)
      + " commit=" + One(commit);
  }

  /// <summary>
  /// A pipeline value the end line accepts. Anything else is <see cref="Failed"/>,
  /// the value that can mark a false skip.
  /// </summary>
  internal static string NormalizePipeline(string? pipeline) =>
    pipeline is Succeeded or Failed or PathSkipped or Cancelled
      or KeySkipped or KeySkipFailed or RunPackOnly or PackOnlyFailed
      ? pipeline
      : Failed;

  /// <summary>True for <c>succeeded</c> and <c>failed</c>. Those runs have wall minutes.</summary>
  internal static bool CountsTowardHitRate(string pipeline) =>
    string.Equals(pipeline, Succeeded, StringComparison.Ordinal)
    || string.Equals(pipeline, Failed, StringComparison.Ordinal);

  /// <summary>Marker upload result.</summary>
  internal static string FormatMarkerWrite(string status, string key, string runId, string commit, string? detail)
  {
    ArgumentNullException.ThrowIfNull(status);
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(runId);
    ArgumentNullException.ThrowIfNull(commit);
    return FormatId + " marker-write=" + One(status)
      + " key=" + One(key)
      + " run=" + One(runId)
      + " commit=" + One(commit)
      + (string.IsNullOrWhiteSpace(detail) ? "" : " " + One(detail));
  }

  /// <summary>Job summary block. Append to <c>GITHUB_STEP_SUMMARY</c>.</summary>
  internal static string FormatSummary(string observationLine, string? pipelineLine)
  {
    StringBuilder builder = new();
    builder.Append("### Green-key ledger\n\n");
    builder.Append("- `").Append(observationLine).Append("`\n");
    if (!string.IsNullOrWhiteSpace(pipelineLine))
    {
      builder.Append("- `").Append(pipelineLine).Append("`\n");
    }

    builder.Append('\n');
    return builder.ToString();
  }

  /// <summary>Strips bearer tokens from a short error string.</summary>
  internal static string Redact(string? text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return "";
    }

    string shortened = text.Length <= 300 ? text : text[..300];
    return Secret.Replace(shortened, "$1$2<redacted>");
  }

  private static string One(string value) => GreenKeyManifest.OneLine(value);
}
