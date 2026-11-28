#region Purpose
// Green-key bracket for dev workflow: compute and look up the key, log the phase-2 decision, and stage a marker after a full success.
#endregion

#region Design
// Observe logs the key and writes green_key_run (full, skip, or pack-only) for the workflow.
// GreenKeyShadow.Effective forces full while the shadow flag is on. A marker is staged only after
// a same-repo candidate's full pipeline succeeded. A skip and a pack-only run do not stage one:
// they did not retest. Forks, path-filter skips, and excluded events stage nothing.
// Lookup uses gh api. A run step has no ACTIONS_RUNTIME_TOKEN, so this type does not call the
// artifact service. Observe never throws: a failure is outcome=error, green_key_run=full, and exit stays 0.
#endregion

namespace DevCli;

/// <summary>Wires the green-key ledger into one <c>dev workflow</c> invocation.</summary>
internal static partial class GreenKeyLedger
{
  private const int CarryRunLimit = 8;

  /// <summary>Computes the key, looks it up, and logs what phase 2 would do. Never throws.</summary>
  internal static async Task<GreenKeyObservation> ObserveAsync(
    ITerminal terminal,
    bool releaseMode,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    string runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ?? "local";
    string commit = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "";
    try
    {
      return await ObserveCoreAsync(terminal, releaseMode, runId, commit, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception)
    {
      if (string.IsNullOrWhiteSpace(commit))
      {
        commit = "unknown";
      }

      string line = GreenKeyLog.FormatError(exception.Message, runId, commit);
      string would = GreenKeyLog.FormatWould("full", "error", "", runId, commit, null);
      terminal.WriteLine(line);
      terminal.WriteLine(would);
      AppendSummary(GreenKeyLog.FormatSummary(line, would));
      await WriteOutputsAsync("", writer: false, "error", GreenKeyLog.RunFull, null, cancellationToken)
        .ConfigureAwait(false);
      return new GreenKeyObservation
      {
        Key = "",
        RunId = runId,
        Commit = commit,
        Disposition = GreenKeyDisposition.Excluded,
        Earlier = "error",
        AllowWrite = false,
        ObservationLine = line
      };
    }
  }

  /// <summary>
  /// Logs the end line and stages a marker after a full success.
  /// <paramref name="pipeline"/> is a <see cref="GreenKeyLog"/> pipeline value.
  /// </summary>
  internal static void Finish(ITerminal terminal, GreenKeyObservation observation, string pipeline)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(observation);
    ArgumentNullException.ThrowIfNull(pipeline);
    if (observation.Completed)
    {
      return;
    }

    observation.Completed = true;
    try
    {
      string end = GreenKeyLog.FormatPipelineEnd(
        observation.Earlier,
        pipeline,
        observation.Key,
        observation.RunId,
        observation.Commit);
      terminal.WriteLine(end);
      AppendSummary(GreenKeyLog.FormatSummary(observation.ObservationLine, end));

      bool fullSuccess = string.Equals(pipeline, GreenKeyLog.Succeeded, StringComparison.Ordinal);
      bool write = fullSuccess && observation.AllowWrite && GreenKeyManifest.IsKey(observation.Key);
      if (!write)
      {
        string reason;
        if (fullSuccess)
        {
          reason = "reason=not-a-writer";
        }
        else if (string.Equals(pipeline, GreenKeyLog.Failed, StringComparison.Ordinal))
        {
          reason = "reason=pipeline-failed";
        }
        else
        {
          reason = "reason=" + pipeline;
        }

        terminal.WriteLine(GreenKeyLog.FormatMarkerWrite(
          "skipped",
          observation.Key,
          observation.RunId,
          observation.Commit,
          reason));
        return;
      }

      string? repoRoot = Git.FindRoot();
      if (repoRoot is null)
      {
        terminal.WriteLine(GreenKeyLog.FormatMarkerWrite(
          "failed",
          observation.Key,
          observation.RunId,
          observation.Commit,
          "reason=no-repository-root"));
        return;
      }

      GreenKeyStageResult staged = Stage(repoRoot, observation.Key, observation.RunId, observation.Commit);
      terminal.WriteLine(GreenKeyLog.FormatMarkerWrite(
        staged.Staged ? "staged" : "failed",
        observation.Key,
        observation.RunId,
        observation.Commit,
        staged.Detail));
    }
    catch (Exception exception)
    {
      terminal.WriteErrorLine(
        "green-key-v1 marker-write=failed error=" + GreenKeyLog.Redact(exception.Message));
    }
  }

