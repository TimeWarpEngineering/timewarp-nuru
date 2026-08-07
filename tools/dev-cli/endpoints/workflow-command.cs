#region Purpose
// Dev CLI command for TimeWarp.Nuru development workflow
#endregion

// ═══════════════════════════════════════════════════════════════════════════════
// WORKFLOW COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Orchestrates the full CI/CD pipeline with mode detection.
// Auto-detects mode from GITHUB_EVENT_NAME or accepts explicit --mode flag.
//
// Modes:
//   pr/merge:  clean -> build -> verify-samples -> test
//   release:   tag-gate -> check-version -> locate-run -> download-artifact -> verify -> push
//              (no rebuild — promotes the exact .nupkg set that master-merge CI
//              already built+tested+uploaded for this commit; kanban task 458-002)
//
// Event mapping: pull_request -> pr, push -> merge, release -> release,
// workflow_dispatch -> merge (manual dispatch never publishes by default;
// break-glass release requires explicit --mode release, wired in workflow.yml
// behind a confirm input).

namespace DevCli;

/// <summary>
/// Run the full CI/CD pipeline.
/// </summary>
[NuruRoute("workflow", Description = "Run full CI/CD pipeline")]
internal sealed class WorkflowCommand : ICommand<Unit>
{
  [Option("mode", "m", Description = "CI mode: pr, merge, or release (auto-detected from GITHUB_EVENT_NAME if not specified)")]
  public string? Mode { get; set; }

  [Option("api-key", Description = "NuGet API key for publishing (from OIDC Trusted Publishing)")]
  public string? ApiKey { get; set; }

  internal sealed class Handler : ICommandHandler<WorkflowCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly IRepoCleanService RepoCleanService;
    private readonly NuGetVersionService NuGetVersionService;
    private readonly IRepoConfigService ConfigService;
    private readonly IPackableProjectService PackableProjectService;

    public Handler
    (
      ITerminal terminal,
      IRepoCleanService repoCleanService,
      NuGetVersionService nuGetVersionService,
      IRepoConfigService configService,
      IPackableProjectService packableProjectService
    )
    {
      Terminal = terminal;
      RepoCleanService = repoCleanService;
      NuGetVersionService = nuGetVersionService;
      ConfigService = configService;
      PackableProjectService = packableProjectService;
    }

    public async ValueTask<Unit> Handle(WorkflowCommand command, CancellationToken ct)
    {
      // Determine CI mode
      CiMode mode = DetermineMode(command.Mode);

      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine($"  CI/CD Pipeline - Mode: {mode}");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("");

      if (mode == CiMode.Release)
      {
        await RunReleaseWorkflowAsync(command.ApiKey);
      }
      else
      {
        await RunPrWorkflowAsync();
      }

      return Unit.Value;
    }

    private CiMode DetermineMode(string? explicitMode)
    {
      string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
      CiMode mode = CiModeDetector.DetermineMode(explicitMode, eventName);

      if (string.IsNullOrEmpty(explicitMode))
      {
        string displayEventName = eventName ?? "(not set)";
        Terminal.WriteLine($"Detected GITHUB_EVENT_NAME: {displayEventName} -> Mode: {mode}");
      }

      return mode;
    }

