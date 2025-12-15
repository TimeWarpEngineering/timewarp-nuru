#!/usr/bin/dotnet --
#:package TimeWarp.Amuru

using TimeWarp.Amuru;
using static TimeWarp.Amuru.Native.FileSystem.Direct;

// Get script directory
string scriptDir = Path.GetDirectoryName(AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
// The script is in tests/scripts, but AppContext returns tests, so testsRoot is scriptDir itself
string testsRoot = scriptDir;

// Parse command line arguments
bool parallel = args.Contains("--parallel");
bool verbose = args.Contains("--verbose");
bool stopOnFail = args.Contains("--stop-on-fail");

// Handle --category CategoryName format
string? category = null;
int categoryIndex = Array.IndexOf(args, "--category");
if (categoryIndex >= 0 && categoryIndex + 1 < args.Length)
{
  category = args[categoryIndex + 1];
}

if (verbose)
{
  WriteLine($"Parsed category: '{category}'");
  WriteLine($"Args: {string.Join(", ", args)}");
}

if (verbose)
{
  WriteLine($"Script dir: {scriptDir}");
  WriteLine($"Tests root: {testsRoot}");
}

// Color codes for output
const string Reset = "\u001b[0m";
const string Green = "\u001b[32m";
const string Red = "\u001b[31m";
const string Yellow = "\u001b[33m";
const string Blue = "\u001b[34m";
const string Cyan = "\u001b[36m";

// Header
WriteLine($"{Blue}========================================{Reset}");
WriteLine($"{Blue}TimeWarp.Nuru Test Suite{Reset}");
WriteLine($"{Blue}========================================{Reset}");

// Clear runfile cache (except for this currently running script) to ensure test scripts pick up latest source
string runfileCacheRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "dotnet", "runfile");
if (Directory.Exists(runfileCacheRoot))
{
  // Get the current executable's directory to avoid deleting ourselves
  string? currentExeDir = AppContext.BaseDirectory;

  WriteLine($"Current executable directory: {currentExeDir}");
  WriteLine("Runfile cache entries:");

  int deletedCount = 0;
  foreach (string cacheDir in Directory.GetDirectories(runfileCacheRoot))
  {
    // Don't delete the directory containing the currently running executable
    // currentExeDir will be something like: /path/runfile/script-hash/bin/debug/
    // cacheDir will be: /path/runfile/script-hash/
    // So check if currentExeDir STARTS WITH cacheDir
    if (currentExeDir?.StartsWith(cacheDir, StringComparison.OrdinalIgnoreCase) == true)
    {
      WriteLine($"  [SKIP] {Path.GetFileName(cacheDir)} (current executable)");
      continue;
    }

    try
    {
      WriteLine($"  [DELETE] {Path.GetFileName(cacheDir)}");
      RemoveItem(cacheDir, recursive: true);
      deletedCount++;
    }
    catch (Exception ex)
    {
      WriteLine($"  [ERROR] {Path.GetFileName(cacheDir)}: {ex.Message}");
    }
  }

  WriteLine($"✓ Cleared {deletedCount} cached test entries from runfile cache");
}

WriteLine();
WriteLine($"Configuration: {(parallel ? "Parallel" : "Sequential")} | {(verbose ? "Verbose" : "Normal")}");
WriteLine($"{Blue}========================================{Reset}");
WriteLine();

// Discover test files
Dictionary<string, List<string>> testCategories = new()
{
  ["Analyzers"] = [.. GetTestFiles(Path.Combine(testsRoot, "timewarp-nuru-analyzers-tests"))],
  ["Core"] = [.. GetTestFiles(Path.Combine(testsRoot, "timewarp-nuru-core-tests"))],
  ["Factory"] = [.. GetTestFiles(Path.Combine(testsRoot, "timewarp-nuru-tests/factory"))],
  ["Lexer"] = [.. GetTestFiles(Path.Combine(testsRoot, "timewarp-nuru-tests/lexer"))],
  ["Parser"] = [.. GetTestFiles(Path.Combine(testsRoot, "timewarp-nuru-tests/parsing/parser"))],
  ["Routing"] = [.. GetTestFiles(Path.Combine(testsRoot, "timewarp-nuru-tests/routing"))],
  ["TypeConversion"] = [.. GetTestFiles(Path.Combine(testsRoot, "timewarp-nuru-tests/type-conversion"))],
  ["MCP"] = GetTestFiles(Path.Combine(testsRoot, "timewarp-nuru-mcp-tests")),
  ["Completion"] = [.. GetTestFiles(Path.Combine(testsRoot, "timewarp-nuru-completion-tests/static")),
                      .. GetTestFiles(Path.Combine(testsRoot, "timewarp-nuru-completion-tests/dynamic"))],
  ["Repl"] = [.. GetTestFiles(Path.Combine(testsRoot, "timewarp-nuru-repl-tests"))],
};

