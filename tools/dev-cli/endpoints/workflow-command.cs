#region Purpose
// Dev CLI command for TimeWarp.Nuru development workflow
#endregion

#region Design
// PR/merge prints ProcessorCount and a duration after each step so a hosted
// log can be compared phase by phase without scraping timestamps.
// The same boundaries are child spans (WorkflowStageSpan) parented to the
// workflow command span. The terminal lines stay; the trace is a second copy.
// --stage runs one slice of the PR/merge pipeline (task 538). workflow.yml runs
// build, samples, test-multi and test-standalone as parallel jobs, and its
// final `ci` job uploads the build job's packages once every job passed.
// The package integrity gate needs build and tests on one machine, so only
// the full pipeline (stage all, the local default) runs it. In CI the
// separate runners give the same guarantee.
// Pull request build decision (task 595): PullRequestBuildClassifier from
// TimeWarp.Nuru.DevCli content decides whether a pull request needs build and
// test. --stage classify runs only that decision and writes code=true|false to
// GITHUB_OUTPUT; workflow.yml's changes job runs it, and the stage jobs skip on
// code=false. --stage all (the local default) asks the same question first and
// stops before attestation when the answer is skip. The single stages never
// re-ask: the changes job already did. Repo additions to the rule live in
// .timewarp/dev.jsonc under pullRequestBuild.
// Green-key ledger (task 540, enable task 603): --stage green-key logs hit or
// miss and writes green_key_run. The workflow skips stages on skip, runs only
// the build stage on pack-only, and runs every stage on full. --stage
// green-key-skip attests and, on a master skip, downloads the carried
// Packages-* into artifacts/packages. --stage green-key-finish logs the end
// line and stages a marker after a full success only. A skip does not retest,
// so it does not stage a marker. classify and all observe too.
// Partial files: .verification, .release, .attestation, .publish.
#endregion

// ═══════════════════════════════════════════════════════════════════════════════
// WORKFLOW COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Orchestrates the full CI/CD pipeline with mode detection.
// Auto-detects mode from GITHUB_EVENT_NAME or accepts explicit --mode flag.
//
// Modes:
//   pr/merge:  [pull request build decision] -> attestation -> clean -> build -> verify-samples -> test
//              (--stage build|samples|test-multi|test-standalone runs one slice;
//              --stage classify runs only the decision)
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
public sealed partial class WorkflowCommand : ICommand<Unit>
{
  [Option("mode", "m", Description = "CI mode: pr, merge, or release (auto-detected from GITHUB_EVENT_NAME if not specified)")]
  public string? Mode { get; set; }

  [Option("stage", "s", Description = "PR/merge stage: all (default), classify, build, samples, test-multi, test-standalone, green-key, green-key-skip, or green-key-finish")]
  public string? Stage { get; set; }

  [Option("api-key", Description = "NuGet API key for publishing (from OIDC Trusted Publishing)")]
  public string? ApiKey { get; set; }

  [Option("attestation", Description = "Attestation mode override for this run: off, warn, or require (overrides .timewarp/dev.jsonc attestation.mode)")]
  public string? Attestation { get; set; }

  internal sealed partial class Handler : IRequestHandler<WorkflowCommand, Unit>
  {
    private const string StageAll = "all";
    private const string StageClassify = "classify";
    private const string StageBuild = "build";
    private const string StageSamples = "samples";
    private const string StageTestMulti = "test-multi";
    private const string StageTestStandalone = "test-standalone";
    private const string StageGreenKey = "green-key";
    private const string StageGreenKeySkip = "green-key-skip";
    private const string StageGreenKeyFinish = "green-key-finish";

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

      string stage = string.IsNullOrEmpty(command.Stage) ? StageAll : command.Stage;
      if (stage is not (
        StageAll or StageClassify or StageBuild or StageSamples or StageTestMulti or StageTestStandalone
        or StageGreenKey or StageGreenKeySkip or StageGreenKeyFinish))
      {
        throw new ArgumentException(
          $"Unknown stage: {stage}. Valid options: all, classify, build, samples, test-multi, test-standalone, green-key, green-key-skip, green-key-finish");
      }

      // Before the release check. A release event may log the key, and must not
      // be told that --stage applies to PR/merge mode only.
      if (stage == StageGreenKey)
      {
        await GreenKeyLedger.ObserveAsync(Terminal, mode == CiMode.Release, ct).ConfigureAwait(false);
        return Unit.Value;
      }

      if (stage == StageGreenKeySkip)
      {
        await RunGreenKeySkipAsync().ConfigureAwait(false);
        return Unit.Value;
      }

