#region Purpose
// Workflow handler: the release-mode pipeline (tag gate through NuGet push).
#endregion

namespace DevCli;

public sealed partial class WorkflowCommand
{
  internal sealed partial class Handler
  {
    private async Task RunReleaseWorkflowAsync(string? apiKey, string? cliAttestationMode, string mode)
    {
      WorkflowStageSpan.TagPipeline(Activity.Current);

      Terminal.WriteLine("Pipeline: tag-gate -> check-version -> locate-run -> download-artifact -> verify -> push");
      Terminal.WriteLine("");

      // Get repo root for pack/push operations
      string repoRoot = ResolveRepoRoot();
      string? propsVersion = null;
      IReadOnlyList<PackableProject> packableProjects = [];
      LocateRunOutcome? locateOutcome = null;

      // Step 1: Release Gate — Tag Assertions
      bool tagGateOk = await InStageAsync(mode, "tag-gate", async () =>
      {
        Terminal.WriteLine("===============================================================================");
        Terminal.WriteLine("  Step 1/6: Release Gate — Tag Assertions");
        Terminal.WriteLine("===============================================================================");

        string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
        propsVersion = ReadPropsVersion(repoRoot);

        if (eventName == "release")
        {
          string? refName = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
          TagAssertionResult tagResult = TagAssertion.Validate(refName, propsVersion);

          if (!tagResult.IsValid)
          {
            Terminal.WriteErrorLine($"Release gate failed: {tagResult.Error}");
            AbortPipeline("release tag does not match source version");
            return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);
          }

          Terminal.WriteLine($"Tag assertion passed: {tagResult.ExpectedTag}");
        }
        else
        {
          Terminal.WriteLine("Tag assertion skipped: GITHUB_EVENT_NAME is not 'release' (break-glass/local release has no triggering ref tag to assert against; the tag-pin check below still applies).");
        }

        if (string.IsNullOrWhiteSpace(propsVersion))
        {
          Terminal.WriteLine("Tag pin skipped: could not read <Version> from source/Directory.Build.props (Step 2/6 Check Version will fail with details).");
        }
        else
        {
          TagPinOutcome tagPinOutcome = await CheckTagPinAsync(propsVersion);
          string pinTag = $"v{propsVersion}";

          switch (tagPinOutcome.Status)
          {
            case TagPinStatus.NoTag:
              Terminal.WriteLine($"Tag pin: {pinTag} not yet tagged.");
              break;

            case TagPinStatus.Match:
              Terminal.WriteLine($"Tag pin passed: HEAD is at {pinTag}.");
              break;

            case TagPinStatus.Mismatch:
              Terminal.WriteErrorLine($"Release gate failed: tag {pinTag} already exists at commit {ShortSha(tagPinOutcome.TagCommit)}; this run is at {ShortSha(tagPinOutcome.HeadCommit)}. A partial-publish resume must run from the tag's commit (or bump the version if source changed).");
              AbortPipeline("tag pin mismatch");
              return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);

            case TagPinStatus.GitError:
              Terminal.WriteErrorLine($"Release gate failed: tag pin check could not run — {tagPinOutcome.Detail}");
              AbortPipeline("tag pin check could not run");
              return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);

            default:
              Terminal.WriteErrorLine($"Release gate failed: unhandled tag pin status '{tagPinOutcome.Status}'.");
              AbortPipeline("unhandled tag pin status");
              return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);
          }
        }

        AncestorCheckOutcome ancestorOutcome = await CheckHeadAncestorOfMasterAsync();

        switch (ancestorOutcome.Status)
        {
          case AncestorCheckStatus.NotAncestor:
            Terminal.WriteErrorLine("Release gate failed: current commit is not an ancestor of master. Releases must be cut from commits on master.");
            AbortPipeline("commit not on master");
            return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);

