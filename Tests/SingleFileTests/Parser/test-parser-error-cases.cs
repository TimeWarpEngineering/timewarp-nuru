#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine
(
  """
  Testing Parser Error Cases for New Modifiers
  =============================================
  These patterns should all fail to parse
  """
);

// Test helper
void TestInvalidPattern(string pattern, string expectedError)
{
    Write($"  {pattern,-35} - ");
    try
    {
        CompiledRoute route = RoutePatternParser.Parse(pattern);
        WriteLine($"✗ SHOULD HAVE FAILED! (Expected: {expectedError})");
    }
    catch (Exception ex)
    {
        string firstError = ex.Message.Split('\n')[0];
        if (firstError.Contains(expectedError, StringComparison.OrdinalIgnoreCase))
        {
            WriteLine($"✓ Failed correctly: {expectedError}");
        }
        else
        {
            WriteLine($"✓ Failed (different error): {firstError}");
        }
    }
}

WriteLine
(
  """

  Double Modifiers:
  """
);

TestInvalidPattern("--flag??", "duplicate modifier");
TestInvalidPattern("--env {var}**", "duplicate modifier");
TestInvalidPattern("--opt?? {val}", "duplicate modifier");
TestInvalidPattern("--port {p}**", "duplicate modifier");

WriteLine
(
  """

  Wrong Position:
  """
);

TestInvalidPattern("?--flag", "invalid position");
TestInvalidPattern("--?flag", "invalid position");
TestInvalidPattern("*--env {var}", "invalid position");
TestInvalidPattern("--env* {var}", "asterisk on flag");
TestInvalidPattern("-?v", "invalid position");

WriteLine
(
  """

  Wrong Order:
  """
);

TestInvalidPattern("--flag*?", "wrong modifier order");
TestInvalidPattern("--env {var}*?", "wrong modifier order");
TestInvalidPattern("--opt*? {val}", "wrong modifier order");
TestInvalidPattern("--port {p:int}*?", "wrong modifier order");

WriteLine
(
  """

  Misplaced on Parameters:
  """
);

TestInvalidPattern("deploy {env?*}", "wrong modifier order");
TestInvalidPattern("test {file*?}", "wrong modifier order");
TestInvalidPattern("build {?target}", "invalid position");
TestInvalidPattern("run {*?args}", "invalid position");

WriteLine
(
  """

  Invalid Combinations:
  """
);

TestInvalidPattern("--env? {*var}", "asterisk in wrong place");
TestInvalidPattern("--flag {?param}", "question mark in wrong place");
TestInvalidPattern("test {param}? --flag", "modifier on positional");
TestInvalidPattern("build {src}* {dst}", "repeated not at end");

WriteLine
(
  """

  Conflicting Semantics:
  """
);

TestInvalidPattern("test {file}* {other}", "parameter after repeated");
TestInvalidPattern("run {args}* --flag", "option after repeated positional");
TestInvalidPattern("exec {cmd}* {env} {*rest}", "multiple arrays");
TestInvalidPattern("deploy {*all} {more}", "parameter after catch-all");

WriteLine
(
  """

  Mixed with Existing Errors:
  """
);

TestInvalidPattern("test <param>?", "angle brackets");
TestInvalidPattern("build {env??", "unbalanced braces");
TestInvalidPattern("run --flag? {", "unbalanced braces");
TestInvalidPattern("deploy {env} {env}*", "duplicate parameter");

WriteLine
(
  """

  ========================================
  Summary:
  Parser validation must ensure:
  - Modifiers appear only once
  - Modifiers are in correct position
  - Modifiers are in correct order (? before *)
  - Semantic rules are enforced
  - Error messages are clear and helpful
  """
);