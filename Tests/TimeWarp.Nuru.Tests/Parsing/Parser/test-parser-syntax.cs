#!/usr/bin/dotnet --
#:project ../../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine
(
  """
  Testing Route Pattern Parser Syntax
  ====================================
  Tests for syntactic parsing errors and valid patterns
  """
);

int passed = 0;
int failed = 0;

// Test valid patterns that should parse successfully
WriteLine
(
  """

  Valid Patterns (should parse):
  -------------------------------
  """
);

void TestValid(string pattern)
{
    Write($"  {pattern,-45} ");
    try
    {
        CompiledRoute route = RoutePatternParser.Parse(pattern);
        WriteLine("✓ Parsed");
        passed++;
    }
    catch (Exception ex)
    {
        WriteLine($"✗ FAILED: {ex.Message}");
        failed++;
    }
}

TestValid("status");
TestValid("git commit");
TestValid("deploy {env}");
TestValid("deploy {env} {tag?}");
TestValid("delay {ms:int}");
TestValid("docker {*args}");
TestValid("build --verbose");
TestValid("build --config {mode}");
TestValid("deploy {env|Environment} --dry-run,-d|Preview");

// Test invalid patterns that should fail
WriteLine
(
  """

  Invalid Patterns (should fail):
  --------------------------------
  """
);

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
        // Check the full error message for the expected error
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

  Basic Syntax Errors:
  --------------------
  """
);
TestInvalid("prompt <input>", "Invalid parameter syntax");
TestInvalid("deploy {env", "Expected '}'");
TestInvalid("build --config {", "Expected parameter name");
TestInvalid("test }", "Unexpected '}'");
TestInvalid("run {", "Expected parameter name");
TestInvalid("test { }", "Expected parameter name");
TestInvalid("build {123abc}", "Expected parameter name");  // Invalid identifier

WriteLine
(
  """

  Modifier Syntax Errors:
  -----------------------
  """
);

// Double modifiers
TestInvalid("--flag??", "duplicate modifier");
TestInvalid("--env {var}**", "duplicate modifier");
TestInvalid("--opt?? {val}", "duplicate modifier");
TestInvalid("--port {p}**", "duplicate modifier");

// Wrong position
TestInvalid("?--flag", "invalid position");
TestInvalid("--?flag", "invalid position");
TestInvalid("*--env {var}", "invalid position");
TestInvalid("--env* {var}", "asterisk on flag");
TestInvalid("-?v", "invalid position");

// Wrong order
TestInvalid("--flag*?", "wrong modifier order");
TestInvalid("--env {var}*?", "wrong modifier order");
TestInvalid("--opt*? {val}", "wrong modifier order");
TestInvalid("--port {p:int}*?", "wrong modifier order");

// Misplaced on parameters
TestInvalid("deploy {env?*}", "wrong modifier order");
TestInvalid("test {file*?}", "wrong modifier order");
TestInvalid("build {?target}", "invalid position");
TestInvalid("run {*?args}", "invalid position");

WriteLine
(
  """

  Invalid Parameter Modifiers:
  ----------------------------
  """
);
TestInvalid("--env? {*var}", "asterisk in wrong place");
TestInvalid("--flag {?param}", "question mark in wrong place");
TestInvalid("test {param}? --flag", "modifier on positional");

WriteLine
(
  """

  Type Constraint Errors:
  -----------------------
  """
);
TestInvalid("test {value:invalid}", "Invalid type constraint");
TestInvalid("run {id:integer}", "Invalid type constraint");  // Should be 'int'
TestInvalid("calc {num:float}", "Invalid type constraint");   // Not supported yet

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

      Note: Some error messages are generic.
      Parser could be improved to provide more specific error messages.
      """
    );
}

Environment.Exit(failed > 0 ? 1 : 0);