          case AncestorCheckStatus.MasterUnresolvable:
            Terminal.WriteErrorLine("Release gate failed: cannot resolve origin/master or master — ensure the checkout has full history (fetch-depth: 0) and a master ref exists.");
            AbortPipeline("master ref unresolvable");
            return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);

          case AncestorCheckStatus.GitError:
            Terminal.WriteErrorLine($"Release gate failed: ancestor check could not run — {ancestorOutcome.Detail}");
            AbortPipeline("ancestor check could not run");
            return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);

          case AncestorCheckStatus.Ancestor:
            break;

          default:
            Terminal.WriteErrorLine($"Release gate failed: unhandled ancestor check status '{ancestorOutcome.Status}'.");
            AbortPipeline("unhandled ancestor check status");
            return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);
        }

        // Attestation gate — honors attestation.mode (and --attestation CLI
        // override). Unset default is Require (TimeWarp-first: preserves the
        // pre-458-011 hard gate when the key is absent). Explicit off/warn/
        // require follow the shared policy helper; typos fail closed to Require.
        AttestationStepResult attestationResult = await RunAttestationPolicyStepAsync(
          repoRoot,
          cliAttestationMode,
          AttestationConfigResolver.DefaultReleaseMode).ConfigureAwait(false);

        if (attestationResult.ShouldAbort)
        {
          AbortPipeline("attestation required (mode=require) and not valid");
          return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);
        }

        return WorkflowStageSpan.Result.Done;
      }).ConfigureAwait(false);
      if (!tagGateOk)
      {
        return;
      }

      // Step 2: Check Version. An empty packable set aborts inside this stage.
      bool checkVersionOk = await InStageAsync(mode, "check-version", async () =>
      {
        Terminal.WriteLine("");
        Terminal.WriteLine("===============================================================================");
        Terminal.WriteLine("  Step 2/6: Check Version");
        Terminal.WriteLine("===============================================================================");
        CheckVersionCommand.Handler checkVersionHandler = new(Terminal, NuGetVersionService, ConfigService, PackableProjectService);
        await checkVersionHandler.Handle(new CheckVersionCommand(), CancellationToken.None);

        if (Environment.ExitCode != 0)
        {
          // 536: name the real cause. A job token without packages: read makes
          // check-version refuse, and that is not "version already released".
          AbortPipeline(CheckVersionAbort.FormatAbortReason(checkVersionHandler.Outcome, checkVersionHandler.GitHubPackagesFailure));
          return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);
        }

        // Derive the packable set once, right after check-version and before
        // Clean/Build — an empty derived set must abort before those steps
        // waste time, not at Step 5 (round-1 review finding #3). The same list
        // is threaded through to both Pack and Push below, and printed here so
        // release logs always show exactly what will ship (finding #1c).
        packableProjects = await PackableProjectService
          .GetPackableProjectsAsync(repoRoot, CancellationToken.None)
          .ConfigureAwait(false);

        if (packableProjects.Count == 0)
        {
          Terminal.WriteErrorLine("Release gate failed: no packable projects found under source/.");
          AbortPipeline("no packable projects found");
          return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);
        }

        Terminal.WriteLine($"Packable set ({packableProjects.Count}): {string.Join(", ", packableProjects.Select(p => p.PackageId))}");
        return WorkflowStageSpan.Result.Done;
      }).ConfigureAwait(false);
      if (!checkVersionOk)
      {
        return;
      }

      // Step 3: Locate CI Run
      bool locateOk = await InStageAsync(mode, "locate-run", async () =>
      {
        Terminal.WriteLine("===============================================================================");
        Terminal.WriteLine("  Step 3/6: Locate CI Run");
        Terminal.WriteLine("===============================================================================");

        locateOutcome = await LocateCiRunAsync();

        switch (locateOutcome.Status)
        {
          case LocateRunStatus.GhUnavailable:
            Terminal.WriteErrorLine("Release gate failed: release mode promotes CI-built artifacts and requires the gh CLI. On runners GH_TOKEN is provided by workflow.yml; locally install gh and run 'gh auth login'.");
            AbortPipeline("gh CLI unavailable");
            return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);

          case LocateRunStatus.GhFailed:
            Terminal.WriteErrorLine($"Release gate failed: gh run list failed — {locateOutcome.Detail}. If this is transient (network/rate limit), retry; for auth issues run 'gh auth login'.");
            AbortPipeline("gh run list failed");
            return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);

          case LocateRunStatus.NoMatchingRun:
            Terminal.WriteErrorLine($"Release gate failed: no successful CI run of workflow.yml exists for commit {locateOutcome.HeadSha}. Only tested CI artifacts are published — this commit must pass CI first. If a run failed, fix and re-run it (gh run rerun <run-id>).");
            AbortPipeline("no successful CI run found");
            return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);

          case LocateRunStatus.Found:
            break;

          default:
            Terminal.WriteErrorLine($"Release gate failed: unhandled locate-run status '{locateOutcome.Status}'.");
            AbortPipeline("unhandled locate-run status");
            return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);
        }

        Terminal.WriteLine($"Found {locateOutcome.CandidateRuns.Count} candidate CI run(s) for commit {ShortSha(locateOutcome.HeadSha)}.");
        return WorkflowStageSpan.Result.Done;
      }).ConfigureAwait(false);
      if (!locateOk)
      {
        return;
      }

      // Step 4: Download Artifact
      bool downloadOk = await InStageAsync(mode, "download-artifact", async () =>
      {
        Terminal.WriteLine("");
        Terminal.WriteLine("===============================================================================");
        Terminal.WriteLine("  Step 4/6: Download Artifact");
        Terminal.WriteLine("===============================================================================");

        DownloadArtifactOutcome downloadOutcome = await DownloadPackagesArtifactAsync(repoRoot, locateOutcome!.CandidateRuns);

        if (downloadOutcome.Status == DownloadArtifactStatus.Exhausted)
        {
          if (downloadOutcome.ExpiredEncounters.Count > 0)
          {
            string expiredDetail = string.Join("; ", downloadOutcome.ExpiredEncounters.Select(e => $"run {e.RunId} ({e.Event}): {string.Join(", ", e.ArtifactNames)}"));
            Terminal.WriteErrorLine($"Release gate failed: every candidate CI run's Packages-* artifact has expired — {expiredDetail}. Re-run CI to produce a fresh tested artifact (gh run rerun {downloadOutcome.ExpiredEncounters[0].RunId}).");
          }
          else
          {
            Terminal.WriteErrorLine($"Release gate failed: no candidate CI run for commit {locateOutcome.HeadSha} uploaded a Packages-* artifact. Re-run CI to produce one (gh run rerun {locateOutcome.CandidateRuns[0].DatabaseId}).");
          }

          AbortPipeline("no usable CI artifact found");
          return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);
        }

        Terminal.WriteLine($"Downloaded '{downloadOutcome.ArtifactName}' from run {downloadOutcome.Run!.DatabaseId} ({downloadOutcome.Run.Event}).");
        return WorkflowStageSpan.Result.Done;
      }).ConfigureAwait(false);
      if (!downloadOk)
      {
        return;
      }

      // Step 5: Verify Package Set
      bool verifyOk = await InStageAsync(mode, "verify", async () =>
      {
        Terminal.WriteLine("");
        Terminal.WriteLine("===============================================================================");
        Terminal.WriteLine("  Step 5/6: Verify Package Set");
        Terminal.WriteLine("===============================================================================");

        string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");
        string[] actualNupkgPaths = Directory.Exists(artifactsDir) ? Directory.GetFiles(artifactsDir, "*.nupkg") : [];
        IReadOnlyList<string> actualFileNames = [.. actualNupkgPaths.Select(Path.GetFileName)!];

        // propsVersion is guaranteed non-null here: Step 2/6 (Check Version) fails
        // the pipeline above whenever it cannot read <Version> from source/Directory.Build.props.
        PackageSetVerification verification = CiRunPromotion.VerifyPackageSet(actualFileNames, packableProjects, propsVersion!);

        if (!verification.IsMatch)
        {
          if (verification.Missing.Count > 0)
          {
            Terminal.WriteErrorLine($"Release gate failed: downloaded artifact is missing package(s): {string.Join(", ", verification.Missing)}.");
          }

          if (verification.Unexpected.Count > 0)
          {
            Terminal.WriteErrorLine($"Release gate failed: downloaded artifact has unexpected package(s): {string.Join(", ", verification.Unexpected)}.");
          }

          Terminal.WriteErrorLine($"CI run likely predates the version bump — re-run CI on commit {locateOutcome!.HeadSha} and retry.");
          AbortPipeline("downloaded package set does not match derived packable set");
          return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);
        }

        Terminal.WriteLine("Package set verified.");
        return WorkflowStageSpan.Result.Done;
      }).ConfigureAwait(false);
      if (!verifyOk)
      {
        return;
      }

      // Step 6: Push
      bool pushOk = await InStageAsync(mode, "push", async () =>
      {
        Terminal.WriteLine("");
        Terminal.WriteLine("===============================================================================");
        Terminal.WriteLine("  Step 6/6: Push to NuGet");
        Terminal.WriteLine("===============================================================================");
        await PushPackagesAsync(repoRoot, packableProjects, apiKey);
        return WorkflowStageSpan.Result.Done;
      }).ConfigureAwait(false);
      if (!pushOk)
      {
        return;
      }

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Pipeline SUCCEEDED - Packages published to NuGet.org");
      Terminal.WriteLine("===============================================================================");
    }
  }
}
