#!/usr/bin/dotnet --

// aot-benchmark.cs - Build and benchmark AOT CLI framework comparison
//
// This script:
// 1. Publishes each CLI framework benchmark project as Native AOT
// 2. Records which frameworks successfully compile with AOT
// 3. Measures binary sizes
// 4. Uses hyperfine to benchmark cold-start times
// 5. Generates a markdown report
//
// Usage:
//   dotnet run -- runfiles/aot-benchmark.cs
//   dotnet run -- runfiles/aot-benchmark.cs --skip-build   # Skip build, just run benchmarks
//   dotnet run -- runfiles/aot-benchmark.cs --build-only   # Only build, don't benchmark

using System.Globalization;
using System.Text;

// Parse arguments
bool skipBuild = args.Contains("--skip-build");
bool buildOnly = args.Contains("--build-only");

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

string aotBenchmarksDir = Path.GetFullPath("../benchmarks/aot-benchmarks");
string publishDir = Path.Combine(aotBenchmarksDir, "publish");
string resultsDir = Path.Combine(aotBenchmarksDir, "results");
string benchmarkArgs = "--str hello -i 13 -b";

// Framework projects to benchmark
string[] projects =
[
    "bench-consoleappframework",
    "bench-systemcommandline",
    "bench-clifx",
    "bench-mcmaster",
    "bench-nuru",
    "bench-powerargs",
    "bench-commandlineparser",
    "bench-cocona",
    "bench-coconalite",
    "bench-spectreconsole"
];

// Track build results
Dictionary<string, (bool Success, string? Error, long? SizeBytes)> buildResults = [];

WriteLine("╔══════════════════════════════════════════════════════════════════╗");
WriteLine("║         AOT CLI Framework Benchmark Suite                        ║");
WriteLine("╚══════════════════════════════════════════════════════════════════╝");
WriteLine();

// Ensure directories exist
Directory.CreateDirectory(publishDir);
Directory.CreateDirectory(resultsDir);

if (!skipBuild)
{
    WriteLine("=== Phase 1: Building AOT Executables ===");
    WriteLine();

    foreach (string project in projects)
    {
        string projectDir = Path.Combine(aotBenchmarksDir, project);
        string projectOutputDir = Path.Combine(publishDir, project);
        string executablePath = Path.Combine(projectOutputDir, project);

        Write($"  Building {project,-30} ");

        try
        {
            // Clean previous build
            if (Directory.Exists(projectOutputDir))
            {
                Directory.Delete(projectOutputDir, recursive: true);
            }

            // Publish with AOT
            CommandResult publishResult = DotNet.Publish()
                .WithProject(projectDir)
                .WithConfiguration("Release")
                .WithOutput(projectOutputDir)
                .Build();

            int exitCode = await publishResult.RunAsync();

            if (exitCode == 0 && File.Exists(executablePath))
            {
                FileInfo fileInfo = new(executablePath);
                string sizeStr = FormatFileSize(fileInfo.Length);
                WriteLine($"✓ {sizeStr,10}");
                buildResults[project] = (true, null, fileInfo.Length);
            }
            else
            {
                WriteLine("✗ FAILED");
                buildResults[project] = (false, "Build failed or executable not found", null);
            }
        }
        catch (Exception ex)
        {
            // Extract just the relevant error message, not the full command
            string errorMessage = ex.Message;
            int commandIndex = errorMessage.IndexOf("\n\nCommand:", StringComparison.Ordinal);
            if (commandIndex > 0)
            {
                errorMessage = errorMessage[..commandIndex].Trim();
            }
            // Also simplify "Command execution failed..." messages
            if (errorMessage.StartsWith("Command execution failed", StringComparison.Ordinal))
            {
                errorMessage = "AOT publish failed (non-zero exit code)";
            }

            WriteLine($"✗ {errorMessage}");
            buildResults[project] = (false, errorMessage, null);
        }
    }

    WriteLine();
    WriteLine($"  Successful builds: {buildResults.Count(r => r.Value.Success)} / {projects.Length}");
    WriteLine();
}
else
{
    WriteLine("=== Skipping build phase (--skip-build) ===");
    WriteLine();

    // Check for existing builds
    foreach (string project in projects)
    {
        string executablePath = Path.Combine(publishDir, project, project);
        if (File.Exists(executablePath))
        {
            FileInfo fileInfo = new(executablePath);
            buildResults[project] = (true, null, fileInfo.Length);
        }
        else
        {
            buildResults[project] = (false, "Executable not found", null);
        }
    }
}