      if (stage == StageGreenKeyFinish)
      {
        GreenKeyLedger.FinishFromEnvironment(Terminal);
        return Unit.Value;
      }

      if (mode == CiMode.Release)
      {
        if (stage != StageAll)
        {
          throw new ArgumentException("--stage applies to PR/merge mode only; release mode builds nothing.");
        }

        await RunReleaseWorkflowAsync(command.ApiKey, command.Attestation, ModeName(mode));
      }
      else
      {
        GreenKeyObservation? observation = null;
        string? greenPipeline = null;
        try
        {
          if (stage is StageAll or StageClassify)
          {
            // Task 595: the fleet pull request build decision, one call.
            PullRequestBuildDecision decision = await PullRequestBuildClassifier.DecideAsync(ResolveRepoRoot(), ct);
            Terminal.WriteLine(decision.Message);
            observation = await GreenKeyLedger.ObserveAsync(Terminal, releaseMode: false, ct).ConfigureAwait(false);
            await WriteGitHubOutputAsync("code", decision.RequiresBuild ? "true" : "false", ct);
            if (!decision.RequiresBuild)
            {
              // ci does not run when code=false, so the end line is logged here.
              greenPipeline = GreenKeyLog.PathSkipped;
              return Unit.Value;
            }

            if (stage == StageClassify)
            {
              return Unit.Value;
            }
          }

          await RunPrWorkflowAsync(command.Attestation, stage, ModeName(mode));
          if (observation is not null)
          {
            greenPipeline = Environment.ExitCode == 0 ? GreenKeyLog.Succeeded : GreenKeyLog.Failed;
          }
        }
        catch (Exception)
        {
          if (observation is not null)
          {
            greenPipeline = GreenKeyLog.Failed;
          }

          throw;
        }
        finally
        {
          if (observation is not null && greenPipeline is not null)
          {
            try
            {
              GreenKeyLedger.Finish(Terminal, observation, greenPipeline);
            }
            catch (Exception exception)
            {
              Terminal.WriteErrorLine(
                "green-key-v1 marker-write=failed error=" + GreenKeyLog.Redact(exception.Message));
            }
          }
        }
      }

      return Unit.Value;
    }

    private static async Task WriteGitHubOutputAsync(string name, string value, CancellationToken ct)
    {
      string? outputPath = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
      if (string.IsNullOrEmpty(outputPath))
      {
        return;
      }

      await File.AppendAllTextAsync(outputPath, $"{name}={value}\n", ct);
    }

    private void WriteStepDuration(Stopwatch step, string label)
    {
      Terminal.WriteLine($"{label} finished in {FormatSeconds(step.Elapsed)}");
      step.Restart();
    }

    private static string FormatSeconds(TimeSpan elapsed)
      => elapsed.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s";

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

    private static string ModeName(CiMode mode) => mode switch
    {
      CiMode.Pr => "pr",
      CiMode.Merge => "merge",
      CiMode.Release => "release",
      _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown CI mode.")
    };

    private static async Task<bool> InStageAsync(string mode, string stage, Func<Task<WorkflowStageSpan.Result>> body)
    {
      using WorkflowStageSpan.Scope scope = WorkflowStageSpan.Begin(mode, stage, stage);
      try
      {
        WorkflowStageSpan.Result result = await body().ConfigureAwait(false);
        if (!result.Continue)
        {
          scope.Fail(result.ErrorType);
          return false;
        }

        scope.Succeed();
        return true;
      }
      catch (Exception exception)
      {
        scope.Fail(exception.GetType().Name);
        throw;
      }
    }

