#region Purpose
// Workflow handler: tag pin, ancestor check, CI run location and artifact download. The attestation
// verify itself is AttestationVerifyRunner in DevCli content (kanban task 592), shared with dev release.
#endregion

namespace DevCli;

public sealed partial class WorkflowCommand
{
  internal sealed partial class Handler
  {
    // Verifies that if tag v{version} already exists locally, HEAD is at that
    // tag's commit. This is what stops a break-glass resume from mixing
    // packages built from two different commits under one version: the tag
    // is created by the release pipeline itself (or by a prior release-event
    // run), so if it exists and HEAD has moved on, this run is not the same
    // source that produced the earlier (partial) push.
    // `git rev-parse -q --verify` suppresses the "not a valid ref" message on
    // a missing tag and just fails silently — nonzero exit with empty stderr
    // means NoTag; nonzero exit with stderr means a real git error.
    private static async Task<TagPinOutcome> CheckTagPinAsync(string version)
    {
      string tag = $"v{version}";

      CommandOutput verifyResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "-q", "--verify", $"refs/tags/{tag}")
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      if (verifyResult.ExitCode != 0)
      {
        if (!string.IsNullOrWhiteSpace(verifyResult.Stderr))
        {
          return new TagPinOutcome(TagPinStatus.GitError, null, null, verifyResult.Stderr.Trim());
        }

        return new TagPinOutcome(TagPinStatus.NoTag, null, null, null);
      }

      CommandOutput tagCommitResult = await Shell.Builder("git")
        .WithArguments("rev-parse", $"{tag}^{{commit}}")
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      if (tagCommitResult.ExitCode != 0)
      {
        return new TagPinOutcome(TagPinStatus.GitError, null, null, tagCommitResult.Stderr.Trim());
      }

      CommandOutput headResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "HEAD")
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      if (headResult.ExitCode != 0)
      {
        return new TagPinOutcome(TagPinStatus.GitError, null, null, headResult.Stderr.Trim());
      }

      string tagCommit = tagCommitResult.Stdout.Trim();
      string headCommit = headResult.Stdout.Trim();

