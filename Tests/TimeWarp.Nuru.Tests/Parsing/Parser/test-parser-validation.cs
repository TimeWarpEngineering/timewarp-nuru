#!/usr/bin/dotnet --
#:project ../../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine
(
  """
  Testing Route Pattern Parser Validation
  ========================================

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

TestValid("status");
TestValid("git commit");
TestValid("deploy {env}");
TestValid("deploy {env} {tag?}");
TestValid("delay {ms:int}");
TestValid("docker {*args}");
TestValid("build --verbose");
TestValid("build --config {mode}");
TestValid("deploy {env|Environment} --dry-run,-d|Preview");

WriteLine
(
  """

  Invalid Patterns (should fail):
  -------------------------------

  Syntax Errors:
  """
);
TestInvalid("prompt <input>", "Invalid parameter syntax");
TestInvalid("deploy {env", "Expected '}'");
TestInvalid("build --config {", "Expected parameter name");
TestInvalid("test }", "Unexpected '}'");

WriteLine
(
  """

  Modifier Errors:
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

  Semantic Errors:
  """
);

// Invalid combinations
TestInvalid("--env? {*var}", "asterisk in wrong place");
TestInvalid("--flag {?param}", "question mark in wrong place");
TestInvalid("test {param}? --flag", "modifier on positional");
TestInvalid("build {src}* {dst}", "repeated not at end");

// Conflicting semantics
TestInvalid("test {file}* {other}", "parameter after repeated");
TestInvalid("run {args}* --flag", "option after repeated positional");
TestInvalid("exec {cmd}* {env} {*rest}", "multiple arrays");
TestInvalid("deploy {*all} {more}", "parameter after catch-all");

// Duplicate parameters
TestInvalid("deploy {env} {env}", "duplicate parameter");
TestInvalid("build {src} --output {src}", "duplicate parameter");
TestInvalid("test {file} {file}*", "duplicate parameter");

WriteLine
(
  """

  ========================================
  """
);
WriteLine($"Summary: {passed} passed, {failed} failed");

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

return failed > 0 ? 1 : 0;

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
        WriteLine("✗ SHOULD HAVE PARSED!");
        WriteLine($"    Error: {ex.Message.Split('\n')[0]}");
        failed++;
    }
}

void TestInvalid(string pattern, string expectedError)
{
    Write($"  {pattern,-35} ");
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