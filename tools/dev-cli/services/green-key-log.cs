#region Purpose
// Log lines and the job summary for the green-key ledger. Shadow mode logs the phase-2 decision and skips nothing.
#endregion

#region Design
// Every line starts with green-key-v1 and shadow=log-only. A later tally must not read these as skips:
// the skip, once enabled, says mode=skip and skip=yes. false_skip=true is a hit whose full run then failed.
// rate=in is a run that spent pipeline minutes (succeeded or failed). path-skipped is rate=out: task 537
// already removed that run, and it did not test this tree.
#endregion

namespace DevCli;

/// <summary>Green-key log lines for <c>dev workflow</c>.</summary>
internal static class GreenKeyLog
{
  internal const string Succeeded = "succeeded";

  internal const string Failed = "failed";

  internal const string PathSkipped = "path-skipped";

  private const string FormatId = GreenKeyManifest.FormatId;

  private const string Shadow = " shadow=log-only";

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
      return FormatId + Shadow + " outcome=excluded reason=" + One(reason)
        + " key=" + One(key) + " run=" + One(runId) + " commit=" + One(commit);
    }

    if (marker is not null)
    {
      return FormatId + Shadow + " outcome=hit key=" + One(key)
        + " run=" + One(runId) + " commit=" + One(commit)
        + " marker_run=" + marker.RunId.ToString(CultureInfo.InvariantCulture)
        + " marker_commit=" + One(marker.Commit)
        + " marker_artifact=" + marker.ArtifactId.ToString(CultureInfo.InvariantCulture)
        + " writer=" + writer;
    }

    string lookupText = string.IsNullOrWhiteSpace(lookup) ? "ok" : One(lookup);
    return FormatId + Shadow + " outcome=miss key=" + One(key)
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
    return FormatId + Shadow + " outcome=error error=" + One(Redact(error))
      + " run=" + One(runId) + " commit=" + One(commit);
  }

  /// <summary>
  /// Phase-2 decision that shadow mode does not act on.
  /// <paramref name="would"/> is <c>full</c>, <c>skip</c>, or <c>pack-only</c>.
  /// </summary>
  internal static string FormatWould(string would, string reason, string key, string runId, string commit, long? carryRunId)
  {
    ArgumentNullException.ThrowIfNull(would);
    ArgumentNullException.ThrowIfNull(reason);
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(runId);
    ArgumentNullException.ThrowIfNull(commit);
    string line = FormatId + Shadow + " would=" + One(would) + " reason=" + One(reason)
      + " key=" + One(key) + " run=" + One(runId) + " commit=" + One(commit);
    if (carryRunId is long carry)
    {
      line += " carry_run=" + carry.ToString(CultureInfo.InvariantCulture);
    }

    return line;
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
