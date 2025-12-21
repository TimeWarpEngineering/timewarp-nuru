#!/usr/bin/dotnet --

// verify-samples.cs - Build all samples to verify they compile
// Usage: dotnet runfiles/verify-samples.cs
//
// This script discovers all runfile samples (*.cs with shebang) and project samples (*.csproj)
// in the samples/ directory and builds each one to catch compilation regressions early.

using TimeWarp.Amuru;

// Get script directory and repo root
string scriptDir = AppContext.GetData("EntryPointFileDirectoryPath") as string
  ?? throw new InvalidOperationException("Could not get entry point directory");
string repoRoot = Path.GetFullPath(Path.Combine(scriptDir, ".."));
string samplesDir = Path.Combine(repoRoot, "samples");

WriteLine("=== Verifying Samples ===");
WriteLine($"Samples directory: {samplesDir}");
WriteLine();

// Discover runfile samples (*.cs files with shebang containing "dotnet")
List<string> runfileSamples = [];
foreach (string csFile in Directory.EnumerateFiles(samplesDir, "*.cs", SearchOption.AllDirectories))
{
  try
  {
    using StreamReader reader = new(csFile);
    string? firstLine = await reader.ReadLineAsync();
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
List<string> projectSamples =
[
  ..Directory.EnumerateFiles(samplesDir, "*.csproj", SearchOption.AllDirectories)
];

int totalSamples = runfileSamples.Count + projectSamples.Count;
WriteLine($"Found {runfileSamples.Count} runfile samples and {projectSamples.Count} project samples");
WriteLine();

// Track results
List<string> failedSamples = [];
int currentSample = 0;

// Build runfile samples
foreach (string sample in runfileSamples.Order())
{
  currentSample++;
  string relativePath = Path.GetRelativePath(repoRoot, sample);
  Write($"[{currentSample}/{totalSamples}] {relativePath} ... ");

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
      WriteLine("✅");
    }
    else
    {
      WriteLine("❌");
      failedSamples.Add(relativePath);
    }
  }
  catch (Exception ex)
  {
    WriteLine("❌");
    WriteLine($"  Error: {ex.Message}");
    failedSamples.Add(relativePath);
  }
}

// Build project samples
foreach (string sample in projectSamples.Order())
{
  currentSample++;
  string relativePath = Path.GetRelativePath(repoRoot, sample);
  Write($"[{currentSample}/{totalSamples}] {relativePath} ... ");

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
      WriteLine("✅");
    }
    else
    {
      WriteLine("❌");
      failedSamples.Add(relativePath);
    }
  }
  catch (Exception ex)
  {
    WriteLine("❌");
    WriteLine($"  Error: {ex.Message}");
    failedSamples.Add(relativePath);
  }
}

// Print summary
WriteLine();
WriteLine("=== Summary ===");

if (failedSamples.Count == 0)
{
  WriteLine($"✅ {totalSamples}/{totalSamples} samples built successfully");
  return 0;
}
else
{
  int passed = totalSamples - failedSamples.Count;
  WriteLine($"❌ {passed}/{totalSamples} samples built successfully ({failedSamples.Count} failed)");
  WriteLine();
  WriteLine("Failed samples:");
  foreach (string failed in failedSamples)
  {
    WriteLine($"  - {failed}");
  }

  return 1;
}
