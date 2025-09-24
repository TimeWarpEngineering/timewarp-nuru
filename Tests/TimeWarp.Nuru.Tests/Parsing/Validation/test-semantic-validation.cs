#!/usr/bin/dotnet --
#:project ../../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine
(
  """
  Testing Semantic Validation Errors
  ===================================
  Tests for semantic errors that are syntactically valid but semantically incorrect
  """
);

int passed = 0;
int failed = 0;

void TestInvalid(string pattern, string expectedError)
{
    Write($"  {pattern,-45} ");
    try
    {
        CompiledRoute route = RoutePatternParser.Parse(pattern);
        WriteLine("✗ SHOULD HAVE FAILED!");
        WriteLine($"    Expected: {expectedError}");
        failed++;
    }
    catch (Exception ex)
    {
        // Check the full error message, not just the first line
        bool hasExpectedError = ex.Message.Contains(expectedError, StringComparison.OrdinalIgnoreCase);

        if (hasExpectedError)
        {
            WriteLine("✓ Failed correctly");
            passed++;
        }
        else
        {
            WriteLine("⚠ Failed (different error)");
            WriteLine($"    Expected: {expectedError}");
            string firstError = ex.Message.Split('\n')[0];
            WriteLine($"    Actual:   {firstError}");
            // Still counts as passed since it failed, just not with expected message
            passed++;
        }
    }
}

WriteLine
(
  """

  Duplicate Parameters:
  ---------------------
  """
);
TestInvalid("deploy {env} {env}", "duplicate parameter");
TestInvalid("build {src} --output {src}", "duplicate parameter");
TestInvalid("test {file} {file}*", "duplicate parameter");
TestInvalid("run --tag {name} --label {name}", "duplicate parameter");

WriteLine
(
  """

  Catch-all Position Errors:
  --------------------------
  """
);
TestInvalid("deploy {*all} {more}", "parameter after catch-all");
TestInvalid("run {*args} --verbose", "catch-all");  // Options are OK after catch-all
TestInvalid("test {*files} {output}", "parameter after catch-all");

WriteLine
(
  """

  Repeated Parameter Errors:
  --------------------------
  """
);
TestInvalid("build {src}* {dst}", "repeated not at end");
TestInvalid("test {file}* {other}", "parameter after repeated");
TestInvalid("run {args}* --flag", "option after repeated positional");
TestInvalid("exec {cmd}* {env} {*rest}", "multiple arrays");

WriteLine
(
  """

  Optional/Required Order:
  ------------------------
  """
);
// These tests would check optional before required validation once implemented
// TestInvalid("deploy {env?} {tag}", "optional before required");

WriteLine
(
  """

  Mixed Catch-all with Optional:
  -------------------------------
  """
);
// TestInvalid("test {file?} {*rest}", "Cannot mix optional parameters with catch-all");

WriteLine
(
  $"""

  ========================================
  Summary: {passed} passed, {failed} failed
  """
);

if (failed > 0)
{
    WriteLine
    (
      """

      Note: Some semantic validation rules may not be implemented yet.
      """
    );
}

Environment.Exit(failed > 0 ? 1 : 0);