  /// <summary>
  /// End line for the <c>ci</c> job. Reads the observe job's outputs from the environment
  /// and does not recompute the key: this job's runner may not match the one that hashed it.
  /// </summary>
  internal static void FinishFromEnvironment(ITerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    string key = Environment.GetEnvironmentVariable("GREEN_KEY") ?? "";
    string earlier = Environment.GetEnvironmentVariable("GREEN_KEY_EARLIER") ?? "";
    if (string.IsNullOrWhiteSpace(earlier))
    {
      earlier = "error";
    }

    string? runKind = Environment.GetEnvironmentVariable("GREEN_KEY_RUN");
    string pipeline = string.IsNullOrWhiteSpace(runKind)
      ? GreenKeyLog.NormalizePipeline(Environment.GetEnvironmentVariable("GREEN_KEY_PIPELINE"))
      : GreenKeyLog.PipelineFor(
        runKind,
        Environment.GetEnvironmentVariable("GREEN_KEY_STAGE"),
        Environment.GetEnvironmentVariable("GREEN_KEY_CARRY"));

    bool writer = string.Equals(
      Environment.GetEnvironmentVariable("GREEN_KEY_WRITER"),
      "true",
      StringComparison.OrdinalIgnoreCase);
    Finish(
      terminal,
      new GreenKeyObservation
      {
        Key = key,
        RunId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ?? "local",
        Commit = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "unknown",
        Disposition = writer ? GreenKeyDisposition.Candidate : GreenKeyDisposition.Excluded,
        Earlier = earlier,
        AllowWrite = writer && GreenKeyManifest.IsKey(key),
        ObservationLine = ""
      },
      pipeline);
  }

  private static async Task<GreenKeyObservation> ObserveCoreAsync(
    ITerminal terminal,
    bool releaseMode,
    string runId,
    string commit,
    CancellationToken cancellationToken)
  {
    string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
    string? githubRef = Environment.GetEnvironmentVariable("GITHUB_REF");
    string? repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
    string? eventJson = ReadEventJson();
    string? headRepository = GreenKeyEvent.TryReadHeadRepository(eventJson);
    GreenKeyDisposition disposition = GreenKeyEvent.Classify(
      eventName,
      githubRef,
      repository,
      headRepository,
      releaseMode,
      out string reason);

    string? repoRoot = Git.FindRoot();
    if (string.IsNullOrWhiteSpace(commit) && repoRoot is not null)
    {
      commit = await HeadShaAsync(repoRoot, cancellationToken).ConfigureAwait(false);
    }

    if (string.IsNullOrWhiteSpace(commit))
    {
      commit = "unknown";
    }

    if (repoRoot is null)
    {
      return await LogFailureAsync(terminal, "repository-root", runId, commit, disposition, cancellationToken)
        .ConfigureAwait(false);
    }

    string sdk = await SdkVersionAsync(cancellationToken).ConfigureAwait(false);
    string runner = await RunnerImageAsync(cancellationToken).ConfigureAwait(false);
    List<string> tags = ReadImagePulls(repoRoot);
    IReadOnlyList<string> imageLines = tags.Count == 0
      ? []
      : GreenKeyRunnerImage.ImageLines(tags, await ResolveDigestsAsync(tags, cancellationToken).ConfigureAwait(false));
    CommandOutput listed = await Shell.Builder("git")
      .WithArguments("ls-tree", "-r", "HEAD")
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);
    if (listed.ExitCode != 0)
    {
      return await LogFailureAsync(terminal, "git-ls-tree", runId, commit, disposition, cancellationToken)
        .ConfigureAwait(false);
    }