// Filter categories if specified
if (category is not null)
{
  List<string> originalCategories = [.. testCategories.Keys];
  testCategories = testCategories
      .Where(kvp => kvp.Key.Equals(category, StringComparison.OrdinalIgnoreCase))
      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

  if (testCategories.Count == 0)
  {
    WriteLine($"{Red}Error: No tests found for category '{category}'{Reset}");
    WriteLine($"Available categories: {string.Join(", ", originalCategories)}");
    return 1;
  }
}

// Count total tests
int totalTests = testCategories.Sum(kvp => kvp.Value.Count);
WriteLine($"Found {totalTests} tests in {testCategories.Count} categories");

if (verbose)
{
  foreach (KeyValuePair<string, List<string>> kvp in testCategories)
  {
    WriteLine($"  {kvp.Key}: {kvp.Value.Count} tests");
  }
}

WriteLine();

// Run tests
ConcurrentBag<TestResult> results = [];
Stopwatch stopwatch = Stopwatch.StartNew();

if (parallel && !stopOnFail)
{
  // Parallel execution
  List<Task> tasks = [];
  foreach (KeyValuePair<string, List<string>> categoryKvp in testCategories)
  {
    tasks.Add(RunCategoryTests(categoryKvp.Key, categoryKvp.Value, results, verbose, stopOnFail));
  }

  await Task.WhenAll(tasks);
}
else
{
  // Sequential execution
  foreach (KeyValuePair<string, List<string>> categoryKvp in testCategories)
  {
    await RunCategoryTests(categoryKvp.Key, categoryKvp.Value, results, verbose, stopOnFail);
  }
}

stopwatch.Stop();

// Generate summary
WriteLine();
WriteLine($"{Blue}========================================{Reset}");
WriteLine($"{Blue}SUMMARY{Reset}");
WriteLine($"{Blue}========================================{Reset}");

// Group results by category for summary
IOrderedEnumerable<IGrouping<string, TestResult>> resultsByCategory = results.GroupBy(r => r.Category).OrderBy(g => g.Key);

foreach (IGrouping<string, TestResult> categoryGroup in resultsByCategory)
{
  int passed = categoryGroup.Count(r => r.Success);
  int total = categoryGroup.Count();
  double percentage = total > 0 ? (passed * 100.0 / total) : 0;
  string color = passed == total ? Green : (passed > 0 ? Yellow : Red);

  WriteLine($"{categoryGroup.Key,-15} {color}{passed}/{total} ({percentage:F1}%){Reset}");
}

WriteLine();

// Overall summary
int totalPassed = results.Count(r => r.Success);
int totalFailed = results.Count(r => !r.Success);
double overallPercentage = totalTests > 0 ? (totalPassed * 100.0 / totalTests) : 0;

WriteLine($"Total: {totalPassed}/{totalTests} tests passed ({overallPercentage:F1}%) in {stopwatch.Elapsed.TotalSeconds:F1}s");

// List failed tests
if (totalFailed > 0)
{
  WriteLine();

  // Separate compilation errors from test failures
  List<TestResult> compilationErrors = [.. results.Where(r => r.ExitCode == -999)];
  List<TestResult> testFailures = [.. results.Where(r => !r.Success && r.ExitCode != -999)];

  if (compilationErrors.Count > 0)
  {
    WriteLine($"{Yellow}Compilation Errors:{Reset}");
    foreach (TestResult failed in compilationErrors)
    {
      WriteLine($"  {Yellow}⚠{Reset} {failed.Category}/{Path.GetFileName(failed.TestFile)}");
    }

    WriteLine();
  }

  if (testFailures.Count > 0)
  {
    WriteLine($"{Red}Failed Tests:{Reset}");
    foreach (TestResult failed in testFailures)
    {
      WriteLine($"  {Red}✗{Reset} {failed.Category}/{Path.GetFileName(failed.TestFile)}");
      if (verbose && !string.IsNullOrEmpty(failed.Output))
      {
        IEnumerable<string> lines = failed.Output.Split('\n').Take(5);
        foreach (string line in lines)
        {
          WriteLine($"    {line}");
        }
      }
    }
  }
}

