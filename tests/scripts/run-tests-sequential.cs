#!/usr/bin/dotnet --
#:package TimeWarp.Amuru

// run-tests-sequential.cs - Diagnostic runner to find compile failures and hanging tests
//
// This script runs each test file individually with timeouts to identify:
// 1. Tests that fail to compile
// 2. Tests that hang (timeout during run)
// 3. Tests that fail during execution
//
// Usage:
//   dotnet tests/scripts/run-tests-sequential.cs              # Use current UseNewGen value
//   dotnet tests/scripts/run-tests-sequential.cs --v1         # Set UseNewGen=false (V1 generators)
//   dotnet tests/scripts/run-tests-sequential.cs --v2         # Set UseNewGen=true (V2 generators)
//
// Results are written to tests/scripts/test-results.md

using System.Diagnostics;
using System.Text.RegularExpressions;
using TimeWarp.Amuru;

// Constants
const int CompileTimeoutSeconds = 60;
const int RunTimeoutSeconds = 30;
const string Shebang = "#!/usr/bin/dotnet";

// Color codes
const string Reset = "\u001b[0m";
const string Green = "\u001b[32m";
const string Red = "\u001b[31m";
const string Yellow = "\u001b[33m";
const string Blue = "\u001b[34m";
const string Cyan = "\u001b[36m";
const string Bold = "\u001b[1m";

// Get paths
string scriptsDir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? Environment.CurrentDirectory;
string testsRoot = Path.GetFullPath(Path.Combine(scriptsDir, ".."));
string repoRoot = Path.GetFullPath(Path.Combine(testsRoot, ".."));
string directoryBuildProps = Path.Combine(repoRoot, "Directory.Build.props");

// Parse command line args
bool setV1 = args.Contains("--v1");
bool setV2 = args.Contains("--v2");

if (setV1 && setV2)
{
  WriteLine($"{Red}ERROR: Cannot specify both --v1 and --v2{Reset}");
  return 1;
}

// Set UseNewGen in Directory.Build.props if requested
if (setV1 || setV2)
{
  string targetValue = setV1 ? "false" : "true";
  string generatorName = setV1 ? "V1" : "V2";

  WriteLine($"{Cyan}Setting UseNewGen={targetValue} ({generatorName} generators)...{Reset}");

  if (!File.Exists(directoryBuildProps))
  {
    WriteLine($"{Red}ERROR: Directory.Build.props not found at {directoryBuildProps}{Reset}");
    return 1;
  }

  string content = await File.ReadAllTextAsync(directoryBuildProps);

  // Check if element exists
  if (!Regex.IsMatch(content, @"<UseNewGen>\w+</UseNewGen>"))
  {
    WriteLine($"{Red}ERROR: Could not find <UseNewGen> element in Directory.Build.props{Reset}");
    return 1;
  }

  string updated = Regex.Replace(content, @"<UseNewGen>\w+</UseNewGen>", $"<UseNewGen>{targetValue}</UseNewGen>");

  if (updated != content)
  {
    await File.WriteAllTextAsync(directoryBuildProps, updated);
    WriteLine($"{Green}Updated Directory.Build.props{Reset}");
  }
  else
  {
    WriteLine($"{Green}UseNewGen already set to {targetValue}{Reset}");
  }

  WriteLine();
}

// Read UseNewGen value from Directory.Build.props
string useNewGenValue = "unknown";
if (File.Exists(directoryBuildProps))
{
  string content = await File.ReadAllTextAsync(directoryBuildProps);
  Match match = Regex.Match(content, @"<UseNewGen>(\w+)</UseNewGen>");
  if (match.Success)
    useNewGenValue = match.Groups[1].Value;
}

string generatorLabel = useNewGenValue.Equals("true", StringComparison.OrdinalIgnoreCase) ? "V2" : "V1";
string resultsFile = Path.Combine(scriptsDir, $"test-results-{generatorLabel.ToLowerInvariant()}.md");

WriteLine($"{Bold}Using {generatorLabel} generators (UseNewGen={useNewGenValue}){Reset}");
WriteLine();