    GreenKeyComputation computed = GreenKeyTree.FromLsTree(listed.Stdout, sdk, runner, imageLines);
    GreenKeyMarker? marker = null;
    IReadOnlyList<GreenKeyArtifactRow> rows = [];
    string? lookup = null;
    string earlier;
    if (disposition == GreenKeyDisposition.Excluded)
    {
      earlier = "excluded";
    }
    else if (string.IsNullOrWhiteSpace(repository))
    {
      lookup = "unavailable";
      earlier = "miss";
    }
    else
    {
      GreenKeyLookup found = await FindAsync(repository, repoRoot, computed.Key, cancellationToken).ConfigureAwait(false);
      lookup = found.Lookup;
      marker = found.Marker;
      rows = found.Rows;
      earlier = marker is null ? "miss" : "hit";
    }

    (string would, string wouldReason, long? carryRun) = await PredictAsync(
      disposition,
      reason,
      earlier,
      eventName,
      githubRef,
      repository,
      repoRoot,
      eventJson,
      commit,
      rows,
      cancellationToken).ConfigureAwait(false);

    GreenKeyRunKind effective = GreenKeyShadow.Effective(GreenKeyLog.RunKindFromWould(would));
    string? acted = GreenKeyShadow.Enabled
      ? null
      : GreenKeyLog.FormatActed(effective, wouldReason, computed.Key, runId, commit, marker, carryRun, earlier);
    string decision = acted ?? GreenKeyLog.FormatWould(would, wouldReason, computed.Key, runId, commit, carryRun);
    string line = GreenKeyLog.FormatObservation(disposition, reason, computed.Key, runId, commit, lookup, marker);
    terminal.WriteLine(line);
    terminal.WriteLine(GreenKeyLog.FormatDetail(sdk, runner, imageLines.Count, computed.TreeCount));
    terminal.WriteLine(decision);
    AppendSummary(GreenKeyLog.FormatSummary(line, decision));

