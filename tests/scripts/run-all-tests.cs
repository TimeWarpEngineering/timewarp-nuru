#!/usr/bin/dotnet --
#:package TimeWarp.Amuru

using TimeWarp.Amuru;

// Get script directory, tests root, and repo root
// EntryPointFileDirectoryPath returns the scripts directory
string scriptsDir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? Environment.CurrentDirectory;
string testsRoot = Path.GetFullPath(Path.Combine(scriptsDir, ".."));
string repoRoot = Path.GetFullPath(Path.Combine(testsRoot, ".."));

// Parse command line arguments
bool verbose = args.Contains("--verbose");
bool skipStandalone = args.Contains("--skip-standalone");
bool skipAnalyzers = args.Contains("--skip-analyzers");
bool onlyStandalone = args.Contains("--only-standalone");

// Color codes for output
const string Reset = "\u001b[0m";
const string Green = "\u001b[32m";
const string Red = "\u001b[31m";
const string Yellow = "\u001b[33m";
const string Blue = "\u001b[34m";
const string Cyan = "\u001b[36m";
const string Bold = "\u001b[1m";

// Header
WriteLine($"{Blue}========================================{Reset}");
WriteLine($"{Blue}{Bold}TimeWarp.Nuru Test Suite{Reset}");
WriteLine($"{Blue}========================================{Reset}");
WriteLine();

if (verbose)
{
  WriteLine($"Tests root: {testsRoot}");
  WriteLine($"Repo root: {repoRoot}");
  WriteLine();
}

// Track overall results
int totalPassed = 0;
int totalFailed = 0;
int totalSkipped = 0;
Stopwatch overallStopwatch = Stopwatch.StartNew();

// ============================================================================
// PHASE 1: Run CI Multi-Mode Tests
// ============================================================================

if (!onlyStandalone)
{
  WriteLine($"{Cyan}Phase 1: CI Multi-Mode Tests{Reset}");
  WriteLine($"{Cyan}Running tests/ci-tests/run-ci-tests.cs...{Reset}");
  WriteLine();

  string ciTestRunner = Path.Combine(testsRoot, "ci-tests", "run-ci-tests.cs");

  CommandOutput ciResult = await Shell.Builder("dotnet")
    .WithArguments(ciTestRunner)
    .WithWorkingDirectory(repoRoot)
    .WithNoValidation()
    .CaptureAsync();

  // Parse results from output - look for the FINAL summary lines only
  // The CI runner outputs individual class results AND an overall summary
  // We only want the overall summary which appears at the very end after "Overall:"
  string[] ciLines = ciResult.Stdout.Split('\n');
  bool foundOverallSection = false;
  for (int i = 0; i < ciLines.Length; i++)
  {
    string line = ciLines[i];

    // Start parsing after we see "Overall:" marker
    if (line.Contains("Overall:"))
    {
      foundOverallSection = true;
      continue;
    }

    if (!foundOverallSection)
      continue;

    // Match lines like "[32mPassed:[0m 1687" (green Passed)
    if (line.Contains("Passed:") && line.Contains("[32m"))
    {
      string numPart = line.Split(':').Last().Trim();
      numPart = System.Text.RegularExpressions.Regex.Replace(numPart, @"\x1b\[[0-9;]*m", "");
      if (int.TryParse(numPart, out int passed))
        totalPassed += passed;
    }
    // Match lines like "[31mFailed:[0m 12" (red Failed)
    else if (line.Contains("Failed:") && line.Contains("[31m"))
    {
      string numPart = line.Split(':').Last().Trim();
      numPart = System.Text.RegularExpressions.Regex.Replace(numPart, @"\x1b\[[0-9;]*m", "");
      if (int.TryParse(numPart, out int failed))
        totalFailed += failed;
    }
    // Match lines like "[33mSkipped:[0m 3" (yellow Skipped)
    else if (line.Contains("Skipped:") && line.Contains("[33m"))
    {
      string numPart = line.Split(':').Last().Trim();
      numPart = System.Text.RegularExpressions.Regex.Replace(numPart, @"\x1b\[[0-9;]*m", "");
      if (int.TryParse(numPart, out int skipped))
        totalSkipped += skipped;
    }

    // Stop after Duration line
    if (line.Contains("Duration:"))
      break;
  }

  // Show output
  WriteLine(ciResult.Stdout);
  if (!string.IsNullOrEmpty(ciResult.Stderr))
  {
    WriteLine($"{Red}[STDERR]{Reset}");
    WriteLine(ciResult.Stderr);
  }

  WriteLine();
}

// ============================================================================
// PHASE 2: Run Standalone-Only Tests
// ============================================================================

