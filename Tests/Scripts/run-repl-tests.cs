#!/usr/bin/dotnet --
#:property TreatWarningsAsErrors=false

// REPL Test Runner
// Executes all REPL tests and reports aggregate results

using System.Diagnostics;

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

string testsDir = Path.GetFullPath(Path.Combine(scriptDir, "..", "TimeWarp.Nuru.Repl.Tests"));

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║           TimeWarp.Nuru REPL Test Runner                     ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Find all REPL test files
IOrderedEnumerable<string> testFiles = Directory.GetFiles(testsDir, "repl-*.cs", SearchOption.TopDirectoryOnly)
  .OrderBy(f => Path.GetFileName(f));

IOrderedEnumerable<string> parserTestFiles = Directory.GetFiles(Path.Combine(testsDir, "CommandLineParser"), "parser-*.cs", SearchOption.TopDirectoryOnly)
  .OrderBy(f => Path.GetFileName(f));

List<string> allTestFiles = [.. parserTestFiles, .. testFiles];

Console.WriteLine($"Found {allTestFiles.Count} test files");
Console.WriteLine();

int totalPassed = 0;
int totalFailed = 0;
int totalSkipped = 0;
List<string> failedTests = [];

foreach (string testFile in allTestFiles)
{
  string fileName = Path.GetFileName(testFile);
  Console.Write($"Running {fileName}... ");

  try
  {
    var psi = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"run \"{testFile}\"",
      WorkingDirectory = Path.GetDirectoryName(testFile),
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using var process = Process.Start(psi);
    if (process is null)
    {
      Console.WriteLine("SKIP (could not start process)");
      totalSkipped++;
      continue;
    }

    string output = await process.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    if (process.ExitCode == 0)
    {
      // Parse output for pass/fail counts
      System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(output, @"(\d+) passed, (\d+) failed");
      if (match.Success)
      {
        int passed = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        int failed = int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        totalPassed += passed;
        totalFailed += failed;

        if (failed > 0)
        {
          Console.WriteLine($"PARTIAL ({passed} passed, {failed} failed)");
          failedTests.Add($"{fileName}: {failed} failed");
        }
        else
        {
          Console.WriteLine($"OK ({passed} passed)");
        }
      }
      else
      {
        Console.WriteLine("OK");
        totalPassed++;
      }
    }
    else
    {
      Console.WriteLine($"FAIL (exit code {process.ExitCode})");
      totalFailed++;
      failedTests.Add($"{fileName}: Exit code {process.ExitCode}");

      // Show error output
      if (!string.IsNullOrWhiteSpace(error))
      {
        Console.WriteLine($"  Error: {error.Split('\n').FirstOrDefault()}");
      }
    }
  }
  catch (Exception ex)
  {
    Console.WriteLine($"ERROR ({ex.Message})");
    totalFailed++;
    failedTests.Add($"{fileName}: {ex.Message}");
  }
}

Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════════════════════════");
Console.WriteLine($"Total: {totalPassed} passed, {totalFailed} failed, {totalSkipped} skipped");
Console.WriteLine("══════════════════════════════════════════════════════════════");

if (failedTests.Count > 0)
{
  Console.WriteLine();
  Console.WriteLine("Failed tests:");
  foreach (string test in failedTests)
  {
    Console.WriteLine($"  ✗ {test}");
  }
}

return totalFailed > 0 ? 1 : 0;
