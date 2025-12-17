#!/usr/bin/dotnet --

// test.cs - Run the fast CI test suite
// Usage: dotnet runfiles/test.cs

using TimeWarp.Amuru;

// Get runfiles directory to build correct path to test runner
string runfilesDir = AppContext.GetData("EntryPointFileDirectoryPath") as string
  ?? throw new InvalidOperationException("Could not get entry point directory");

string repoRoot = Path.GetFullPath(Path.Combine(runfilesDir, ".."));
string testRunner = Path.Combine(repoRoot, "tests", "ci-tests", "run-ci-tests.cs");

if (!File.Exists(testRunner))
{
  await Console.Error.WriteLineAsync($"Test runner not found: {testRunner}");
  return 1;
}

// Run the CI test suite
int exitCode = await Shell.Builder("dotnet")
  .WithArguments(testRunner)
  .WithWorkingDirectory(repoRoot)
  .WithNoValidation()
  .RunAsync();

return exitCode;