// Clear runfile cache
WriteLine($"{Cyan}Clearing runfile cache...{Reset}");
await Shell.Builder("ganda")
  .WithArguments("runfile", "cache", "--clear")
  .WithWorkingDirectory(repoRoot)
  .WithNoValidation()
  .RunAsync();
WriteLine();

// Initialize results file
DateTime startTime = DateTime.Now;
await File.WriteAllTextAsync(resultsFile, $"""
# Sequential Test Run Results

**Started:** {startTime:yyyy-MM-dd HH:mm:ss}
**UseNewGen:** {useNewGenValue} ({generatorLabel} generators)

## Compile Phase


""");

// Glob patterns in order matching Directory.Build.props
string[][] globPatterns =
[
  ["timewarp-nuru-core-tests/lexer", "*.cs"],
  ["timewarp-nuru-core-tests", "ansi-string-utils-*.cs"],
  ["timewarp-nuru-core-tests", "help-provider-*.cs"],
  ["timewarp-nuru-core-tests", "hyperlink-*.cs"],
  ["timewarp-nuru-core-tests", "panel-widget-*.cs"],
  ["timewarp-nuru-core-tests", "rule-widget-*.cs"],
  ["timewarp-nuru-core-tests", "table-widget-*.cs"],
  ["timewarp-nuru-core-tests", "invoker-registry-*.cs"],
  ["timewarp-nuru-core-tests", "message-type-*.cs"],
  ["timewarp-nuru-core-tests", "nuru-route-registry-*.cs"],
  ["timewarp-nuru-core-tests", "test-terminal-context-*.cs"],
  ["timewarp-nuru-core-tests/parser", "*.cs"],
  ["timewarp-nuru-core-tests/routing", "*.cs"],
  ["timewarp-nuru-core-tests/configuration", "*.cs"],
  ["timewarp-nuru-core-tests/options", "*.cs"],
  ["timewarp-nuru-core-tests/type-conversion", "*.cs"],
  ["timewarp-nuru-completion-tests/static", "*.cs"],
  ["timewarp-nuru-completion-tests/dynamic", "*.cs"],
  ["timewarp-nuru-completion-tests/engine", "*.cs"],
  ["timewarp-nuru-repl-tests", "*.cs"],
  ["timewarp-nuru-repl-tests/command-line-parser", "*.cs"],
  ["timewarp-nuru-mcp-tests", "*.cs"],
];

// Discover test files
List<string> testFiles = [];
foreach (string[] pattern in globPatterns)
{
  string dir = Path.Combine(testsRoot, pattern[0]);
  if (!Directory.Exists(dir))
    continue;

  string[] files = Directory.GetFiles(dir, pattern[1]);
  Array.Sort(files);
  foreach (string file in files)
  {
    // Skip obj/bin directories
    if (file.Contains("/obj/") || file.Contains("/bin/") ||
        file.Contains("\\obj\\") || file.Contains("\\bin\\"))
      continue;

    // Check for shebang
    try
    {
      await using FileStream fs = new(file, FileMode.Open, FileAccess.Read);
      using StreamReader reader = new(fs);
      string? firstLine = await reader.ReadLineAsync();
      if (firstLine?.StartsWith(Shebang, StringComparison.Ordinal) != true)
        continue;
    }
    catch
    {
      continue;
    }

    testFiles.Add(file);
  }
}

WriteLine($"{Bold}Found {testFiles.Count} test files{Reset}");
WriteLine();

// Track results
int compileSuccess = 0;
int compileFailed = 0;
int compileTimeout = 0;
int runSuccess = 0;
int runFailed = 0;
int runTimeout = 0;
List<string> compiledFiles = [];
List<string> compileFailures = [];
List<string> runFailures = [];
List<string> runTimeouts = [];

// ============================================================================
// COMPILE PHASE
// ============================================================================

WriteLine($"{Blue}========================================{Reset}");
WriteLine($"{Blue}{Bold}COMPILE PHASE{Reset}");
WriteLine($"{Blue}========================================{Reset}");
WriteLine();

