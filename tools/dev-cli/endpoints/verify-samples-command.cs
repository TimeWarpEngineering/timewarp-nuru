#region Purpose
// Build every sample so CI proves the samples still compile.
#endregion

#region Design
// Each sample is a file-based app. `dotnet build file.cs` rejects `-m`, so
// parallelism is one process per sample, bounded by ProcessorCount.
// Sample project references (TimeWarp.Nuru and the analyzer) pack on build.
// Parallel builds race on artifacts/packages/*.nupkg, so those projects are
// built once first and each sample build passes --no-dependencies.
// DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER keeps the sample processes off one
// shared MSBuild server. Output is captured and written under a lock.
#endregion

// ═══════════════════════════════════════════════════════════════════════════════
// VERIFY SAMPLES COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Builds all samples to verify they compile correctly.
// Migrated from runfiles/verify-samples.cs to endpoints pattern.

namespace DevCli;

/// <summary>
/// Build all samples to verify they compile.
/// </summary>
[NuruRoute("verify-samples", Description = "Verify all samples compile")]
public sealed class VerifySamplesCommand : ICommand<Unit>
{
  [Option("category", "c", Description = "Filter by category: fluent, endpoints, hybrid")]
  public string? Category { get; set; }

  internal sealed class Handler : ICommandHandler<VerifySamplesCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(VerifySamplesCommand command, CancellationToken ct)
    {
      // Get repo root using Git.FindRoot
      string? repoRoot = Git.FindRoot();

      if (repoRoot is null)
      {
        throw new InvalidOperationException("Could not find git repository root (.git not found)");
      }

      // Verify we're in the right place
      if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
      {
        throw new InvalidOperationException("Could not find repository root (timewarp-nuru.slnx not found)");
      }

      string samplesDir = Path.Combine(repoRoot, "samples");

      Terminal.WriteLine("=== Verifying Samples ===");
      Terminal.WriteLine($"Samples directory: {samplesDir}");
      Terminal.WriteLine("");

      // Discover runfile samples (*.cs files with shebang containing "dotnet")
      // Exclude directories starting with underscore (work-in-progress samples)
      List<string> runfileSamples = [];
      foreach (string csFile in Directory.EnumerateFiles(samplesDir, "*.cs", SearchOption.AllDirectories))
      {
        // Skip files in directories starting with underscore
        string relativePath = Path.GetRelativePath(samplesDir, csFile);
        if (relativePath.Split(Path.DirectorySeparatorChar).Any(part => part.StartsWith('_')))
        {
          continue;
        }

        try
        {
          using StreamReader reader = new(csFile);
          string? firstLine = await reader.ReadLineAsync(ct);
          if (firstLine?.StartsWith("#!", StringComparison.Ordinal) == true &&
              firstLine.Contains("dotnet", StringComparison.Ordinal))
          {
            runfileSamples.Add(csFile);
          }
        }
        catch
        {
          // Skip files we can't read
        }
      }

      // Discover project samples (*.csproj files)
      // Exclude directories starting with underscore (work-in-progress samples)
      List<string> projectSamples =
      [
        .. Directory.EnumerateFiles(samplesDir, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !Path.GetRelativePath(samplesDir, f)
                .Split(Path.DirectorySeparatorChar)
                .Any(part => part.StartsWith('_')))
      ];

      // Apply category filter if specified
      runfileSamples = FilterByCategory(runfileSamples, command.Category, samplesDir);
      projectSamples = FilterByCategory(projectSamples, command.Category, samplesDir);

      int totalSamples = runfileSamples.Count + projectSamples.Count;

      if (!string.IsNullOrEmpty(command.Category))
      {
        Terminal.WriteLine($"Category filter: {command.Category}");
      }

      int parallelism = Math.Max(1, Environment.ProcessorCount);
      Terminal.WriteLine($"Found {runfileSamples.Count} runfile samples and {projectSamples.Count} project samples");
      Terminal.WriteLine($"Parallelism: {parallelism}");
      Terminal.WriteLine("");

      List<string> failedSamples = [];
      await BuildSharedProjectsAsync(repoRoot, ct);

      List<string> orderedSamples =
      [
        .. runfileSamples.Order(),
        .. projectSamples.Order()
      ];

      await BuildSamplesAsync(orderedSamples, repoRoot, totalSamples, parallelism, failedSamples, ct);

      // Print summary
      Terminal.WriteLine("");
      Terminal.WriteLine("=== Summary ===");