    private async Task RunPrWorkflowAsync()
    {
      Terminal.WriteLine("Pipeline: attestation -> clean -> build -> verify-samples -> test");
      Terminal.WriteLine("");

      string repoRoot = ResolveRepoRoot();

      // Step 1: Attestation
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 1/5: Attestation");
      Terminal.WriteLine("===============================================================================");
      AttestationStepResult attestationResult = await RunPrAttestationStepAsync(repoRoot).ConfigureAwait(false);

      if (attestationResult.ShouldAbort)
      {
        AbortPipeline("attestation required (mode=require) and not valid");
        return;
      }

      // Step 2: Clean
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 2/5: Clean");
      Terminal.WriteLine("===============================================================================");
      CleanCommand.Handler cleanHandler = new(Terminal, RepoCleanService);
      await cleanHandler.Handle(new CleanCommand(), CancellationToken.None);

      // Step 3: Build
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 3/5: Build");
      Terminal.WriteLine("===============================================================================");
      BuildCommand.Handler buildHandler = new(Terminal);
      await buildHandler.Handle(new BuildCommand(), CancellationToken.None);

      // Step 4: Verify Samples
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 4/5: Verify Samples");
      Terminal.WriteLine("===============================================================================");
      VerifySamplesCommand.Handler verifySamplesHandler = new(Terminal);
      await verifySamplesHandler.Handle(new VerifySamplesCommand(), CancellationToken.None);

      // Step 5: Test
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 5/5: Test");
      Terminal.WriteLine("===============================================================================");
      TestCommand.Handler testHandler = new(Terminal);
      await testHandler.Handle(new TestCommand(), CancellationToken.None);

      // Attestation advisory (warn mode only) is repeated here, immediately
      // before the SUCCEEDED banner, so it is not lost above scrollback from
      // clean/build/verify-samples/test output (round-1 self-review finding).
      if (attestationResult.RepeatAdvisory is not null)
      {
        Terminal.WriteLine("");
        Terminal.WriteLine(attestationResult.RepeatAdvisory.Yellow());
      }

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Pipeline SUCCEEDED");
      Terminal.WriteLine("===============================================================================");
    }

    // Runs the attestation verify step for PR/merge mode and reports the
    // outcome per .timewarp/dev.jsonc `attestation.mode` (default "warn" —
    // nothing is enforced org-wide until repos opt in; see
    // attestation-config.cs Design region). "require" fails the pipeline on
    // any non-Valid outcome; "warn" prints a loud advisory but never aborts.
    private async Task<AttestationStepResult> RunPrAttestationStepAsync(string repoRoot)
    {
      RepoConfig config = await ConfigService.GetConfigAsync(CancellationToken.None).ConfigureAwait(false);
      bool requireMode = string.Equals(config.Attestation?.Mode, "require", StringComparison.OrdinalIgnoreCase);

      AttestationVerifyOutcome outcome = await VerifyAttestationAsync(repoRoot).ConfigureAwait(false);

      if (outcome.Status == AttestationVerificationStatus.Valid)
      {
        Terminal.WriteLine($"Attestation valid: check_set {ShortSha(outcome.CheckSet)} ts {outcome.Ts}");
        return new AttestationStepResult(ShouldAbort: false, RepeatAdvisory: null);
      }

      string message = DescribeAttestationOutcome(outcome);

      if (requireMode)
      {
        Terminal.WriteErrorLine($"Attestation required (mode=require): {message}");
        return new AttestationStepResult(ShouldAbort: true, RepeatAdvisory: null);
      }

      string advisory = $"Attestation advisory (mode=warn, not enforced): {message}";
      Terminal.WriteLine("");
      Terminal.WriteLine("*******************************************************************************".Yellow());
      Terminal.WriteLine($"  {advisory}".Yellow());
      Terminal.WriteLine("  Set attestation.mode = \"require\" in .timewarp/dev.jsonc once org rollout".Yellow());
      Terminal.WriteLine("  completes to enforce this in CI (kanban task 458-010).".Yellow());
      Terminal.WriteLine("*******************************************************************************".Yellow());

      return new AttestationStepResult(ShouldAbort: false, RepeatAdvisory: advisory);
    }