if (buildOnly)
{
    WriteLine("=== Build only mode (--build-only), skipping benchmarks ===");
    await GenerateReport();
    return;
}

// Phase 2: Run hyperfine benchmark
WriteLine("=== Phase 2: Running Hyperfine Benchmark ===");
WriteLine();

List<string> successfulProjects = [.. buildResults
    .Where(r => r.Value.Success)
    .Select(r => r.Key)];

if (successfulProjects.Count == 0)
{
    WriteLine("  No successful builds to benchmark!");
    await GenerateReport();
    return;
}

// Check if hyperfine is available
CommandResult hyperfineCheck = Shell.Builder("hyperfine")
    .WithArguments("--version")
    .Build();

if (await hyperfineCheck.RunAsync() != 0)
{
    WriteLine("  ERROR: hyperfine is not installed!");
    WriteLine("  Install with: sudo apt install hyperfine");
    await GenerateReport();
    return;
}

// Build hyperfine command
string hyperfineResultsFile = Path.Combine(resultsDir, "hyperfine-results.md");
string hyperfineJsonFile = Path.Combine(resultsDir, "hyperfine-results.json");

// Build the full hyperfine command as a shell command
StringBuilder hyperfineCmd = new();
hyperfineCmd.Append("hyperfine --warmup 3 --runs 100 --ignore-failure ");
hyperfineCmd.Append(CultureInfo.InvariantCulture, $"--export-markdown '{hyperfineResultsFile}' ");
hyperfineCmd.Append(CultureInfo.InvariantCulture, $"--export-json '{hyperfineJsonFile}' ");

foreach (string project in successfulProjects)
{
    string executablePath = Path.Combine(publishDir, project, project);
    hyperfineCmd.Append(CultureInfo.InvariantCulture, $"-n '{project}' '{executablePath} {benchmarkArgs}' ");
}

WriteLine($"  Benchmarking {successfulProjects.Count} frameworks with 100 runs each...");
WriteLine();

// Run via bash to properly handle argument parsing
CommandResult hyperfineResult = Shell.Builder("bash")
    .WithArguments("-c", hyperfineCmd.ToString())
    .Build();

await hyperfineResult.RunAsync();

WriteLine();

// Phase 3: Generate report
await GenerateReport();

return;

// === Helper Functions ===

