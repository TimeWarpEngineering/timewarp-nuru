#!/usr/bin/dotnet --
#:project ../../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine
(
  """
  Testing Duplicate Parameter Validation
  =======================================
  Validates that semantic validator detects duplicate parameter names
  across positional parameters and option parameters
  """
);

int passed = 0;
int failed = 0;

// Test helper
void TestDuplicateDetection(string pattern, bool shouldHaveDuplicate, string description)
{
    Write($"  {pattern,-40} - {description,-40} ");

    RouteParser parser = new();
    ParseResult<RouteSyntax> result = parser.Parse(pattern);

    bool hasDuplicateError = result.SemanticErrors.Any(e =>
        e.ErrorType == SemanticErrorType.DuplicateParameterNames);

    if (shouldHaveDuplicate && hasDuplicateError)
    {
        WriteLine("✓ Correctly detected duplicate");
        passed++;
    }
    else if (!shouldHaveDuplicate && !hasDuplicateError)
    {
        WriteLine("✓ Correctly allowed (no duplicate)");
        passed++;
    }
    else if (shouldHaveDuplicate && !hasDuplicateError)
    {
        WriteLine("✗ FAILED: Should detect duplicate!");
        failed++;
    }
    else
    {
        WriteLine("✗ FAILED: False positive!");
        failed++;
    }
}

WriteLine();
WriteLine("Testing duplicate parameter detection:");
WriteLine("--------------------------------------");

// Test cases that SHOULD detect duplicates
TestDuplicateDetection(
    "deploy {env} {env}",
    true,
    "Same param twice (positional)");

TestDuplicateDetection(
    "build {src} --output {src}",
    true,
    "Same param in positional and option");

TestDuplicateDetection(
    "test {file} --input {file}",
    true,
    "Positional and option with value");

TestDuplicateDetection(
    "run {name} --name {name}",
    true,
    "Same name in positional and option param");

// Test cases that should NOT detect duplicates
TestDuplicateDetection(
    "deploy {env} {tag}",
    false,
    "Different parameter names");

TestDuplicateDetection(
    "build {source} --output {dest}",
    false,
    "Different names pos/opt");

TestDuplicateDetection(
    "run {name} --name {value}",
    false,
    "Option param has different name");

TestDuplicateDetection(
    "test --input {file} --output {result}",
    false,
    "Different option parameters");

WriteLine();
WriteLine("========================================");
WriteLine($"Summary: {passed} passed, {failed} failed");

if (failed > 0)
{
    Environment.Exit(1);
}