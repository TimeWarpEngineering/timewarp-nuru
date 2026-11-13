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
// Partial files: .verification, .release, .attestation, .publish.
#endregion

// ═══════════════════════════════════════════════════════════════════════════════
// WORKFLOW COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Orchestrates the full CI/CD pipeline with mode detection.
// Auto-detects mode from GITHUB_EVENT_NAME or accepts explicit --mode flag.
//
// Modes:
//   pr/merge:  attestation -> clean -> build -> verify-samples -> test
//              (--stage build|samples|test-multi|test-standalone runs one slice)
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

  [Option("stage", "s", Description = "PR/merge stage: all (default), build, samples, test-multi, or test-standalone")]
  public string? Stage { get; set; }

  [Option("api-key", Description = "NuGet API key for publishing (from OIDC Trusted Publishing)")]
  public string? ApiKey { get; set; }

  [Option("attestation", Description = "Attestation mode override for this run: off, warn, or require (overrides .timewarp/dev.jsonc attestation.mode)")]
  public string? Attestation { get; set; }

  internal sealed partial class Handler : IRequestHandler<WorkflowCommand, Unit>
  {
    private const string StageAll = "all";
    private const string StageBuild = "build";
    private const string StageSamples = "samples";
    private const string StageTestMulti = "test-multi";
    private const string StageTestStandalone = "test-standalone";

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
      if (stage is not (StageAll or StageBuild or StageSamples or StageTestMulti or StageTestStandalone))
      {
        throw new ArgumentException($"Unknown stage: {stage}. Valid options: all, build, samples, test-multi, test-standalone");
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
        await RunPrWorkflowAsync(command.Attestation, stage, ModeName(mode));
      }

      return Unit.Value;
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
          await testHandler.Handle(new TestCommand { Phase = testPhase }, CancellationToken.None);
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
