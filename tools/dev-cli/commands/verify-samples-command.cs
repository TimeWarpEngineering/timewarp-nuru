// ═══════════════════════════════════════════════════════════════════════════════
// VERIFY SAMPLES COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Builds all samples to verify they compile correctly.
// Migrated from runfiles/verify-samples.cs to attributed routes pattern.

namespace DevCli.Commands;

/// <summary>
/// Build all samples to verify they compile.
/// </summary>
[NuruRoute("verify-samples", Description = "Verify all samples compile")]
internal sealed class VerifySamplesCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<VerifySamplesCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(VerifySamplesCommand command, CancellationToken ct)
    {
      // Get repo root
      string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

      // Verify we're in the right place
      if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
      {
        repoRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
        if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
        {
          throw new InvalidOperationException("Could not find repository root (timewarp-nuru.slnx not found)");
        }
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

      int totalSamples = runfileSamples.Count + projectSamples.Count;
      Terminal.WriteLine($"Found {runfileSamples.Count} runfile samples and {projectSamples.Count} project samples");
      Terminal.WriteLine("");

      // Track results
      List<string> failedSamples = [];
      int currentSample = 0;

      // Build runfile samples
      foreach (string sample in runfileSamples.Order())
      {
        currentSample++;
        string relativePath = Path.GetRelativePath(repoRoot, sample);
        Terminal.Write($"[{currentSample}/{totalSamples}] {relativePath} ... ");

        try
        {
          CommandResult buildResult = DotNet.Build()
            .WithProject(sample)
            .WithConfiguration("Release")
            .WithVerbosity("quiet")
            .Build();

          int exitCode = await buildResult.RunAsync();

          if (exitCode == 0)
          {
            Terminal.WriteLine("OK");
          }
          else
          {
            Terminal.WriteLine("FAILED");
            failedSamples.Add(relativePath);
          }
        }
        catch (Exception ex)
        {
          Terminal.WriteLine("FAILED");
          Terminal.WriteLine($"  Error: {ex.Message}");
          failedSamples.Add(relativePath);
        }
      }

      // Build project samples
      foreach (string sample in projectSamples.Order())
      {
        currentSample++;
        string relativePath = Path.GetRelativePath(repoRoot, sample);
        Terminal.Write($"[{currentSample}/{totalSamples}] {relativePath} ... ");

        try
        {
          CommandResult buildResult = DotNet.Build()
            .WithProject(sample)
            .WithConfiguration("Release")
            .WithVerbosity("quiet")
            .Build();

          int exitCode = await buildResult.RunAsync();

          if (exitCode == 0)
          {
            Terminal.WriteLine("OK");
          }
          else
          {
            Terminal.WriteLine("FAILED");
            failedSamples.Add(relativePath);
          }
        }
        catch (Exception ex)
        {
          Terminal.WriteLine("FAILED");
          Terminal.WriteLine($"  Error: {ex.Message}");
          failedSamples.Add(relativePath);
        }
      }

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
  }
}