    private async Task RunReleaseWorkflowAsync(string? apiKey)
    {
      Terminal.WriteLine("Pipeline: tag-gate -> check-version -> locate-run -> download-artifact -> verify -> push");
      Terminal.WriteLine("");

      // Get repo root for pack/push operations
      string repoRoot = ResolveRepoRoot();

      // Step 1: Release Gate — Tag Assertions
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 1/6: Release Gate — Tag Assertions");
      Terminal.WriteLine("===============================================================================");

      string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
      string? propsVersion = ReadPropsVersion(repoRoot);

      if (eventName == "release")
      {
        string? refName = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
        TagAssertionResult tagResult = TagAssertion.Validate(refName, propsVersion);

        if (!tagResult.IsValid)
        {
          Terminal.WriteErrorLine($"Release gate failed: {tagResult.Error}");
          AbortPipeline("release tag does not match source version");
          return;
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
            return;

          case TagPinStatus.GitError:
            Terminal.WriteErrorLine($"Release gate failed: tag pin check could not run — {tagPinOutcome.Detail}");
            AbortPipeline("tag pin check could not run");
            return;

          default:
            Terminal.WriteErrorLine($"Release gate failed: unhandled tag pin status '{tagPinOutcome.Status}'.");
            AbortPipeline("unhandled tag pin status");
            return;
        }
      }

      AncestorCheckOutcome ancestorOutcome = await CheckHeadAncestorOfMasterAsync();

      switch (ancestorOutcome.Status)
      {
        case AncestorCheckStatus.NotAncestor:
          Terminal.WriteErrorLine("Release gate failed: current commit is not an ancestor of master. Releases must be cut from commits on master.");
          AbortPipeline("commit not on master");
          return;

        case AncestorCheckStatus.MasterUnresolvable:
          Terminal.WriteErrorLine("Release gate failed: cannot resolve origin/master or master — ensure the checkout has full history (fetch-depth: 0) and a master ref exists.");
          AbortPipeline("master ref unresolvable");
          return;

        case AncestorCheckStatus.GitError:
          Terminal.WriteErrorLine($"Release gate failed: ancestor check could not run — {ancestorOutcome.Detail}");
          AbortPipeline("ancestor check could not run");
          return;

        case AncestorCheckStatus.Ancestor:
          break;

        default:
          Terminal.WriteErrorLine($"Release gate failed: unhandled ancestor check status '{ancestorOutcome.Status}'.");
          AbortPipeline("unhandled ancestor check status");
          return;
      }

      // Attestation gate — ALWAYS enforced in release mode, regardless of
      // .timewarp/dev.jsonc `attestation.mode` (that config only governs
      // PR/merge mode's advisory-vs-required behavior; a release with no
      // verifiable audit evidence must never ship — kanban task 458-010).
      // The runner never signs; a missing/invalid attestation fails with
      // guidance to pull master locally so ganda can attest.
      AttestationVerifyOutcome attestationOutcome = await VerifyAttestationAsync(repoRoot).ConfigureAwait(false);

      if (attestationOutcome.Status != AttestationVerificationStatus.Valid)
      {
        Terminal.WriteErrorLine($"Release gate failed: {DescribeAttestationOutcome(attestationOutcome)}");
        AbortPipeline("attestation missing or invalid");
        return;
      }

      Terminal.WriteLine($"Attestation valid: check_set {ShortSha(attestationOutcome.CheckSet)} ts {attestationOutcome.Ts}");

      // Step 2: Check Version
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 2/6: Check Version");
      Terminal.WriteLine("===============================================================================");
      CheckVersionCommand.Handler checkVersionHandler = new(Terminal, NuGetVersionService, ConfigService, PackableProjectService);
      await checkVersionHandler.Handle(new CheckVersionCommand(), CancellationToken.None);

      if (Environment.ExitCode != 0)
      {
        AbortPipeline("version already released");
        return;
      }

      // Derive the packable set once, right after check-version and before
      // Clean/Build — an empty derived set must abort before those steps
      // waste time, not at Step 5 (round-1 review finding #3). The same list
      // is threaded through to both Pack and Push below, and printed here so
      // release logs always show exactly what will ship (finding #1c).
      IReadOnlyList<PackableProject> packableProjects = await PackableProjectService
        .GetPackableProjectsAsync(repoRoot, CancellationToken.None)
        .ConfigureAwait(false);

      if (packableProjects.Count == 0)
      {
        Terminal.WriteErrorLine("Release gate failed: no packable projects found under source/.");
        AbortPipeline("no packable projects found");
        return;
      }

      Terminal.WriteLine($"Packable set ({packableProjects.Count}): {string.Join(", ", packableProjects.Select(p => p.PackageId))}");

      // Step 3: Locate CI Run
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 3/6: Locate CI Run");
      Terminal.WriteLine("===============================================================================");

      LocateRunOutcome locateOutcome = await LocateCiRunAsync();

      switch (locateOutcome.Status)
      {
        case LocateRunStatus.GhUnavailable:
          Terminal.WriteErrorLine("Release gate failed: release mode promotes CI-built artifacts and requires the gh CLI. On runners GH_TOKEN is provided by workflow.yml; locally install gh and run 'gh auth login'.");
          AbortPipeline("gh CLI unavailable");
          return;

        case LocateRunStatus.GhFailed:
          Terminal.WriteErrorLine($"Release gate failed: gh run list failed — {locateOutcome.Detail}. If this is transient (network/rate limit), retry; for auth issues run 'gh auth login'.");
          AbortPipeline("gh run list failed");
          return;

        case LocateRunStatus.NoMatchingRun:
          Terminal.WriteErrorLine($"Release gate failed: no successful CI run of workflow.yml exists for commit {locateOutcome.HeadSha}. Only tested CI artifacts are published — this commit must pass CI first. If a run failed, fix and re-run it (gh run rerun <run-id>).");
          AbortPipeline("no successful CI run found");
          return;

        case LocateRunStatus.Found:
          break;

        default:
          Terminal.WriteErrorLine($"Release gate failed: unhandled locate-run status '{locateOutcome.Status}'.");
          AbortPipeline("unhandled locate-run status");
          return;
      }

      Terminal.WriteLine($"Found {locateOutcome.CandidateRuns.Count} candidate CI run(s) for commit {ShortSha(locateOutcome.HeadSha)}.");

      // Step 4: Download Artifact
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 4/6: Download Artifact");
      Terminal.WriteLine("===============================================================================");

      DownloadArtifactOutcome downloadOutcome = await DownloadPackagesArtifactAsync(repoRoot, locateOutcome.CandidateRuns);

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
        return;
      }

