#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru

// Multi-mode Test Runner
// Test classes are auto-registered via [ModuleInitializer] when compiled with JARIBU_MULTI.
// After the multi-mode phase it runs tests/ci-standalone/run-standalone-tests.cs,
// the files that cannot run in this assembly. NURU_CI_PHASE=multi skips that
// second phase; CI runs it as a job of its own (task 538).

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