if (!skipStandalone)
{
  WriteLine($"{Cyan}Phase 2: Standalone-Only Tests{Reset}");
  WriteLine($"{Cyan}Tests excluded from multi-mode due to conflicts...{Reset}");
  WriteLine();

  // These tests cannot run in multi-mode due to source generator conflicts
  // or unimplemented features
  string[] standaloneTests =
  [
    Path.Combine(testsRoot, "timewarp-nuru-core-tests", "routing", "routing-22-async-task-int-return.cs"),
    Path.Combine(testsRoot, "timewarp-nuru-core-tests", "options", "options-03-nuru-context.cs"),
  ];

  foreach (string testFile in standaloneTests)
  {
    if (!File.Exists(testFile))
    {
      WriteLine($"  {Yellow}⚠{Reset} {Path.GetFileName(testFile)} - NOT FOUND");
      continue;
    }

    Stopwatch sw = Stopwatch.StartNew();
    CommandOutput result = await Shell.Builder("dotnet")
      .WithArguments(testFile)
      .WithWorkingDirectory(Path.GetDirectoryName(testFile)!)
      .WithNoValidation()
      .CaptureAsync();
    sw.Stop();

    bool success = result.ExitCode == 0;
    string symbol = success ? $"{Green}✓{Reset}" : $"{Red}✗{Reset}";
    WriteLine($"  {symbol} {Path.GetFileName(testFile)} ({sw.ElapsedMilliseconds}ms)");

    if (success)
    {
      // Count tests from output - look for "[32mPassed:[0m N" line
      foreach (string line in result.Stdout.Split('\n'))
      {
        if (line.Contains("Passed:") && line.Contains("[32m"))
        {
          string numPart = line.Split(':').Last().Trim();
          numPart = System.Text.RegularExpressions.Regex.Replace(numPart, @"\x1b\[[0-9;]*m", "");
          if (int.TryParse(numPart, out int count))
            totalPassed += count;

          break;
        }
      }
    }
    else
    {
      totalFailed++;
      if (verbose)
      {
        WriteLine($"    {Red}Exit code: {result.ExitCode}{Reset}");
        if (!string.IsNullOrEmpty(result.Stderr))
        {
          foreach (string line in result.Stderr.Split('\n').Take(5))
            WriteLine($"    {line}");
        }
      }
    }
  }

  WriteLine();
}

// ============================================================================
// PHASE 3: Run Analyzer Tests (Optional)
// ============================================================================

if (!skipAnalyzers)
{
  WriteLine($"{Cyan}Phase 3: Analyzer Tests{Reset}");
  WriteLine($"{Cyan}Tests using Roslyn compilation API...{Reset}");
  WriteLine();

  string analyzerTestsDir = Path.Combine(testsRoot, "timewarp-nuru-analyzers-tests", "auto");

  if (Directory.Exists(analyzerTestsDir))
  {
    string[] analyzerTests = Directory.GetFiles(analyzerTestsDir, "*.cs");

    foreach (string testFile in analyzerTests)
    {
      Stopwatch sw = Stopwatch.StartNew();
      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments(testFile)
        .WithWorkingDirectory(Path.GetDirectoryName(testFile)!)
        .WithNoValidation()
        .CaptureAsync();
      sw.Stop();

      bool success = result.ExitCode == 0;
      string symbol = success ? $"{Green}✓{Reset}" : $"{Red}✗{Reset}";
      WriteLine($"  {symbol} {Path.GetFileName(testFile)} ({sw.ElapsedMilliseconds}ms)");

      if (success)
      {
        // Count tests from output - look for "[32mPassed:[0m N" line
        foreach (string line in result.Stdout.Split('\n'))
        {
          if (line.Contains("Passed:") && line.Contains("[32m"))
          {
            string numPart = line.Split(':').Last().Trim();
            numPart = System.Text.RegularExpressions.Regex.Replace(numPart, @"\x1b\[[0-9;]*m", "");
            if (int.TryParse(numPart, out int count))
              totalPassed += count;

            break;
          }
        }
      }
      else
      {
        totalFailed++;
        if (verbose)
        {
          WriteLine($"    {Red}Exit code: {result.ExitCode}{Reset}");
        }
      }
    }
  }
  else
  {
    WriteLine($"  {Yellow}⚠{Reset} Analyzer tests directory not found");
  }

  WriteLine();
}

// ============================================================================
// SUMMARY
// ============================================================================

overallStopwatch.Stop();

WriteLine($"{Blue}========================================{Reset}");
WriteLine($"{Blue}{Bold}OVERALL SUMMARY{Reset}");
WriteLine($"{Blue}========================================{Reset}");
WriteLine();

int totalTests = totalPassed + totalFailed + totalSkipped;
string statusColor = totalFailed == 0 ? Green : Red;
string status = totalFailed == 0 ? "ALL TESTS PASSED" : "TESTS FAILED";

WriteLine($"{Bold}Status:{Reset} {statusColor}{status}{Reset}");
WriteLine($"{Bold}Total Tests:{Reset} {totalTests}");
WriteLine($"{Green}Passed:{Reset} {totalPassed}");
if (totalFailed > 0)
  WriteLine($"{Red}Failed:{Reset} {totalFailed}");
if (totalSkipped > 0)
  WriteLine($"{Yellow}Skipped:{Reset} {totalSkipped}");
WriteLine($"{Bold}Duration:{Reset} {overallStopwatch.Elapsed.TotalSeconds:F1}s");

WriteLine();
WriteLine($"{Blue}========================================{Reset}");

return totalFailed > 0 ? 1 : 0;
