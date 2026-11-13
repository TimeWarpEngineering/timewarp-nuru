#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru

#region Purpose
// Multi-mode CI test runner: runs every test class auto-registered into the JARIBU_MULTI assembly, then
// the standalone phase.
// NURU_CI_PHASE=multi skips the standalone phase, which CI runs as a job of its own (task 538).
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