async Task GenerateReport()
{
    WriteLine("=== Phase 3: Generating Report ===");
    WriteLine();

    string timestamp = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    string reportPath = Path.Combine(resultsDir, timestamp + "-aot-benchmark.md");
    StringBuilder report = new();

    report.AppendLine("# AOT CLI Framework Benchmark Results");
    report.AppendLine();
    report.AppendLine(CultureInfo.InvariantCulture, $"**Date:** {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
    report.AppendLine(CultureInfo.InvariantCulture, $"**Platform:** {Environment.OSVersion}");
    report.AppendLine(CultureInfo.InvariantCulture, $"**Runtime:** {Environment.Version}");
    report.AppendLine();

    // AOT Compatibility Table
    report.AppendLine("## AOT Compatibility");
    report.AppendLine();
    report.AppendLine("| Framework | AOT Support | Binary Size | Notes |");
    report.AppendLine("|-----------|-------------|-------------|-------|");

    foreach (string project in projects)
    {
        (bool success, string? error, long? size) = buildResults.GetValueOrDefault(project, (false, "Not built", null));
        string status = success ? "✓ Yes" : "✗ No";
        string sizeStr = size.HasValue ? FormatFileSize(size.Value) : "N/A";
        string notes = success ? "" : (error ?? "Build failed");
        report.AppendLine(CultureInfo.InvariantCulture, $"| {project} | {status} | {sizeStr} | {notes} |");
    }

    report.AppendLine();

    // Binary Size Ranking (successful builds only)
    List<KeyValuePair<string, (bool Success, string? Error, long? SizeBytes)>> successfulBuilds = [.. buildResults
        .Where(r => r.Value.Success && r.Value.SizeBytes.HasValue)
        .OrderBy(r => r.Value.SizeBytes!.Value)];

    if (successfulBuilds.Count > 0)
    {
        report.AppendLine("## Binary Size Ranking");
        report.AppendLine();
        report.AppendLine("| Rank | Framework | Size |");
        report.AppendLine("|------|-----------|------|");

        int rank = 1;
        foreach (KeyValuePair<string, (bool Success, string? Error, long? SizeBytes)> kvp in successfulBuilds)
        {
            report.AppendLine(CultureInfo.InvariantCulture, $"| {rank} | {kvp.Key} | {FormatFileSize(kvp.Value.SizeBytes!.Value)} |");
            rank++;
        }

        report.AppendLine();
    }

    // Include hyperfine results if available
    if (File.Exists(Path.Combine(resultsDir, "hyperfine-results.md")))
    {
        report.AppendLine("## Cold Start Performance (hyperfine)");
        report.AppendLine();
        string hyperfineMarkdown = await File.ReadAllTextAsync(Path.Combine(resultsDir, "hyperfine-results.md"));
        report.AppendLine(hyperfineMarkdown);
        report.AppendLine();
    }

    // Failed builds details
    List<KeyValuePair<string, (bool Success, string? Error, long? SizeBytes)>> failedBuilds = [.. buildResults
        .Where(r => !r.Value.Success)];

    if (failedBuilds.Count > 0)
    {
        report.AppendLine("## Failed Builds (AOT Not Supported)");
        report.AppendLine();
        foreach (KeyValuePair<string, (bool Success, string? Error, long? SizeBytes)> kvp in failedBuilds)
        {
            report.AppendLine(CultureInfo.InvariantCulture, $"### {kvp.Key}");
            report.AppendLine();
            report.AppendLine(CultureInfo.InvariantCulture, $"**Error:** {kvp.Value.Error ?? "Unknown error"}");
            report.AppendLine();
        }
    }

    // Summary
    report.AppendLine("## Summary");
    report.AppendLine();
    report.AppendLine(CultureInfo.InvariantCulture, $"- **Total frameworks tested:** {projects.Length}");
    report.AppendLine(CultureInfo.InvariantCulture, $"- **AOT compatible:** {buildResults.Count(r => r.Value.Success)}");
    report.AppendLine(CultureInfo.InvariantCulture, $"- **AOT incompatible:** {buildResults.Count(r => !r.Value.Success)}");
    report.AppendLine();

    await File.WriteAllTextAsync(reportPath, report.ToString());

    WriteLine($"  Report saved to: {reportPath}");
    WriteLine();

    // Print summary to console
    WriteLine("╔══════════════════════════════════════════════════════════════════╗");
    WriteLine("║                           Summary                                ║");
    WriteLine("╠══════════════════════════════════════════════════════════════════╣");
    WriteLine($"║  AOT Compatible:   {buildResults.Count(r => r.Value.Success),3} / {projects.Length,-3}                                     ║");
    WriteLine($"║  AOT Incompatible: {buildResults.Count(r => !r.Value.Success),3} / {projects.Length,-3}                                     ║");
    WriteLine("╚══════════════════════════════════════════════════════════════════╝");
}

static string FormatFileSize(long bytes)
{
    string[] suffixes = ["B", "KB", "MB", "GB"];
    int suffixIndex = 0;
    double size = bytes;

    while (size >= 1024 && suffixIndex < suffixes.Length - 1)
    {
        size /= 1024;
        suffixIndex++;
    }

    return string.Format(CultureInfo.InvariantCulture, "{0:F1} {1}", size, suffixes[suffixIndex]);
}
