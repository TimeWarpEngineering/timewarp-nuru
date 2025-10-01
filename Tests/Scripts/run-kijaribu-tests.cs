#!/usr/bin/dotnet --
#:property LangVersion=preview
#:property EnablePreviewFeatures=true
#:project ../../Source/TimeWarp.Kijaribu/TimeWarp.Kijaribu.csproj
#:package TimeWarp.Amuru

using static System.Console;
using static TimeWarp.Amuru.Native.FileSystem.Direct;

// Run all Kijaribu-based tests
WriteLine("🧪 Running Kijaribu-based Parser Tests...");
WriteLine();

// Clear runfile cache (except for this currently running script)
string runfileCacheRoot = Path.Combine(
  Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
  ".local", "share", "dotnet", "runfile"
);

if (Directory.Exists(runfileCacheRoot))
{
  string? currentExeDir = AppContext.BaseDirectory;
  WriteLine("Clearing runfile cache (except current executable)...");

  int deletedCount = 0;
  foreach (string cacheDir in Directory.GetDirectories(runfileCacheRoot))
  {
    // Don't delete if currentExeDir STARTS WITH cacheDir (parent-child relationship)
    if (currentExeDir?.StartsWith(cacheDir, StringComparison.OrdinalIgnoreCase) == true)
    {
      WriteLine($"  [SKIP] {Path.GetFileName(cacheDir)} (current executable)");
      continue;
    }

    try
    {
      RemoveItem(cacheDir, recursive: true);
      deletedCount++;
    }
    catch (Exception ex)
    {
      WriteLine($"  [ERROR] {Path.GetFileName(cacheDir)}: {ex.Message}");
    }
  }

  WriteLine($"✓ Cleared {deletedCount} cached test entries");
  WriteLine();
}

// Track overall results
int totalTests = 0;
int passedTests = 0;

// List of Kijaribu-based test files (relative to current working directory)
string[] testFiles = [
  "Tests/TimeWarp.Nuru.Tests/Parsing/Parser/test-catchall-validation.cs",
];

foreach (string testFile in testFiles)
{
  string fullPath = Path.GetFullPath(testFile);
  if (!File.Exists(fullPath))
  {
    WriteLine($"⚠ Test file not found: {testFile}");
    continue;
  }

  totalTests++;
  WriteLine($"Running: {Path.GetFileName(testFile)}");

  // Make test file executable if needed
  if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
  {
    await TimeWarp.Amuru.Shell.Builder("chmod").WithArguments("+x", fullPath).RunAsync();
  }

  TimeWarp.Amuru.CommandOutput result = await TimeWarp.Amuru.Shell.Builder(fullPath)
    .WithWorkingDirectory(Path.GetDirectoryName(fullPath)!)
    .WithNoValidation()
    .CaptureAsync();

  if (result.Success)
  {
    passedTests++;
    WriteLine("✅ PASSED");
  }
  else
  {
    WriteLine("❌ FAILED");
    if (!string.IsNullOrWhiteSpace(result.Stdout))
      WriteLine(result.Stdout);
    if (!string.IsNullOrWhiteSpace(result.Stderr))
      WriteLine($"Stderr: {result.Stderr}");
  }

  WriteLine();
}

// Summary
WriteLine($"{'═',-60}");
WriteLine($"Results: {passedTests}/{totalTests} test files passed");
WriteLine($"{'═',-60}");

Environment.Exit(passedTests == totalTests ? 0 : 1);