WriteLine($"{Blue}========================================{Reset}");

return totalFailed > 0 ? 1 : 0;

// Helper functions

async Task RunCategoryTests(string category, List<string> testFiles, ConcurrentBag<TestResult> results, bool verbose, bool stopOnFail)
{
  WriteLine($"{Cyan}Running {category} ({testFiles.Count} tests)...{Reset}");

  foreach (string testFile in testFiles)
  {

    TestResult result = await RunTest(testFile, category, verbose);
    results.Add(result);

    string symbol = result.Success ? $"{Green}✓{Reset}" :
                   result.ExitCode == -999 ? $"{Yellow}⚠{Reset}" : $"{Red}✗{Reset}";
    string status = result.ExitCode == -999 ? "NO COMPILE" : "";
    string fileName = Path.GetFileName(testFile);
    WriteLine($"  {symbol} {fileName,-40} ({result.Duration.TotalMilliseconds:F0}ms) {status}");

    if (!result.Success && stopOnFail)
    {
      WriteLine($"{Red}Stopping on first failure{Reset}");
      break;
    }
  }
}

async Task<TestResult> RunTest(string testFile, string category, bool verbose)
{
  Stopwatch sw = Stopwatch.StartNew();

  try
  {
    // Make test file executable if needed
    if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
    {
      await Shell.Builder("chmod").WithArguments("+x", testFile).RunAsync();
    }

    // Run the test using Amuru
    CommandOutput result = await Shell.Builder(testFile)
        .WithWorkingDirectory(Path.GetDirectoryName(testFile)!)
        .WithNoValidation()
        .CaptureAsync();

    sw.Stop();

    // Check if it's a compilation error
    bool isCompilationError = result.Stderr.Contains("build failed", StringComparison.OrdinalIgnoreCase) ||
                             result.Stderr.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase) ||
                             result.Stdout.Contains("build failed", StringComparison.OrdinalIgnoreCase) ||
                             result.Stdout.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase);

    if (verbose)
    {
      if (!string.IsNullOrEmpty(result.Stdout))
      {
        foreach (string line in result.Stdout.Split('\n'))
        {
          WriteLine($"    {line}");
        }
      }

      if (!string.IsNullOrEmpty(result.Stderr))
      {
        foreach (string line in result.Stderr.Split('\n'))
        {
          WriteLine($"    {Red}[ERROR]{Reset} {line}");
        }
      }
    }

    string output = result.Stdout;
    if (!string.IsNullOrEmpty(result.Stderr))
    {
      output += Environment.NewLine + "[STDERR]" + Environment.NewLine + result.Stderr;
    }

    // Use -999 as exit code for compilation errors
    int exitCode = isCompilationError ? -999 : result.ExitCode;
    bool success = result.Success && !isCompilationError;

    return new TestResult(testFile, category, success, exitCode, sw.Elapsed, output);
  }
  catch (TimeoutException)
  {
    sw.Stop();
    return new TestResult(testFile, category, false, -1, sw.Elapsed, "Test timeout (30s)");
  }
  catch (InvalidOperationException ex)
  {
    sw.Stop();
    return new TestResult(testFile, category, false, -1, sw.Elapsed, $"Exception: {ex.Message}");
  }
}

List<string> GetTestFiles(string directory)
{
  if (!Directory.Exists(directory))
  {
    WriteLine($"Directory does not exist: {directory}");
    return [];
  }

  List<string> files =
  [
    .. Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
      .Where(f => !f.Contains("/obj/", StringComparison.Ordinal) &&
                  !f.Contains("/bin/", StringComparison.Ordinal) &&
                  !f.EndsWith(".csproj", StringComparison.Ordinal) &&
                  !Path.GetFileName(f).StartsWith("run-", StringComparison.Ordinal) && // Exclude test runners
                  !Path.GetFileName(f).Contains("helper", StringComparison.OrdinalIgnoreCase)) // Exclude helper files
      .Order()
  ];

  if (verbose && files.Count == 0)
  {
    WriteLine($"No test files found in: {directory}");
    if (Directory.Exists(directory))
    {
      int allFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories).Length;
      WriteLine($"  Total .cs files in directory: {allFiles}");
    }
  }

  return files;
}

// Test result tracking
sealed record TestResult(string TestFile, string Category, bool Success, int ExitCode, TimeSpan Duration, string Output);