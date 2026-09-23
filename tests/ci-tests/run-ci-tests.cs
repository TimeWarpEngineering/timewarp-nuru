#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru

// Multi-mode Test Runner
// Test classes are auto-registered via [ModuleInitializer] when compiled with JARIBU_MULTI.

using TimeWarp.Amuru;

WriteLine("TimeWarp.Nuru Multi-Mode Test Runner");
WriteLine();

int multiResult = await RunAllTests();

// Files that cannot run (or whose gated cases cannot run) inside the JARIBU_MULTI
// assembly are listed in CiTestExcludes (Directory.Build.props) and/or executed here
// as a second phase so CI still covers them:
// - generator-17: top-level-statements local-function ConfigureServices (M14 / 470-010)
// - check-version-04: entire body #if !JARIBU_MULTI; needs CheckVersionCommand endpoint (M15)
// - generator-19/20: multi-included for filtered cases, but #if !JARIBU_MULTI methods
//   (NoFilter_IncludesAll, Gen20KanbanQuery) only run in this standalone phase (M29)
// - generator-28..45: Roslyn-hosted; timewarp-nuru-analyzers LIBRARY collides (CS0433)
string ciDir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? ".";
string[] standaloneTests =
[
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-17-local-function-config.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "devcli", "check-version-04-endpoint-zero-package.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-19-group-filtering.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-20-parameterized-service-constructor.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-28-interpreter-cycle-guard.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-29-h002-named-arguments.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-30-nuru-r003-overlap.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-31-m7-param-mismatch.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-32-m9-unrelated-fluent.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-33-m6-failsoft-catch.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-34-m8-ilistmanager-repeated.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-35-m8-idata-service.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-36-m8-typeconverter-fqn.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-37-incrementality-caching.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-38-h002-sole-identifier-body.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-39-keyword-param-identifiers.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-40-with-example-non-literal-command.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-42-extension-method-lowering-diagnostics.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-43-telemetry-options.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-44-referenced-type-arguments.cs"),
  Path.Combine(ciDir, "..", "timewarp-nuru-tests", "generator", "generator-45-constructor-default-literals.cs"),
];

int standaloneFailures = 0;
foreach (string testFile in standaloneTests)
{
  string fullPath = Path.GetFullPath(testFile);
  WriteLine();
  WriteLine($"Running standalone: {Path.GetFileName(fullPath)}");

  CommandOutput result = await Shell.Builder("dotnet")
    .WithArguments("run", fullPath)
    .WithWorkingDirectory(Path.GetDirectoryName(fullPath)!)
    .WithNoValidation()
    .CaptureAsync();

  WriteLine(result.Stdout);
  if (result.ExitCode != 0)
  {
    standaloneFailures++;
    WriteLine(result.Stderr);
    WriteLine($"❌ {Path.GetFileName(fullPath)} failed with exit code {result.ExitCode}");
  }
}

return multiResult != 0 ? multiResult : (standaloneFailures > 0 ? 1 : 0);
