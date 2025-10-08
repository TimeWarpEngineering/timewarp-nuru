#!/usr/bin/dotnet --
#:property LangVersion=preview
#:property EnablePreviewFeatures=true
#:package TimeWarp.Amuru

using TimeWarp.Amuru;

// Get script directory to build correct paths
string scriptDir = AppContext.GetData("EntryPointFileDirectoryPath") as string
  ?? throw new InvalidOperationException("Could not get entry point directory");

string testsDir = Path.GetDirectoryName(scriptDir)!;

// Configure Nuru app with routing
NuruAppBuilder builder = new();

// TODO: Bug in Nuru - optional flag with required param doesn't work without args
// Should be: builder.AddRoute("--tag? {tag}", (string? tag) => RunTests(tag), ...);
// Workaround: Use two routes until optional flag bug is fixed

// builder.AddDefaultRoute(() => RunTests(null), "Run all Jaribu tests");
builder.AddRoute("--tag? {tag?}", (string? tag) => RunTests(tag), "Run tests filtered by tag (Lexer, Parser)");

NuruApp app = builder.Build();
return await app.RunAsync(args);

async Task<int> RunTests(string? filterTag)
{
  // Run all Jaribu-based tests
  WriteLine("🧪 Running Jaribu-based Tests");

  if (filterTag is not null)
  {
    WriteLine($"   Filtering by tag: {filterTag}");
  }

  WriteLine();

  // Track overall results
  int totalTests = 0;
  int passedTests = 0;

// List of Jaribu-based test files (relative to Tests directory)
string[] testFiles = [
  // Jaribu self-tests
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-01-discovery.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-02-parameterized.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-03-tag-filtering.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-04-skipping-exceptions.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-05-cache-clearing.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-06-reporting-cleanup.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-07-edges.cs"),
  // Nuru tests
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-01-basic-token-types.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-02-valid-options.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-03-invalid-double-dashes.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-04-invalid-trailing-dashes.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-05-multi-char-short-options.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-06-end-of-options.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-07-invalid-angle-brackets.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-08-whitespace-handling.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-09-complex-patterns.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-10-edge-cases.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-11-error-reporting.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-12-description-tokenization.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-13-parameter-context.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/lexer-14-token-position.cs"),
  // Parser tests (numbered)
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-01-basic-parameters.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-02-typed-parameters.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-03-optional-parameters.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-04-duplicate-parameters.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-05-consecutive-optionals.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-06-catchall-position.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-07-catchall-optional-conflict.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-08-option-modifiers.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-09-end-of-options.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-10-specificity-ranking.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-11-complex-integration.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-12-error-reporting.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-13-syntax-errors.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-14-mixed-modifiers.cs"),
  // Routing tests (numbered)
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Routing/routing-01-basic-matching.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Routing/routing-02-parameter-binding.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Routing/routing-03-optional-parameters.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Routing/routing-04-catch-all.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Routing/routing-05-option-matching.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Routing/routing-06-repeated-options.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Routing/routing-07-route-selection.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Routing/routing-08-end-of-options.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Routing/routing-09-complex-integration.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Routing/routing-10-error-cases.cs"),
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Routing/routing-11-delegate-mediator.cs"),
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
    await Shell.Builder("chmod").WithArguments("+x", fullPath).RunAsync();
  }

  // Build shell command with optional tag filter environment variable
  CommandOutput result = filterTag is not null
    ? await Shell.Builder(fullPath)
        .WithWorkingDirectory(Path.GetDirectoryName(fullPath)!)
        .WithNoValidation()
        .WithEnvironmentVariable("JARIBU_FILTER_TAG", filterTag)
        .CaptureAsync()
    : await Shell.Builder(fullPath)
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

  return passedTests == totalTests ? 0 : 1;
}
