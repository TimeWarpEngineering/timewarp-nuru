#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru

// Multi-mode Test Runner
// Test classes are auto-registered via [ModuleInitializer] when compiled with JARIBU_MULTI.

using TimeWarp.Amuru;

WriteLine("TimeWarp.Nuru Multi-Mode Test Runner");
WriteLine();

int multiResult = await RunAllTests();

// Roslyn-hosted generator tests reference timewarp-nuru-analyzers as a LIBRARY, whose
// shared parsing types (Lexer/Token) collide with timewarp-nuru's in the multi-mode
// compilation (CS0433). They are excluded from the multi assembly (CiTestExcludes in
// Directory.Build.props) and executed standalone here so CI still covers them.
string ciDir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? ".";
string[] standaloneTests =
[
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
