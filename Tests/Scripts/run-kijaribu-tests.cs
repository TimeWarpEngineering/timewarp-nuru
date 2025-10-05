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
var builder = new NuruAppBuilder();

// TODO: Bug in Nuru - optional flag with required param doesn't work without args
// Should be: builder.AddRoute("--tag? {tag}", (string? tag) => RunTests(tag), ...);
// Workaround: Use two routes until optional flag bug is fixed

builder.AddDefaultRoute(() => RunTests(null), "Run all Kijaribu tests");
builder.AddRoute("--tag {tag}", (string tag) => RunTests(tag), "Run tests filtered by tag (Lexer, Parser)");

NuruApp app = builder.Build();
return await app.RunAsync(args);

async Task<int> RunTests(string? filterTag)
{
  // Run all Kijaribu-based tests
  WriteLine("🧪 Running Kijaribu-based Tests");

  if (filterTag is not null)
  {
    WriteLine($"   Filtering by tag: {filterTag}");
  }

  WriteLine();

  // Track overall results
  int totalTests = 0;
  int passedTests = 0;

// List of Kijaribu-based test files (relative to Tests directory)
string[] testFiles = [
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
  Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/parser-12-specificity-ranking.cs"),
  // Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/test-route-pattern-tokenization-kijaribu.cs"),
  // Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/test-end-of-options-separator-kijaribu.cs"),
  // Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/test-invalid-token-detection-kijaribu.cs"),
  // Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/test-compound-identifier-tokenization-kijaribu.cs"),
  // Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/test-description-tokenization-kijaribu.cs"),
  // Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Lexer/test-modifier-tokenization-kijaribu.cs"),
  // Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/test-catchall-validation.cs"),
  // Path.Combine(testsDir, "TimeWarp.Nuru.Tests/Parsing/Parser/test-parser-end-of-options.cs"),
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
        .WithEnvironmentVariable("KIJARIBU_FILTER_TAG", filterTag)
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
