#!/usr/bin/dotnet --
#:project ../../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests
#pragma warning disable CA1307 // Specify StringComparison

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine("Testing Parser Support for -- (End-of-Options) Separator:");
WriteLine("==========================================================");
WriteLine();

int passed = 0;
int failed = 0;

void TestPattern(string pattern, bool shouldPass, string description)
{
  WriteLine($"Pattern: '{pattern}'");
  WriteLine($"Test: {description}");

  try
  {
    CompiledRoute route = RoutePatternParser.Parse(pattern);

    if (shouldPass)
    {
      WriteLine("  ✅ PASSED: Successfully parsed");
      passed++;
    }
    else
    {
      WriteLine("  ❌ FAILED: Expected to fail but parsed successfully");
      failed++;
    }
  }
  catch (Exception ex)
  {
    if (!shouldPass)
    {
      WriteLine($"  ✅ PASSED: Correctly failed with: {ex.Message}");
      passed++;
    }
    else
    {
      WriteLine($"  ❌ FAILED: {ex.Message}");
      failed++;
    }
  }

  WriteLine();
}

WriteLine("Valid patterns with -- separator:");
WriteLine("----------------------------------");

TestPattern("exec -- {*cmd}", true, "Simple -- with catch-all");
TestPattern("git log -- {*files}", true, "Command then -- with catch-all");
TestPattern("docker exec {container} -- {*cmd}", true, "Parameter then -- with catch-all");
TestPattern("exec --env {e} -- {*cmd}", true, "Option then -- with catch-all");
TestPattern("exec --env {e}* -- {*cmd}", true, "Repeated option then -- with catch-all");

WriteLine("Invalid patterns with -- separator:");
WriteLine("------------------------------------");

TestPattern("exec -- {param}", false, "-- without catch-all (regular parameter)");
TestPattern("exec -- {param?}", false, "-- with optional parameter");
TestPattern("exec -- {param:int}", false, "-- with typed parameter");
TestPattern("exec --", false, "-- without any parameter");
TestPattern("exec -- --verbose", false, "Option after --");
TestPattern("exec -- {*args} --verbose", false, "Option after catch-all following --");
TestPattern("exec -- {*args} {other}", false, "Parameter after catch-all following --");

WriteLine("Edge cases:");
WriteLine("-----------");

TestPattern("--", false, "Just -- alone");
TestPattern("-- {*args}", true, "-- at start with catch-all");
TestPattern("exec {cmd} -- {*args} -- {*more}", false, "Multiple -- separators");

WriteLine("========================================");
WriteLine($"Summary: {passed} passed, {failed} failed");

if (failed > 0)
{
  WriteLine();
  WriteLine("The parser needs to:");
  WriteLine("1. Recognize EndOfOptions token from lexer");
  WriteLine("2. Validate that only catch-all parameter follows --");
  WriteLine("3. Reject any options after --");
  WriteLine("4. Reject non-catch-all parameters after --");
}