foreach (string testFile in testFiles)
{
  string relativePath = Path.GetRelativePath(repoRoot, testFile);

  Write($"COMPILING: {relativePath} ... ");
  await Console.Out.FlushAsync();

  Stopwatch sw = Stopwatch.StartNew();

  try
  {
    Task<CommandOutput> buildTask = Shell.Builder("dotnet")
      .WithArguments("build", testFile)
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .CaptureAsync();

    Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(CompileTimeoutSeconds));
    Task completedTask = await Task.WhenAny(buildTask, timeoutTask);

    sw.Stop();

    if (completedTask == timeoutTask)
    {
      // Timeout occurred
      WriteLine($"{Yellow}⏱ TIMEOUT{Reset} (killed after {CompileTimeoutSeconds}s)");
      await File.AppendAllTextAsync(resultsFile, $"⏱ COMPILE TIMEOUT: {relativePath} (killed after {CompileTimeoutSeconds}s)\n");
      compileTimeout++;
      compileFailures.Add($"{relativePath} (timeout)");

      // Kill any hanging dotnet processes for this file
      try
      {
        await Shell.Builder("pkill")
          .WithArguments("-f", Path.GetFileName(testFile))
          .WithNoValidation()
          .RunAsync();
      }
      catch { /* ignore */ }
    }
    else
    {
      // Build completed
      CommandOutput result = await buildTask;

      if (result.ExitCode == 0)
      {
        WriteLine($"{Green}✓{Reset} ({sw.Elapsed.TotalSeconds:F1}s)");
        await File.AppendAllTextAsync(resultsFile, $"✓ {relativePath} ({sw.Elapsed.TotalSeconds:F1}s)\n");
        compileSuccess++;
        compiledFiles.Add(testFile);
      }
      else
      {
        WriteLine($"{Red}✗ FAIL{Reset} ({sw.Elapsed.TotalSeconds:F1}s)");
        string errorMsg = result.Stderr.Length > 200 ? result.Stderr[..200] + "..." : result.Stderr;
        await File.AppendAllTextAsync(resultsFile, $"✗ COMPILE FAIL: {relativePath} - {errorMsg.Replace("\n", " ")}\n");
        compileFailed++;
        compileFailures.Add(relativePath);
      }
    }
  }
  catch (Exception ex)
  {
    sw.Stop();
    WriteLine($"{Red}✗ ERROR: {ex.Message}{Reset}");
    await File.AppendAllTextAsync(resultsFile, $"✗ COMPILE ERROR: {relativePath} - {ex.Message}\n");
    compileFailed++;
    compileFailures.Add(relativePath);
  }
}

WriteLine();
WriteLine($"Compile: {Green}{compileSuccess} success{Reset}, {Red}{compileFailed} failed{Reset}, {Yellow}{compileTimeout} timeout{Reset}");
WriteLine();

// ============================================================================
// RUN PHASE
// ============================================================================

await File.AppendAllTextAsync(resultsFile, """

## Run Phase


""");

WriteLine($"{Blue}========================================{Reset}");
WriteLine($"{Blue}{Bold}RUN PHASE{Reset}");
WriteLine($"{Blue}========================================{Reset}");
WriteLine();