      return string.Equals(tagCommit, headCommit, StringComparison.Ordinal)
        ? new TagPinOutcome(TagPinStatus.Match, tagCommit, headCommit, null)
        : new TagPinOutcome(TagPinStatus.Mismatch, tagCommit, headCommit, null);
    }

    private static string ShortSha(string? sha)
    {
      if (string.IsNullOrEmpty(sha))
        return "(unknown)";

      return sha.Length > 7 ? sha[..7] : sha;
    }

    private static string? ReadPropsVersion(string repoRoot)
    {
      string propsPath = Path.Combine(repoRoot, "source", "Directory.Build.props");

      if (!File.Exists(propsPath))
      {
        return null;
      }

      XDocument doc = XDocument.Load(propsPath);
      return doc.Descendants("Version").FirstOrDefault()?.Value.Trim();
    }

    // git merge-base --is-ancestor exit codes: 0 = ancestor, 1 = NOT ancestor,
    // >1 = git error (e.g. bad ref, corrupt repo) — must not be reported as
    // "not an ancestor", which is a specific, different verdict.
    private async Task<AncestorCheckOutcome> CheckHeadAncestorOfMasterAsync()
    {
      string masterRef = "origin/master";

      CommandOutput verifyResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "--verify", "origin/master")
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      if (verifyResult.ExitCode != 0)
      {
        masterRef = "master";

        CommandOutput fallbackVerifyResult = await Shell.Builder("git")
          .WithArguments("rev-parse", "--verify", "master")
          .WithNoValidation()
          .CaptureAsync(CancellationToken.None);

        if (fallbackVerifyResult.ExitCode != 0)
        {
          return new AncestorCheckOutcome(AncestorCheckStatus.MasterUnresolvable, null);
        }

        Terminal.WriteLine("origin/master not found; using local master.");
      }

      CommandOutput ancestorResult = await Shell.Builder("git")
        .WithArguments("merge-base", "--is-ancestor", "HEAD", masterRef)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      if (ancestorResult.ExitCode == 0)
      {
        return new AncestorCheckOutcome(AncestorCheckStatus.Ancestor, null);
      }

      if (ancestorResult.ExitCode == 1)
      {
        return new AncestorCheckOutcome(AncestorCheckStatus.NotAncestor, null);
      }

      return new AncestorCheckOutcome(AncestorCheckStatus.GitError, ancestorResult.Stderr.Trim());
    }

    // Resolves the commit to release (HEAD) and asks gh for every successful
    // workflow.yml run at that commit, ordered by CiRunPromotion.OrderCandidateRuns
    // (push-event preferred, then newest). `gh run list` exits 0 with an empty
    // JSON array `[]` when nothing matches — that is NoMatchingRun, a distinct
    // outcome from gh itself being unusable. Two DIFFERENT unusable-gh outcomes
    // are distinguished (round-1 review finding #2), matching the
    // TagPinOutcome/AncestorCheckOutcome GitError precedent of surfacing the
    // real detail rather than a single generic message:
    // - GhUnavailable: gh could not even be launched — a missing binary throws
    //   Win32Exception from Process.Start, caught here so the release gate
    //   reports "install gh / gh auth login" instead of a raw exception.
    // - GhFailed: gh launched and ran, but exited nonzero (bad/expired token,
    //   network failure, API rate limit, ...) — its stderr is real diagnostic
    //   information and must not be discarded behind the same "install gh"
    //   message, which would be actively misleading on a runner where gh is
    //   already installed and normally authenticated via GH_TOKEN.
    private static async Task<LocateRunOutcome> LocateCiRunAsync()
    {
      CommandOutput headResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "HEAD")
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      if (headResult.ExitCode != 0)
      {
        throw new InvalidOperationException($"Could not determine HEAD commit: {headResult.Stderr.Trim()}");
      }

      string headSha = headResult.Stdout.Trim();

      CommandOutput runListResult;
      try
      {
        runListResult = await Gh.Run().List()
          .WithWorkflow("workflow.yml")
          .WithCommit(headSha)
          .WithStatus("success")
          .WithJson("databaseId", "event", "headSha", "createdAt")
          .WithNoValidation()
          .CaptureAsync(CancellationToken.None);
      }
      catch (System.ComponentModel.Win32Exception)
      {
        return new LocateRunOutcome(LocateRunStatus.GhUnavailable, headSha, [], null);
      }

      if (runListResult.ExitCode != 0)
      {
        return new LocateRunOutcome(LocateRunStatus.GhFailed, headSha, [], runListResult.Stderr.Trim());
      }

      List<CiRunSummary>? runs = JsonSerializer.Deserialize(runListResult.Stdout, DevCliJsonContext.Default.ListCiRunSummary);
      IReadOnlyList<CiRunSummary> candidateRuns = CiRunPromotion.OrderCandidateRuns(runs ?? [], headSha);

      return candidateRuns.Count == 0
        ? new LocateRunOutcome(LocateRunStatus.NoMatchingRun, headSha, [], null)
        : new LocateRunOutcome(LocateRunStatus.Found, headSha, candidateRuns, null);
    }

    // Walks candidateRuns (already ordered push-first/newest-first) until one
    // has a non-expired Packages-* artifact. A run with NO matching artifact
    // (NoneMatching — pre-458-002 release-event runs won't have one) is skipped
    // silently; a run whose ONLY matches are expired is recorded so the final
    // abort message (if every candidate is exhausted) can tell an operator
    // "these artifacts expired" apart from "CI never uploaded one at all".
    // On the winning run: the artifacts directory is cleared and recreated
    // first — Clean no longer runs in release mode, so a leftover file from a
    // prior local/break-glass attempt must not silently survive into the
    // verified set.
    private async Task<DownloadArtifactOutcome> DownloadPackagesArtifactAsync(string repoRoot, IReadOnlyList<CiRunSummary> candidateRuns)
    {
      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");
      List<ExpiredArtifactEncounter> expiredEncounters = [];

      foreach (CiRunSummary run in candidateRuns)
      {
        CommandOutput artifactsResult = await Gh.Api($"repos/{{owner}}/{{repo}}/actions/runs/{run.DatabaseId}/artifacts")
          .WithNoValidation()
          .CaptureAsync(CancellationToken.None);

        if (artifactsResult.ExitCode != 0)
        {
          throw new InvalidOperationException($"Failed to list artifacts for run {run.DatabaseId}: {artifactsResult.Stderr.Trim()}");
        }

        RunArtifactListResponse? artifactList = JsonSerializer.Deserialize(artifactsResult.Stdout, DevCliJsonContext.Default.RunArtifactListResponse);
        PackagesArtifactOutcome selectOutcome = CiRunPromotion.SelectPackagesArtifact(artifactList?.Artifacts ?? []);

        if (selectOutcome.Status == PackagesArtifactStatus.Expired)
        {
          expiredEncounters.Add(new ExpiredArtifactEncounter(run.DatabaseId, run.Event, selectOutcome.ExpiredNames));
          continue;
        }

        if (selectOutcome.Status == PackagesArtifactStatus.NoneMatching)
        {
          continue;
        }

        RunArtifact artifact = selectOutcome.Artifact!;

        if (Directory.Exists(artifactsDir))
        {
          Directory.Delete(artifactsDir, recursive: true);
        }

        Directory.CreateDirectory(artifactsDir);

        Terminal.WriteLine($"Downloading artifact '{artifact.Name}' from run {run.DatabaseId} ({run.Event})...");

        int exitCode = await Gh.Run().Download(run.DatabaseId.ToString(System.Globalization.CultureInfo.InvariantCulture))
          .WithName(artifact.Name)
          .WithDirectory(artifactsDir)
          .WithWorkingDirectory(repoRoot)
          .RunAsync();

        if (exitCode != 0)
        {
          throw new InvalidOperationException($"Failed to download artifact '{artifact.Name}' from run {run.DatabaseId}!");
        }

        return new DownloadArtifactOutcome(DownloadArtifactStatus.Downloaded, run, artifact.Name, expiredEncounters);
      }

      return new DownloadArtifactOutcome(DownloadArtifactStatus.Exhausted, null, null, expiredEncounters);
    }
  }
}
