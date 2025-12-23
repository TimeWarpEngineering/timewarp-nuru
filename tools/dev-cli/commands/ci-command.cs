// ═══════════════════════════════════════════════════════════════════════════════
// CI COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Orchestrates the full CI/CD pipeline with mode detection.
// Auto-detects mode from GITHUB_EVENT_NAME or accepts explicit --mode flag.
//
// Modes:
//   pr/merge:  clean -> build -> verify-samples -> test
//   release:   clean -> build -> check-version -> pack -> push

namespace DevCli.Commands;

/// <summary>
/// Run the full CI/CD pipeline.
/// </summary>
[NuruRoute("ci", Description = "Run full CI/CD pipeline")]
internal sealed class CiCommand : ICommand<Unit>
{
  [Option("mode", "m", Description = "CI mode: pr, merge, or release (auto-detected from GITHUB_EVENT_NAME if not specified)")]
  public string? Mode { get; set; }

  [Option("api-key", Description = "NuGet API key for publishing (from OIDC Trusted Publishing)")]
  public string? ApiKey { get; set; }

  internal sealed class Handler : ICommandHandler<CiCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly IMediator Mediator;

    public Handler(ITerminal terminal, IMediator mediator)
    {
      Terminal = terminal;
      Mediator = mediator;
    }

    public async ValueTask<Unit> Handle(CiCommand command, CancellationToken ct)
    {
      // Determine CI mode
      CiMode mode = DetermineMode(command.Mode);

      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine($"  CI/CD Pipeline - Mode: {mode}");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("");

      if (mode == CiMode.Release)
      {
        await RunReleaseWorkflowAsync(command.ApiKey, ct);
      }
      else
      {
        await RunPrWorkflowAsync(ct);
      }

      return Unit.Value;
    }

    private CiMode DetermineMode(string? explicitMode)
    {
      // If explicit mode provided, use it
      if (!string.IsNullOrEmpty(explicitMode))
      {
        return explicitMode.ToLowerInvariant() switch
        {
          "pr" => CiMode.Pr,
          "merge" => CiMode.Merge,
          "release" => CiMode.Release,
          _ => CiMode.Pr
        };
      }

      // Auto-detect from GitHub Actions environment
      string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");

      CiMode mode = eventName switch
      {
        "pull_request" => CiMode.Pr,
        "push" => CiMode.Merge,
        "release" => CiMode.Release,
        "workflow_dispatch" => CiMode.Release,
        _ => CiMode.Pr  // Default for local dev
      };

      string displayEventName = eventName ?? "(not set)";
      Terminal.WriteLine($"Detected GITHUB_EVENT_NAME: {displayEventName} -> Mode: {mode}");
      return mode;
    }

    private async Task RunPrWorkflowAsync(CancellationToken ct)
    {
      Terminal.WriteLine("Pipeline: clean -> build -> verify-samples -> test");
      Terminal.WriteLine("");

      // Step 1: Clean
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 1/4: Clean");
      Terminal.WriteLine("===============================================================================");
      await Mediator.Send(new CleanCommand(), ct);

      // Step 2: Build
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 2/4: Build");
      Terminal.WriteLine("===============================================================================");
      await Mediator.Send(new BuildCommand(), ct);

      // Step 3: Verify Samples
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 3/4: Verify Samples");
      Terminal.WriteLine("===============================================================================");
      await Mediator.Send(new VerifySamplesCommand(), ct);

      // Step 4: Test
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 4/4: Test");
      Terminal.WriteLine("===============================================================================");
      await Mediator.Send(new TestCommand(), ct);

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Pipeline SUCCEEDED");
      Terminal.WriteLine("===============================================================================");
    }

    private async Task RunReleaseWorkflowAsync(string? apiKey, CancellationToken ct)
    {
      Terminal.WriteLine("Pipeline: clean -> build -> check-version -> pack -> push");
      Terminal.WriteLine("");

      // Get repo root for pack/push operations
      string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
      if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
      {
        repoRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
      }

      // Step 1: Clean
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 1/5: Clean");
      Terminal.WriteLine("===============================================================================");
      await Mediator.Send(new CleanCommand(), ct);

      // Step 2: Build
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 2/5: Build");
      Terminal.WriteLine("===============================================================================");
      await Mediator.Send(new BuildCommand(), ct);

      // Step 3: Check Version
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 3/5: Check Version");
      Terminal.WriteLine("===============================================================================");
      await Mediator.Send(new CheckVersionCommand(), ct);

      // Step 4: Pack
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 4/5: Pack");
      Terminal.WriteLine("===============================================================================");
      await PackProjectsAsync(repoRoot);

      // Step 5: Push
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 5/5: Push to NuGet");
      Terminal.WriteLine("===============================================================================");
      await PushPackagesAsync(repoRoot, apiKey);

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Pipeline SUCCEEDED - Packages published to NuGet.org");
      Terminal.WriteLine("===============================================================================");
    }

    private async Task PackProjectsAsync(string repoRoot)
    {
      // Create artifacts directory
      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");
      Directory.CreateDirectory(artifactsDir);

      // Projects to pack (in dependency order)
      string[] projectsToPack =
      [
        "source/timewarp-nuru-core/timewarp-nuru-core.csproj",
        "source/timewarp-nuru-logging/timewarp-nuru-logging.csproj",
        "source/timewarp-nuru-completion/timewarp-nuru-completion.csproj",
        "source/timewarp-nuru-telemetry/timewarp-nuru-telemetry.csproj",
        "source/timewarp-nuru-repl/timewarp-nuru-repl.csproj",
        "source/timewarp-nuru/timewarp-nuru.csproj",
        "source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj",
        "source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj"
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
      string propsPath = Path.Combine(repoRoot, "source", "Directory.Build.props");
      XDocument doc = XDocument.Load(propsPath);
      string? version = doc.Descendants("Version").FirstOrDefault()?.Value;

      if (string.IsNullOrEmpty(version))
      {
        throw new InvalidOperationException("Could not determine version for push");
      }

      // Packages in dependency order
      string[] packages =
      [
        "TimeWarp.Nuru.Core",
        "TimeWarp.Nuru.Logging",
        "TimeWarp.Nuru.Completion",
        "TimeWarp.Nuru.Telemetry",
        "TimeWarp.Nuru.Repl",
        "TimeWarp.Nuru",
        "TimeWarp.Nuru.Analyzers",
        "TimeWarp.Nuru.Mcp"
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
  }
}

internal enum CiMode
{
  Pr,
  Merge,
  Release
}