    private async Task RunPrWorkflowAsync(string? cliAttestationMode, string stage, string mode)
    {
      WorkflowStageSpan.TagPipeline(Activity.Current);

      bool all = stage == StageAll;
      bool runBuild = all || stage == StageBuild;
      bool runSamples = all || stage == StageSamples;
      string? testPhase = stage switch
      {
        StageAll => "all",
        StageTestMulti => "multi",
        StageTestStandalone => "standalone",
        _ => null
      };

      List<string> stepNames = [];
      if (runBuild)
      {
        stepNames.AddRange(["Attestation", "Clean", "Build"]);
      }

      if (runSamples)
      {
        stepNames.Add("Verify Samples");
      }

      if (testPhase is not null)
      {
        stepNames.Add(all ? "Test" : $"Test ({testPhase})");
      }

      Terminal.WriteLine($"Pipeline: {string.Join(" -> ", stepNames.Select(static name => name.ToLowerInvariant()))}");
      if (!all)
      {
        Terminal.WriteLine($"Stage: {stage} (the other stages run as parallel CI jobs)");
      }

      Terminal.WriteLine($"ProcessorCount: {Environment.ProcessorCount}");
      Terminal.WriteLine("");

      string repoRoot = ResolveRepoRoot();
      Stopwatch pipeline = Stopwatch.StartNew();
      Stopwatch step = Stopwatch.StartNew();
      int stepNumber = 0;
      AttestationStepResult? attestationResult = null;
      Dictionary<string, string>? packageHashes = null;
      string packagesDir = Path.Combine(repoRoot, "artifacts", "packages");

      if (runBuild)
      {
        // Attestation
        bool attestationOk = await InStageAsync(mode, "attestation", async () =>
        {
          string label = BeginStep(++stepNumber, stepNames);
          attestationResult = await RunAttestationPolicyStepAsync(
            repoRoot,
            cliAttestationMode,
            AttestationConfigResolver.DefaultPrMode).ConfigureAwait(false);
          WriteStepDuration(step, label);

          if (attestationResult.ShouldAbort)
          {
            AbortPipeline("attestation required (mode=require) and not valid");
            return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PipelineAbort);
          }

          return WorkflowStageSpan.Result.Done;
        }).ConfigureAwait(false);
        if (!attestationOk)
        {
          return;
        }

        // Clean
        await InStageAsync(mode, "clean", async () =>
        {
          string label = BeginStep(++stepNumber, stepNames);
          CleanCommand.Handler cleanHandler = new(Terminal, RepoCleanService);
          await cleanHandler.Handle(new CleanCommand(), CancellationToken.None);
          WriteStepDuration(step, label);
          return WorkflowStageSpan.Result.Done;
        }).ConfigureAwait(false);

        // Build
        bool buildOk = await InStageAsync(mode, "build", async () =>
        {
          string label = BeginStep(++stepNumber, stepNames);
          BuildCommand.Handler buildHandler = new(Terminal);
          await buildHandler.Handle(new BuildCommand(), CancellationToken.None);

          if (!VerifyBuiltPackages())
          {
            return WorkflowStageSpan.Result.Stop(WorkflowStageSpan.PackageLayoutGate);
          }

          // Package integrity gate (task 503), part 1: snapshot the hash of every nupkg
          // so the end of the pipeline can prove the samples/tests steps did not
          // rebuild or repack them.
          packageHashes = HashPackages(packagesDir);
          WriteStepDuration(step, label);
          return WorkflowStageSpan.Result.Done;
        }).ConfigureAwait(false);
        if (!buildOk)
        {
          return;
        }
      }

      if (runSamples)
      {
        await InStageAsync(mode, "verify-samples", async () =>
        {
          string label = BeginStep(++stepNumber, stepNames);
          VerifySamplesCommand.Handler verifySamplesHandler = new(Terminal);
          await verifySamplesHandler.Handle(new VerifySamplesCommand(), CancellationToken.None);
          WriteStepDuration(step, label);
          return WorkflowStageSpan.Result.Done;
        }).ConfigureAwait(false);
      }

      if (testPhase is not null)
      {
        await InStageAsync(mode, "test", async () =>
        {
          string label = BeginStep(++stepNumber, stepNames);
          TestCommand.Handler testHandler = new(Terminal);
          // GitHub Actions legs collect coverage (task 574); the workflow
          // installs dotnet-coverage and uploads artifacts/coverage.
          bool coverage = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
          await testHandler.Handle(new TestCommand { Phase = testPhase, Coverage = coverage }, CancellationToken.None);
          WriteStepDuration(step, label);
          return WorkflowStageSpan.Result.Done;
        }).ConfigureAwait(false);
      }

      // Package integrity gate (task 503), part 2: the artifact uploaded after this
      // pipeline must be byte-identical to the Release packages verified in the
      // build step. Only the full pipeline runs build, samples and tests on one
      // machine. Split CI (task 538) builds and uploads the packages in the build
      // job; the samples and test jobs run on other runners and cannot touch them.
      if (all && packageHashes is not null)
      {
        if (!VerifyPackagesUnchanged(packagesDir, packageHashes))
        {
          return;
        }
      }

      pipeline.Stop();
      Terminal.WriteLine($"Pipeline finished in {FormatSeconds(pipeline.Elapsed)}");

      // Attestation advisory (warn mode only) is repeated here, immediately
      // before the SUCCEEDED banner, so it is not lost above scrollback from
      // clean/build/verify-samples/test output (round-1 self-review finding).
      if (attestationResult?.RepeatAdvisory is not null)
      {
        Terminal.WriteLine("");
        Terminal.WriteLine(attestationResult.RepeatAdvisory.Yellow());
      }

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Pipeline SUCCEEDED");
      Terminal.WriteLine("===============================================================================");
    }
  }
}