foreach (string testFile in compiledFiles)
{
  string relativePath = Path.GetRelativePath(repoRoot, testFile);

  Write($"RUNNING: {relativePath} ... ");
  await Console.Out.FlushAsync();

  Stopwatch sw = Stopwatch.StartNew();

  try
  {
    Task<CommandOutput> runTask = Shell.Builder("dotnet")
      .WithArguments(testFile)
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .CaptureAsync();

    Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(RunTimeoutSeconds));
    Task completedTask = await Task.WhenAny(runTask, timeoutTask);

    sw.Stop();

    if (completedTask == timeoutTask)
    {
      // Timeout occurred - likely a hang
      WriteLine($"{Yellow}⏱ TIMEOUT - LIKELY HANG{Reset} (killed after {RunTimeoutSeconds}s)");
      await File.AppendAllTextAsync(resultsFile, $"⏱ RUN TIMEOUT: {relativePath} (killed after {RunTimeoutSeconds}s) ← LIKELY HANG\n");
      runTimeout++;
      runTimeouts.Add(relativePath);

      // Kill any hanging dotnet processes for this file
      try
      {
        await Shell.Builder("pkill")
          .WithArguments("-f", Path.GetFileName(testFile))
          .WithNoValidation()
          .RunAsync();
      }
      catch { /* ignore */ }
    }
    else
    {
      // Run completed
      CommandOutput result = await runTask;

      if (result.ExitCode == 0)
      {
        WriteLine($"{Green}✓{Reset} ({sw.Elapsed.TotalSeconds:F1}s)");
        await File.AppendAllTextAsync(resultsFile, $"✓ {relativePath} ({sw.Elapsed.TotalSeconds:F1}s)\n");
        runSuccess++;
      }
      else
      {
        WriteLine($"{Red}✗ FAIL (exit {result.ExitCode}){Reset} ({sw.Elapsed.TotalSeconds:F1}s)");
        await File.AppendAllTextAsync(resultsFile, $"✗ RUN FAIL: {relativePath} (exit code {result.ExitCode})\n");
        runFailed++;
        runFailures.Add(relativePath);
      }
    }
  }
  catch (Exception ex)
  {
    sw.Stop();
    WriteLine($"{Red}✗ ERROR: {ex.Message}{Reset}");
    await File.AppendAllTextAsync(resultsFile, $"✗ RUN ERROR: {relativePath} - {ex.Message}\n");
    runFailed++;
    runFailures.Add(relativePath);
  }
}

// ============================================================================
// SUMMARY
// ============================================================================

DateTime endTime = DateTime.Now;
TimeSpan duration = endTime - startTime;

string summary = $"""

## Summary

| Phase   | Success | Failed | Timeout |
|---------|---------|--------|---------|
| Compile | {compileSuccess} | {compileFailed} | {compileTimeout} |
| Run     | {runSuccess} | {runFailed} | {runTimeout} |

""";

if (compileFailures.Count > 0)
{
  summary += "\n### Compile Failures\n\n";
  foreach (string f in compileFailures)
    summary += $"- {f}\n";
}

if (runFailures.Count > 0)
{
  summary += "\n### Run Failures\n\n";
  foreach (string f in runFailures)
    summary += $"- {f}\n";
}

if (runTimeouts.Count > 0)
{
  summary += "\n### Run Timeouts (Likely Hangs)\n\n";
  foreach (string f in runTimeouts)
    summary += $"- **{f}**\n";
}

summary += $"""

**Completed:** {endTime:yyyy-MM-dd HH:mm:ss}
**Duration:** {duration.TotalMinutes:F1} minutes
""";

await File.AppendAllTextAsync(resultsFile, summary);

WriteLine();
WriteLine($"{Blue}========================================{Reset}");
WriteLine($"{Blue}{Bold}SUMMARY{Reset}");
WriteLine($"{Blue}========================================{Reset}");
WriteLine();
WriteLine($"Compile: {Green}{compileSuccess} success{Reset}, {Red}{compileFailed} failed{Reset}, {Yellow}{compileTimeout} timeout{Reset}");
WriteLine($"Run:     {Green}{runSuccess} success{Reset}, {Red}{runFailed} failed{Reset}, {Yellow}{runTimeout} timeout{Reset}");
WriteLine();

if (runTimeouts.Count > 0)
{
  WriteLine($"{Red}{Bold}HANGING TESTS FOUND:{Reset}");
  foreach (string f in runTimeouts)
    WriteLine($"  - {f}");
  WriteLine();
}

WriteLine($"Results written to: {resultsFile}");
WriteLine($"Duration: {duration.TotalMinutes:F1} minutes");

return (compileFailed + runFailed + runTimeout) > 0 ? 1 : 0;
