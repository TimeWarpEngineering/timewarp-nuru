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
//   release:   tag-gate -> check-version -> clean -> build -> pack -> push
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

    public Handler
    (
      ITerminal terminal,
      IRepoCleanService repoCleanService,
      NuGetVersionService nuGetVersionService,
      IRepoConfigService configService
    )
    {
      Terminal = terminal;
      RepoCleanService = repoCleanService;
      NuGetVersionService = nuGetVersionService;
      ConfigService = configService;
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
      Terminal.WriteLine("Pipeline: clean -> build -> verify-samples -> test");
      Terminal.WriteLine("");

      // Step 1: Clean
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 1/4: Clean");
      Terminal.WriteLine("===============================================================================");
      CleanCommand.Handler cleanHandler = new(Terminal, RepoCleanService);
      await cleanHandler.Handle(new CleanCommand(), CancellationToken.None);

      // Step 2: Build
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 2/4: Build");
      Terminal.WriteLine("===============================================================================");
      BuildCommand.Handler buildHandler = new(Terminal);
      await buildHandler.Handle(new BuildCommand(), CancellationToken.None);

      // Step 3: Verify Samples
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 3/4: Verify Samples");
      Terminal.WriteLine("===============================================================================");
      VerifySamplesCommand.Handler verifySamplesHandler = new(Terminal);
      await verifySamplesHandler.Handle(new VerifySamplesCommand(), CancellationToken.None);

      // Step 4: Test
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 4/4: Test");
      Terminal.WriteLine("===============================================================================");
      TestCommand.Handler testHandler = new(Terminal);
      await testHandler.Handle(new TestCommand(), CancellationToken.None);

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Pipeline SUCCEEDED");
      Terminal.WriteLine("===============================================================================");
    }

    private async Task RunReleaseWorkflowAsync(string? apiKey)
    {
      Terminal.WriteLine("Pipeline: tag-gate -> check-version -> clean -> build -> pack -> push");
      Terminal.WriteLine("");

      // Get repo root for pack/push operations
      string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
      if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
      {
        repoRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
      }

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
        Terminal.WriteLine("Tag assertion skipped: GITHUB_EVENT_NAME is not 'release' (break-glass/local release has no tag).");
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
        default:
          break;
      }

      // Step 2: Check Version
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 2/6: Check Version");
      Terminal.WriteLine("===============================================================================");
      CheckVersionCommand.Handler checkVersionHandler = new(Terminal, NuGetVersionService, ConfigService);
      await checkVersionHandler.Handle(new CheckVersionCommand(), CancellationToken.None);

      if (Environment.ExitCode != 0)
      {
        AbortPipeline("version already released");
        return;
      }

      // Step 3: Clean
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 3/6: Clean");
      Terminal.WriteLine("===============================================================================");
      CleanCommand.Handler cleanHandler = new(Terminal, RepoCleanService);
      await cleanHandler.Handle(new CleanCommand(), CancellationToken.None);

      // Step 4: Build
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 4/6: Build");
      Terminal.WriteLine("===============================================================================");
      BuildCommand.Handler buildHandler = new(Terminal);
      await buildHandler.Handle(new BuildCommand(), CancellationToken.None);

      // Step 5: Pack
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 5/6: Pack");
      Terminal.WriteLine("===============================================================================");
      await PackProjectsAsync(repoRoot);

      // Step 6: Push
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 6/6: Push to NuGet");
      Terminal.WriteLine("===============================================================================");
      await PushPackagesAsync(repoRoot, apiKey);

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

    private async Task PackProjectsAsync(string repoRoot)
    {
      // Create artifacts directory
      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");
      Directory.CreateDirectory(artifactsDir);

      // Projects to pack (in dependency order)
      string[] projectsToPack =
      [
        "source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj",
        "source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj",
        "source/timewarp-nuru/timewarp-nuru.csproj",
        "source/timewarp-nuru-search/timewarp-nuru-search.csproj",
        "source/timewarp-nuru-devcli/timewarp-nuru-devcli.csproj"
      ];

      foreach (string projectPath in projectsToPack)
      {
        string fullPath = Path.Combine(repoRoot, projectPath);
        Terminal.WriteLine($"Packing {projectPath}...");

        int exitCode = await Shell.Builder("dotnet")
          .WithArguments("pack", fullPath, "--configuration", "Release", "--output", artifactsDir, "--no-build")
          .WithWorkingDirectory(repoRoot)
          .RunAsync();

        if (exitCode != 0)
        {
          throw new InvalidOperationException($"Failed to pack {projectPath}!");
        }
      }

      Terminal.WriteLine($"\nPackages created in: {artifactsDir}");
    }

    private async Task PushPackagesAsync(string repoRoot, string? apiKey)
    {
      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");

      // Read version to construct package names
      string? version = ReadPropsVersion(repoRoot);

      if (string.IsNullOrEmpty(version))
      {
        throw new InvalidOperationException("Could not determine version for push");
      }

      // Packages in dependency order
      string[] packages =
      [
        "TimeWarp.Nuru.Analyzers",
        "TimeWarp.Nuru.Mcp",
        "TimeWarp.Nuru",
        "TimeWarp.Nuru.Search",
        "TimeWarp.Nuru.DevCli"
      ];

      foreach (string package in packages)
      {
        string nupkgPath = Path.Combine(artifactsDir, $"{package}.{version}.nupkg");

        if (!File.Exists(nupkgPath))
        {
          throw new FileNotFoundException($"Package not found: {nupkgPath}");
        }

        Terminal.WriteLine($"Pushing {package}.{version}.nupkg...");

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
          throw new InvalidOperationException($"Failed to push {package}!");
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
  }
}