      if (failedSamples.Count == 0)
      {
        Terminal.WriteLine($"{totalSamples}/{totalSamples} samples built successfully");
        return Unit.Value;
      }

      int passed = totalSamples - failedSamples.Count;
      Terminal.WriteLine($"{passed}/{totalSamples} samples built successfully ({failedSamples.Count} failed)");
      Terminal.WriteLine("");
      Terminal.WriteLine("Failed samples:");
      foreach (string failed in failedSamples)
      {
        Terminal.WriteLine($"  - {failed}");
      }

      throw new InvalidOperationException($"{failedSamples.Count} sample(s) failed to build");
    }

    private async Task BuildSharedProjectsAsync(string repoRoot, CancellationToken cancellationToken)
    {
      // Order matches dev build: the analyzer and the build task before Nuru,
      // which packs both into the nupkg. One process, so the pack has no race.
      string[] sharedProjects =
      [
        "source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj",
        "source/timewarp-nuru-build/timewarp-nuru-build.csproj",
        "source/timewarp-nuru/timewarp-nuru.csproj"
      ];

      Terminal.WriteLine("Building shared projects once before the parallel sample builds...");
      foreach (string project in sharedProjects)
      {
        string fullPath = Path.Combine(repoRoot, project);
        CommandOutput output = await DotNet.Build()
          .WithProject(fullPath)
          .WithConfiguration("Release")
          .WithVerbosity("quiet")
          .WithNoValidation()
          .CaptureAsync(cancellationToken);

        if (output.ExitCode != 0)
        {
          string detail = FormatBuildFailure(output);
          Terminal.WriteLine($"{project} ... FAILED");
          Terminal.WriteLine(detail);
          throw new InvalidOperationException($"Failed to build {project}");
        }

        Terminal.WriteLine($"{project} ... OK");
      }

      Terminal.WriteLine("");
    }

    private async Task BuildSamplesAsync
    (
      List<string> samples,
      string repoRoot,
      int totalSamples,
      int parallelism,
      List<string> failedSamples,
      CancellationToken cancellationToken
    )
    {
      object writeLock = new();
      int completed = 0;
      ParallelOptions options = new()
      {
        MaxDegreeOfParallelism = parallelism,
        CancellationToken = cancellationToken
      };

      await Parallel.ForEachAsync(samples, options, async (sample, token) =>
      {
        string relativePath = Path.GetRelativePath(repoRoot, sample);
        bool succeeded = false;
        string? failureDetail = null;

        try
        {
          CommandOutput output = await DotNet.Build()
            .WithProject(sample)
            .WithConfiguration("Release")
            .WithVerbosity("quiet")
            .WithNoDependencies()
            .WithNoValidation()
            .WithEnvironmentVariable("DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER", "1")
            .CaptureAsync(token);

          succeeded = output.ExitCode == 0;
          if (!succeeded)
          {
            failureDetail = FormatBuildFailure(output);
          }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
          failureDetail = ex.Message;
        }

        int finished = Interlocked.Increment(ref completed);
        lock (writeLock)
        {
          Terminal.Write($"[{finished}/{totalSamples}] {relativePath} ... ");
          if (succeeded)
          {
            Terminal.WriteLine("OK");
          }
          else
          {
            Terminal.WriteLine("FAILED");
            if (!string.IsNullOrWhiteSpace(failureDetail))
            {
              Terminal.WriteLine(failureDetail);
            }

            failedSamples.Add(relativePath);
          }
        }
      });
    }

    private static string FormatBuildFailure(CommandOutput output)
    {
      string detail = string.Join
      (
        Environment.NewLine,
        new[] { output.Stdout, output.Stderr }.Where(static text => !string.IsNullOrWhiteSpace(text))
      );

      if (string.IsNullOrWhiteSpace(detail))
      {
        return $"  Error: exit code {output.ExitCode}";
      }

      return detail;
    }

    private static List<string> FilterByCategory(List<string> samples, string? category, string samplesDir)
    {
      if (string.IsNullOrEmpty(category))
        return samples;

      return [.. samples.Where(s =>
      {
        string relativePath = Path.GetRelativePath(samplesDir, s);
        string[] parts = relativePath.Split(Path.DirectorySeparatorChar);

        return category.ToLowerInvariant() switch
        {
          "fluent" => parts[0] == "fluent",
          "endpoints" => parts[0] == "endpoints",
          "hybrid" => parts[0] == "hybrid",
          _ => throw new ArgumentException($"Unknown category: {category}. Valid options: fluent, endpoints, hybrid")
        };
      })];
    }
  }
}
