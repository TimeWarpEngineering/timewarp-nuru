#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru

using TimeWarp.Amuru;

// Legacy entry point — delegates to the official CI runner (task 470-012 / M27).
// Prefer: dotnet run tests/ci-tests/run-ci-tests.cs

string scriptsDir = AppContext.GetData("EntryPointFileDirectoryPath") as string
  ?? Environment.CurrentDirectory;
string testsRoot = Path.GetFullPath(Path.Combine(scriptsDir, ".."));
string repoRoot = Path.GetFullPath(Path.Combine(testsRoot, ".."));
string ciTestRunner = Path.Combine(testsRoot, "ci-tests", "run-ci-tests.cs");

WriteLine("Delegating to tests/ci-tests/run-ci-tests.cs (official CI runner)...");
WriteLine();

return await Shell.Builder("dotnet")
  .WithArguments([ciTestRunner, ..args])
  .WithWorkingDirectory(repoRoot)
  .WithNoValidation()
  .RunAsync();