      Terminal.WriteLine($"Downloaded '{downloadOutcome.ArtifactName}' from run {downloadOutcome.Run!.DatabaseId} ({downloadOutcome.Run.Event}).");

      // Step 5: Verify Package Set
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

        Terminal.WriteErrorLine($"CI run likely predates the version bump — re-run CI on commit {locateOutcome.HeadSha} and retry.");
        AbortPipeline("downloaded package set does not match derived packable set");
        return;
      }

      Terminal.WriteLine("Package set verified.");

      // Step 6: Push
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 6/6: Push to NuGet");
      Terminal.WriteLine("===============================================================================");
      await PushPackagesAsync(repoRoot, packableProjects, apiKey);

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Pipeline SUCCEEDED - Packages published to NuGet.org");
      Terminal.WriteLine("===============================================================================");
    }

    private void AbortPipeline(string reason)
    {
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine($"  Pipeline ABORTED — {reason}");
      Terminal.WriteLine("===============================================================================");
      Environment.ExitCode = 1;
    }

    // Repo root heuristic shared by both pipeline modes: prefer the
    // AOT-published binary's on-disk layout (bin/<rid>/ -> repo root is four
    // levels up); fall back to CWD when that guess misses (e.g. running via
    // `dotnet run tools/dev-cli/dev.cs` instead of the published binary).
    private static string ResolveRepoRoot()
    {
      string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
      if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
      {
        repoRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
      }

      return repoRoot;
    }

    // Orchestrates the ganda-audit attestation verify step (kanban task
    // 458-010): fetch notes (best-effort), resolve HEAD's tree, read the
    // note, run the pure AttestationVerifier.Evaluate, and — only when it
    // says ReadyToVerify — shell out to `openssl pkeyutl -verify -rawin`
    // (no pure-.NET Ed25519 verify exists in the BCL; see
    // attestation-verifier.cs's Design region for why that check lives here
    // and not in the pure verifier). Never throws for any attestation-shaped
    // failure — every branch below returns a distinct AttestationVerifyOutcome
    // status; only a genuinely broken git repo (HEAD^{tree} unresolvable)
    // throws, matching LocateCiRunAsync's precedent for "this should never
    // happen in a real checkout".
    private static async Task<AttestationVerifyOutcome> VerifyAttestationAsync(string repoRoot)
    {
      // (a) Best-effort forced fetch of the notes ref. A repo that has never
      // been attested has no such ref on origin at all — `git fetch` exits
      // 128 with "couldn't find remote ref ..." on stderr in that case; that
      // is expected and not a fetch failure, just a signal that RefMissing
      // (rather than plain NoNote) is the more accurate verdict below.
      CommandOutput fetchResult = await Shell.Builder("git")
        .WithArguments("fetch", "origin", $"+{AttestationVerifier.NotesRef}:{AttestationVerifier.NotesRef}")
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None)
        .ConfigureAwait(false);

      bool remoteNotesRefMissing = fetchResult.ExitCode == 128
        && fetchResult.Stderr.Contains("couldn't find remote ref", StringComparison.OrdinalIgnoreCase);

      // (b) Resolve HEAD's tree — the note is keyed by tree, not commit.
      CommandOutput treeResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "HEAD^{tree}")
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None)
        .ConfigureAwait(false);

      if (treeResult.ExitCode != 0)
      {
        throw new InvalidOperationException($"Could not resolve HEAD tree for attestation verify: {treeResult.Stderr.Trim()}");
      }

      string treeSha = treeResult.Stdout.Trim();

      // (c) Read the note for this tree.
      CommandOutput noteResult = await Shell.Builder("git")
        .WithArguments("notes", $"--ref={AttestationVerifier.NotesRefShort}", "show", treeSha)
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None)
        .ConfigureAwait(false);

      string? noteJson = noteResult.ExitCode == 0 ? noteResult.Stdout.Trim() : null;

      if (string.IsNullOrWhiteSpace(noteJson))
      {
        bool noLocalNote = noteResult.ExitCode == 1
          && noteResult.Stderr.Contains("no note found", StringComparison.OrdinalIgnoreCase);

        // RefMissing (never attested anywhere, ever) is a more actionable
        // verdict than plain NoNote (ref exists, just not for this tree) —
        // only claim it when BOTH the remote ref and a local note are absent.
        AttestationVerificationStatus status = remoteNotesRefMissing && noLocalNote
          ? AttestationVerificationStatus.RefMissing
          : AttestationVerificationStatus.NoNote;

        return new AttestationVerifyOutcome(status, treeSha, null, null, null);
      }

      // (d) Pure evaluation — parse, schema/field checks, key lookup, tree match.
      AttestationEvaluation evaluation = AttestationVerifier.Evaluate(noteJson, treeSha);

      if (evaluation.Status != AttestationVerificationStatus.ReadyToVerify)
      {
        return new AttestationVerifyOutcome(evaluation.Status, treeSha, evaluation.Note?.CheckSet, evaluation.Note?.Ts, evaluation.Detail);
      }

      // (e) Ed25519 verify via openssl — the one step that is genuinely a
      // process launch, isolated here so everything above stays unit-testable.
      DirectoryInfo tempDir = Directory.CreateTempSubdirectory("dev-attest-");
      try
      {
        string payloadPath = Path.Combine(tempDir.FullName, "payload.bin");
        string sigPath = Path.Combine(tempDir.FullName, "sig.bin");
        string pemPath = Path.Combine(tempDir.FullName, "pub.pem");

        await File.WriteAllBytesAsync(payloadPath, evaluation.CanonicalBytes!).ConfigureAwait(false);
        await File.WriteAllBytesAsync(sigPath, evaluation.SignatureBytes!).ConfigureAwait(false);
        await File.WriteAllTextAsync(pemPath, evaluation.PublicKeyPem).ConfigureAwait(false);

        CommandOutput verifyResult;
        try
        {
          verifyResult = await Shell.Builder("openssl")
            .WithArguments("pkeyutl", "-verify", "-pubin", "-inkey", pemPath, "-rawin", "-in", payloadPath, "-sigfile", sigPath)
            .WithNoValidation()
            .CaptureAsync(CancellationToken.None)
            .ConfigureAwait(false);
        }
        catch (System.ComponentModel.Win32Exception)
        {
          return new AttestationVerifyOutcome(
            AttestationVerificationStatus.VerifierUnavailable,
            treeSha,
            evaluation.Note!.CheckSet,
            evaluation.Note.Ts,
            "openssl could not be launched");
        }

        AttestationVerificationStatus finalStatus = verifyResult.ExitCode == 0
          ? AttestationVerificationStatus.Valid
          : AttestationVerificationStatus.BadSignature;

        return new AttestationVerifyOutcome(
          finalStatus,
          treeSha,
          evaluation.Note!.CheckSet,
          evaluation.Note.Ts,
          finalStatus == AttestationVerificationStatus.BadSignature ? verifyResult.Stderr.Trim() : null);
      }
      finally
      {
        tempDir.Delete(recursive: true);
      }
    }

    // Single source of truth for the operator-facing message per outcome —
    // shared by PR/merge mode (advisory or required-failure text) and
    // release mode (hard-gate failure text). Messages match kanban task
    // 458-010's frozen guidance verbatim ("pull master locally so ganda can
    // attest" / "update TimeWarp.Nuru.DevCli" / "install openssl" /
    // "re-attest via ganda").
    private static string DescribeAttestationOutcome(AttestationVerifyOutcome outcome)
    {
      string shortTree = ShortSha(outcome.Tree);

      return outcome.Status switch
      {
        AttestationVerificationStatus.Valid =>
          $"Attestation valid: check_set {ShortSha(outcome.CheckSet)} ts {outcome.Ts}",

        AttestationVerificationStatus.NoNote or AttestationVerificationStatus.RefMissing =>
          $"tree {shortTree} is unattested — pull master locally so ganda can attest (ganda repo attest)",

        AttestationVerificationStatus.UnknownKey =>
          $"tree {shortTree} attestation uses an unrecognized key_id — update TimeWarp.Nuru.DevCli ({outcome.Detail})",

        AttestationVerificationStatus.VerifierUnavailable =>
          "openssl not found — install openssl",

        AttestationVerificationStatus.TreeMismatch or AttestationVerificationStatus.BadSignature =>
          $"tree {shortTree} attestation invalid (possible tampering) — re-attest via ganda",

        AttestationVerificationStatus.ParseFailure =>
          $"tree {shortTree} attestation note could not be parsed ({outcome.Detail})",

        _ => $"tree {shortTree} attestation verification returned unexpected status '{outcome.Status}'"
      };
    }

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

    private static string ShortSha(string? sha) =>
      string.IsNullOrEmpty(sha) ? "(unknown)" : sha.Length > 7 ? sha[..7] : sha;

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
        runListResult = await Shell.Builder("gh")
          .WithArguments("run", "list", "--workflow", "workflow.yml", "--commit", headSha, "--status", "success", "--json", "databaseId,event,headSha,createdAt")
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
        CommandOutput artifactsResult = await Shell.Builder("gh")
          .WithArguments("api", $"repos/{{owner}}/{{repo}}/actions/runs/{run.DatabaseId}/artifacts")
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

        int exitCode = await Shell.Builder("gh")
          .WithArguments("run", "download", run.DatabaseId.ToString(System.Globalization.CultureInfo.InvariantCulture), "--name", artifact.Name, "--dir", artifactsDir)
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

    // Push order is cosmetic: NuGet does not validate inter-package dependencies
    // at push time (kanban task 458-004, decision D1). projects is the derived
    // packable set (IPackableProjectService), sorted by PackageId.
    private async Task PushPackagesAsync(string repoRoot, IReadOnlyList<PackableProject> projects, string? apiKey)
    {
      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");

      // Read version to construct package names
      string? version = ReadPropsVersion(repoRoot);

      if (string.IsNullOrEmpty(version))
      {
        throw new InvalidOperationException("Could not determine version for push");
      }

      HashSet<string> expectedNupkgFileNames = [.. projects.Select(p => $"{p.PackageId}.{version}.nupkg")];

      // Cross-check: no *.{version}.nupkg file in the artifacts directory may
      // fall outside the derived packable set — stronger than a glob-only push,
      // catches a stray/leftover package (e.g. from a renamed or removed
      // project) that would otherwise be pushed unnoticed.
      string[] actualNupkgFiles = Directory.Exists(artifactsDir)
        ? Directory.GetFiles(artifactsDir, $"*.{version}.nupkg")
        : [];

      List<string> unexpectedNupkgFileNames = [];
      foreach (string filePath in actualNupkgFiles)
      {
        string fileName = Path.GetFileName(filePath);
        if (!expectedNupkgFileNames.Contains(fileName))
        {
          unexpectedNupkgFileNames.Add(fileName);
        }
      }

      if (unexpectedNupkgFileNames.Count > 0)
      {
        throw new InvalidOperationException($"Unexpected package(s) in {artifactsDir} not in the derived packable set: {string.Join(", ", unexpectedNupkgFileNames)}");
      }

      foreach (PackableProject project in projects)
      {
        string nupkgPath = Path.Combine(artifactsDir, $"{project.PackageId}.{version}.nupkg");

        if (!File.Exists(nupkgPath))
        {
          throw new FileNotFoundException($"Package not found: {nupkgPath}");
        }

        Terminal.WriteLine($"Pushing {project.PackageId}.{version}.nupkg...");

        // Build push arguments - include API key if provided (from OIDC Trusted Publishing)
        List<string> args = ["nuget", "push", nupkgPath, "--source", "https://api.nuget.org/v3/index.json", "--skip-duplicate"];

        if (!string.IsNullOrEmpty(apiKey))
        {
          args.AddRange(["--api-key", apiKey]);
        }

        int exitCode = await Shell.Builder("dotnet")
          .WithArguments([.. args])
          .WithWorkingDirectory(repoRoot)
          .RunAsync();

        if (exitCode != 0)
        {
          throw new InvalidOperationException($"Failed to push {project.PackageId}!");
        }
      }

      Terminal.WriteLine("\nAll packages pushed successfully!");
    }

    // Outcome of the release-gate ancestor check — a git-error verdict must
    // never be silently coerced into "not an ancestor".
    private enum AncestorCheckStatus
    {
      Ancestor,
      NotAncestor,
      MasterUnresolvable,
      GitError
    }

    private sealed record AncestorCheckOutcome(AncestorCheckStatus Status, string? Detail);

    // Outcome of the release-gate tag-pin check — Mismatch and GitError are
    // distinct verdicts and must not be collapsed into one message.
    private enum TagPinStatus
    {
      NoTag,
      Match,
      Mismatch,
      GitError
    }

    private sealed record TagPinOutcome(TagPinStatus Status, string? TagCommit, string? HeadCommit, string? Detail);

    // Outcome of locating the CI run to promote. GhUnavailable (gh could not
    // be launched at all), GhFailed (gh launched and ran but exited nonzero —
    // Detail carries its stderr), and NoMatchingRun (gh ran fine, exited 0,
    // nothing matched this commit) are three DIFFERENT verdicts requiring
    // different operator remediation and must not collapse into one message
    // (round-1 review finding #2).
    private enum LocateRunStatus
    {
      Found,
      GhUnavailable,
      GhFailed,
      NoMatchingRun
    }

    private sealed record LocateRunOutcome(LocateRunStatus Status, string HeadSha, IReadOnlyList<CiRunSummary> CandidateRuns, string? Detail);

    // Outcome of walking candidate runs for a downloadable Packages-* artifact.
    private enum DownloadArtifactStatus
    {
      Downloaded,
      Exhausted
    }

    private sealed record DownloadArtifactOutcome(DownloadArtifactStatus Status, CiRunSummary? Run, string? ArtifactName, IReadOnlyList<ExpiredArtifactEncounter> ExpiredEncounters);

    // One candidate run whose only matching artifact(s) were expired — carried
    // through so the final "every candidate exhausted" abort message can name
    // a specific run for `gh run rerun {RunId}` guidance.
    private sealed record ExpiredArtifactEncounter(long RunId, string Event, IReadOnlyList<string> ArtifactNames);

    // Outcome of VerifyAttestationAsync — one of every AttestationVerificationStatus
    // value EXCEPT ReadyToVerify (an internal-to-Evaluate intermediate state
    // that never escapes VerifyAttestationAsync; by the time this record is
    // returned, the openssl step has already resolved it to Valid or
    // BadSignature, or a pre-signature check already resolved it to something
    // else). CheckSet/Ts are populated whenever a note was parsed far enough
    // to have them (including failure paths like TreeMismatch/BadSignature),
    // so failure messages can still cite them when useful.
    private sealed record AttestationVerifyOutcome(AttestationVerificationStatus Status, string? Tree, string? CheckSet, string? Ts, string? Detail);

    // Outcome of RunPrAttestationStepAsync — whether PR/merge mode must abort
    // the pipeline (mode=require + non-Valid) and, when in mode=warn with a
    // non-Valid outcome, the advisory line to repeat just before the
    // SUCCEEDED banner so it survives scrollback from the later steps.
    private sealed record AttestationStepResult(bool ShouldAbort, string? RepeatAdvisory);
  }
}
