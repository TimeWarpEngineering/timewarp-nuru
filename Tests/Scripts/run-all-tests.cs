#!/usr/bin/dotnet --
#:property LangVersion=preview
#:property EnablePreviewFeatures=true

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

// Get script directory
string scriptDir = Path.GetDirectoryName(AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
// The script is in Tests/Scripts, but AppContext returns Tests, so testsRoot is scriptDir itself
string testsRoot = scriptDir;

// Parse command line arguments
bool parallel = args.Contains("--parallel");
bool verbose = args.Contains("--verbose");
bool stopOnFail = args.Contains("--stop-on-fail");
string? category = args.FirstOrDefault(a => a.StartsWith("--category=", StringComparison.Ordinal))?.Split('=')[1];

if (verbose)
{
    Console.WriteLine($"Script dir: {scriptDir}");
    Console.WriteLine($"Tests root: {testsRoot}");
}

// Color codes for output
const string Reset = "\u001b[0m";
const string Green = "\u001b[32m";
const string Red = "\u001b[31m";
const string Yellow = "\u001b[33m";
const string Blue = "\u001b[34m";
const string Cyan = "\u001b[36m";

// Header
Console.WriteLine($"{Blue}========================================{Reset}");
Console.WriteLine($"{Blue}TimeWarp.Nuru Test Suite{Reset}");
Console.WriteLine($"{Blue}========================================{Reset}");
Console.WriteLine($"Configuration: {(parallel ? "Parallel" : "Sequential")} | {(verbose ? "Verbose" : "Normal")}");
Console.WriteLine($"{Blue}========================================{Reset}");
Console.WriteLine();

// Discover test files
var testCategories = new Dictionary<string, List<string>>
{
    ["Lexer"] = GetTestFiles(Path.Combine(testsRoot, "SingleFileTests/Lexer")),
    ["Parser"] = GetTestFiles(Path.Combine(testsRoot, "SingleFileTests/Parser")),
    ["Routing"] = GetTestFiles(Path.Combine(testsRoot, "SingleFileTests/Routing")),
    ["Features"] = GetTestFiles(Path.Combine(testsRoot, "SingleFileTests/Features")),
    ["Options"] = GetTestFiles(Path.Combine(testsRoot, "SingleFileTests/test-matrix")),
    ["MCP"] = GetTestFiles(Path.Combine(testsRoot, "TimeWarp.Nuru.Mcp.Tests")),
};

// Filter categories if specified
if (category is not null)
{
    testCategories = testCategories
        .Where(kvp => kvp.Key.Equals(category, StringComparison.OrdinalIgnoreCase))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    if (testCategories.Count == 0)
    {
        Console.WriteLine($"{Red}Error: No tests found for category '{category}'{Reset}");
        Console.WriteLine($"Available categories: {string.Join(", ", testCategories.Keys)}");
        return 1;
    }
}

// Count total tests
int totalTests = testCategories.Sum(kvp => kvp.Value.Count);
Console.WriteLine($"Found {totalTests} tests in {testCategories.Count} categories");
Console.WriteLine();

// Run tests
var results = new ConcurrentBag<TestResult>();
var stopwatch = Stopwatch.StartNew();
bool shouldStop = false;

if (parallel && !stopOnFail)
{
    // Parallel execution
    var options = new ParallelOptions
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    Parallel.ForEach(testCategories, options, categoryKvp =>
    {
        if (!shouldStop)
        {
            RunCategoryTests(categoryKvp.Key, categoryKvp.Value, results, verbose, ref shouldStop, stopOnFail);
        }
    });
}
else
{
    // Sequential execution
    foreach (KeyValuePair<string, List<string>> categoryKvp in testCategories)
    {
        if (shouldStop) break;
        RunCategoryTests(categoryKvp.Key, categoryKvp.Value, results, verbose, ref shouldStop, stopOnFail);
    }
}

stopwatch.Stop();

// Generate summary
Console.WriteLine();
Console.WriteLine($"{Blue}========================================{Reset}");
Console.WriteLine($"{Blue}SUMMARY{Reset}");
Console.WriteLine($"{Blue}========================================{Reset}");

// Group results by category for summary
IOrderedEnumerable<IGrouping<string, TestResult>> resultsByCategory = results.GroupBy(r => r.Category).OrderBy(g => g.Key);

foreach (IGrouping<string, TestResult> categoryGroup in resultsByCategory)
{
    int passed = categoryGroup.Count(r => r.Success);
    int total = categoryGroup.Count();
    double percentage = total > 0 ? (passed * 100.0 / total) : 0;
    string color = passed == total ? Green : (passed > 0 ? Yellow : Red);

    Console.WriteLine($"{categoryGroup.Key,-15} {color}{passed}/{total} ({percentage:F1}%){Reset}");
}

Console.WriteLine();

// Overall summary
int totalPassed = results.Count(r => r.Success);
int totalFailed = results.Count(r => !r.Success);
double overallPercentage = totalTests > 0 ? (totalPassed * 100.0 / totalTests) : 0;

Console.WriteLine($"Total: {totalPassed}/{totalTests} tests passed ({overallPercentage:F1}%) in {stopwatch.Elapsed.TotalSeconds:F1}s");

// List failed tests
if (totalFailed > 0)
{
    Console.WriteLine();
    Console.WriteLine($"{Red}Failed Tests:{Reset}");
    foreach (TestResult failed in results.Where(r => !r.Success))
    {
        Console.WriteLine($"  {Red}✗{Reset} {failed.Category}/{Path.GetFileName(failed.TestFile)}");
        if (verbose && !string.IsNullOrEmpty(failed.Output))
        {
            IEnumerable<string> lines = failed.Output.Split('\n').Take(5);
            foreach (string line in lines)
            {
                Console.WriteLine($"    {line}");
            }
        }
    }
}

Console.WriteLine($"{Blue}========================================{Reset}");

return totalFailed > 0 ? 1 : 0;

// Helper functions

void RunCategoryTests(string category, List<string> testFiles, ConcurrentBag<TestResult> results, bool verbose, ref bool shouldStop, bool stopOnFail)
{
    Console.WriteLine($"{Cyan}Running {category} ({testFiles.Count} tests)...{Reset}");

    foreach (string testFile in testFiles)
    {
        if (shouldStop) break;

        TestResult result = RunTest(testFile, category, verbose);
        results.Add(result);

        string symbol = result.Success ? $"{Green}✓{Reset}" : $"{Red}✗{Reset}";
        string fileName = Path.GetFileName(testFile);
        Console.WriteLine($"  {symbol} {fileName,-40} ({result.Duration.TotalMilliseconds:F0}ms)");

        if (!result.Success && stopOnFail)
        {
            shouldStop = true;
            Console.WriteLine($"{Red}Stopping on first failure{Reset}");
            break;
        }
    }
}

TestResult RunTest(string testFile, string category, bool verbose)
{
    var stopwatch = Stopwatch.StartNew();
    var output = new StringBuilder();

    try
    {
        // Make test file executable if needed
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            Process.Start("chmod", $"+x {testFile}")?.WaitForExit(1000);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = testFile,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(testFile)
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return new TestResult(testFile, category, false, -1, stopwatch.Elapsed, "Failed to start process");
        }

        // Capture output
        var outputTask = Task.Run(() =>
        {
            string? line;
            while ((line = process.StandardOutput.ReadLine()) is not null)
            {
                output.AppendLine(line);
                if (verbose)
                {
                    Console.WriteLine($"    {line}");
                }
            }
        });

        var errorTask = Task.Run(() =>
        {
            string? line;
            while ((line = process.StandardError.ReadLine()) is not null)
            {
                output.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"[ERROR] {line}");
                if (verbose)
                {
                    Console.WriteLine($"    {Red}[ERROR]{Reset} {line}");
                }
            }
        });

        // Wait for completion with timeout
        bool completed = process.WaitForExit(30000); // 30 second timeout

        if (!completed)
        {
            process.Kill();
            return new TestResult(testFile, category, false, -1, stopwatch.Elapsed, "Test timeout (30s)");
        }

        Task.WaitAll(outputTask, errorTask);

        stopwatch.Stop();
        return new TestResult(testFile, category, process.ExitCode == 0, process.ExitCode, stopwatch.Elapsed, output.ToString());
    }
    catch (System.InvalidOperationException ex)
    {
        stopwatch.Stop();
        return new TestResult(testFile, category, false, -1, stopwatch.Elapsed, $"Exception: {ex.Message}");
    }
}

List<string> GetTestFiles(string directory)
{
    if (!Directory.Exists(directory))
    {
        Console.WriteLine($"Directory does not exist: {directory}");
        return [];
    }

    var files = Directory.GetFiles(directory, "test*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains("/obj/", StringComparison.Ordinal) && !f.Contains("/bin/", StringComparison.Ordinal) && !f.EndsWith(".csproj", StringComparison.Ordinal))
        .Order()
        .ToList();

    if (verbose && files.Count == 0)
    {
        Console.WriteLine($"No test files found in: {directory}");
        if (Directory.Exists(directory))
        {
            int allFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories).Length;
            Console.WriteLine($"  Total .cs files in directory: {allFiles}");
        }
    }

    return files;
}

// Test result tracking
sealed record TestResult(string TestFile, string Category, bool Success, int ExitCode, TimeSpan Duration, string Output);