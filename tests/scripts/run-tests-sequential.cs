#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru

using TimeWarp.Amuru;

// Legacy diagnostic runner — hand-lists pointed at deleted dirs (task 470-012 / M27).
// Prefer: dotnet run tests/ci-tests/run-ci-tests.cs

string scriptsDir = AppContext.GetData("EntryPointFileDirectoryPath") as string
  ?? Environment.CurrentDirectory;
string testsRoot = Path.GetFullPath(Path.Combine(scriptsDir, ".."));
string repoRoot = Path.GetFullPath(Path.Combine(testsRoot, ".."));
string ciTestRunner = Path.Combine(testsRoot, "ci-tests", "run-ci-tests.cs");

WriteLine("Delegating to tests/ci-tests/run-ci-tests.cs (official CI runner)...");
WriteLine("(Legacy sequential hand-lists removed; UseNewGen flags are ignored here.)");
WriteLine();

return await Shell.Builder("dotnet")
  .WithArguments([ciTestRunner, ..args])
  .WithWorkingDirectory(repoRoot)
  .WithNoValidation()
  .RunAsync();