    bool allowWrite = disposition == GreenKeyDisposition.Candidate && GreenKeyManifest.IsKey(computed.Key);
    long? carryOutput = effective == GreenKeyRunKind.Skip ? carryRun : null;
    await WriteOutputsAsync(
      computed.Key,
      allowWrite,
      earlier,
      GreenKeyLog.RunKindName(effective),
      carryOutput,
      cancellationToken).ConfigureAwait(false);
    return new GreenKeyObservation
    {
      Key = computed.Key,
      RunId = runId,
      Commit = commit,
      Disposition = disposition,
      Earlier = earlier,
      AllowWrite = allowWrite,
      ObservationLine = line
    };
  }

  private static async Task<GreenKeyObservation> LogFailureAsync(
    ITerminal terminal,
    string error,
    string runId,
    string commit,
    GreenKeyDisposition disposition,
    CancellationToken cancellationToken)
  {
    string line = GreenKeyLog.FormatError(error, runId, commit);
    string would = GreenKeyLog.FormatWould("full", "error", "", runId, commit, null);
    terminal.WriteLine(line);
    terminal.WriteLine(would);
    AppendSummary(GreenKeyLog.FormatSummary(line, would));
    await WriteOutputsAsync("", writer: false, "error", GreenKeyLog.RunFull, null, cancellationToken)
      .ConfigureAwait(false);
    return new GreenKeyObservation
    {
      Key = "",
      RunId = runId,
      Commit = commit,
      Disposition = disposition,
      Earlier = "error",
      AllowWrite = false,
      ObservationLine = line
    };
  }

  private static async Task<(string Would, string Reason, long? CarryRun)> PredictAsync(
    GreenKeyDisposition disposition,
    string reason,
    string earlier,
    string? eventName,
    string? githubRef,
    string? repository,
    string repoRoot,
    string? eventJson,
    string commit,
    IReadOnlyList<GreenKeyArtifactRow> rows,
    CancellationToken cancellationToken)
  {
    if (disposition != GreenKeyDisposition.Candidate)
    {
      return ("full", reason, null);
    }

    if (!string.Equals(earlier, "hit", StringComparison.Ordinal))
    {
      string missReason = string.Equals(earlier, "error", StringComparison.Ordinal) ? "error" : "miss";
      return ("full", missReason, null);
    }

    ForceRead force = await ReadForceAsync(eventName, eventJson, repoRoot, commit, cancellationToken).ConfigureAwait(false);
    if (force == ForceRead.Unreadable)
    {
      return ("full", "trailer-unreadable", null);
    }

    if (force == ForceRead.FullRun)
    {
      return ("full", "force-trailer", null);
    }

    if (!GreenKeySkip.IsMasterPush(eventName, githubRef))
    {
      return ("skip", "hit", null);
    }

    return await PredictMasterCarryAsync(repository, repoRoot, rows, cancellationToken).ConfigureAwait(false);
  }

  private static async Task<(string Would, string Reason, long? CarryRun)> PredictMasterCarryAsync(
    string? repository,
    string repoRoot,
    IReadOnlyList<GreenKeyArtifactRow> rows,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(repository))
    {
      return ("full", "carry-unknown", null);
    }

    IReadOnlyList<long> runs = GreenKeySkip.CarryCandidateRuns(rows);
    if (runs.Count == 0)
    {
      return ("full", "carry-unknown", null);
    }

    int probed = 0;
    foreach (long runId in runs)
    {
      if (probed >= CarryRunLimit)
      {
        break;
      }

      probed++;
      PackageProbe probe = await ProbePackagesAsync(repository, repoRoot, runId, cancellationToken).ConfigureAwait(false);
      if (probe == PackageProbe.Unknown)
      {
        return ("full", "carry-unknown", null);
      }

      if (probe == PackageProbe.Live)
      {
        return ("skip", "carried", runId);
      }
    }

    return ("pack-only", "no-packages-to-carry", null);
  }

  private static async Task<ForceRead> ReadForceAsync(
    string? eventName,
    string? eventJson,
    string repoRoot,
    string commit,
    CancellationToken cancellationToken)
  {
    string head;
    if (string.Equals(eventName, "pull_request", StringComparison.Ordinal))
    {
      string? pullHead = GreenKeyEvent.TryReadPullRequestHeadSha(eventJson);
      if (string.IsNullOrWhiteSpace(pullHead))
      {
        return ForceRead.Unreadable;
      }

      head = pullHead;
    }
    else
    {
      if (string.IsNullOrWhiteSpace(commit) || string.Equals(commit, "unknown", StringComparison.Ordinal))
      {
        return ForceRead.Unreadable;
      }

      head = commit;
    }

    CommandOutput logged;
    try
    {
      logged = await Shell.Builder("git")
        .WithArguments("log", "-1", "--format=%(trailers:key=Green-Key,valueonly)", head)
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
    }
    catch (System.ComponentModel.Win32Exception)
    {
      return ForceRead.Unreadable;
    }

    if (logged.ExitCode != 0)
    {
      return ForceRead.Unreadable;
    }

    return GreenKeySkip.IsForceFullRun(logged.Stdout) ? ForceRead.FullRun : ForceRead.NotSet;
  }
}

/// <summary>One workflow invocation's ledger state. Finish is idempotent.</summary>
internal sealed class GreenKeyObservation
{
  internal bool Completed { get; set; }

  internal string Key { get; init; } = "";

  internal string RunId { get; init; } = "";

  internal string Commit { get; init; } = "";

  internal GreenKeyDisposition Disposition { get; init; }

  internal string Earlier { get; init; } = "error";

  internal bool AllowWrite { get; init; }

  internal string ObservationLine { get; init; } = "";
}

/// <summary>Marker lookup. <c>Lookup</c> is <c>ok</c>, <c>failed</c>, or <c>unavailable</c>.</summary>
internal readonly record struct GreenKeyLookup(
  bool StoreAvailable,
  GreenKeyMarker? Marker,
  string Lookup,
  IReadOnlyList<GreenKeyArtifactRow> Rows);

/// <summary>Marker stage. <c>Detail</c> is a short status, never a URL or token.</summary>
internal readonly record struct GreenKeyStageResult(bool Staged, string? Detail);
