#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru

#region Purpose
// Multi-mode CI test runner: runs every test class auto-registered into the JARIBU_MULTI assembly, then
// the standalone phase.
// NURU_CI_PHASE=multi skips the standalone phase, which CI runs as a job of its own (task 538).
// JARIBU_COVERAGE=1 runs both phases inside one dotnet-coverage collection (task 574).
#endregion

using TimeWarp.Amuru;

// Runner progress is process stdout. RS0030 bans System.Console in app code.
#pragma warning disable RS0030

WriteLine("TimeWarp.Nuru Multi-Mode Test Runner");
WriteLine();

Stopwatch multiExecute = Stopwatch.StartNew();
int multiResult = await RunAllTests();
multiExecute.Stop();
WriteLine
(
  $"Multi-mode execute finished in {multiExecute.Elapsed.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}s"
);

// JARIBU_COVERAGE=1 (dev test --coverage, task 574): RunAllTests relaunched this
// script under dotnet-coverage and returned the child's exit code. The child ran
// every phase, so the parent stops here.
if (CoverageRelaunchedParent())
{
  return multiResult;
}

string ciDir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? ".";
string phase = Environment.GetEnvironmentVariable("NURU_CI_PHASE") ?? "";
if (phase == "multi")
{
  WriteLine("NURU_CI_PHASE=multi: standalone phase skipped.");
  return multiResult;
}

string standaloneRunner = Path.GetFullPath(Path.Combine(ciDir, "..", "ci-standalone", "run-standalone-tests.cs"));
int standaloneResult = await Shell.Builder("dotnet")
  .WithArguments("run", standaloneRunner)
  .WithWorkingDirectory(Path.GetDirectoryName(standaloneRunner)!)
  .WithNoValidation()
  .RunAsync();

return multiResult != 0 ? multiResult : standaloneResult;

static bool CoverageRelaunchedParent()
{
  string? requested = Environment.GetEnvironmentVariable(RunfileCoverageOptions.VariableName);
  bool coverage = requested == "1" || string.Equals(requested, "true", StringComparison.OrdinalIgnoreCase);
  return coverage && Environment.GetEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName) != "1";
}
