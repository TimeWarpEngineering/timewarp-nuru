#!/usr/bin/dotnet --
#:project ../../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine
(
  """
  Testing Parser Support for Mixed Modifiers (? and *)
  =====================================================
  Testing combinations of optional and repeated modifiers
  """
);

// Test helper
void TestPattern(string pattern, bool shouldSucceed, string description)
{
    Write($"  {pattern,-50} - {description,-30} ");
    try
    {
        CompiledRoute route = RoutePatternParser.Parse(pattern);
        if (shouldSucceed)
        {
            WriteLine("✓ Parsed");
        }
        else
        {
            WriteLine("✗ UNEXPECTED: Should have failed!");
        }
    }
    catch (Exception ex)
    {
        if (!shouldSucceed)
        {
            WriteLine("✓ Failed as expected");
        }
        else
        {
            WriteLine($"✗ FAILED: {ex.Message.Split('\n')[0]}");
        }
    }
}

WriteLine
(
  """

  Optional and Repeated Combined:
  """
);

TestPattern("docker --env? {var}*", true, "Optional flag, repeated param");
TestPattern("build --define? {key}*", true, "Optional repeated definitions");
TestPattern("test --tag? {t}*", true, "Optional repeated tags");
TestPattern("run --port? {p:int}*", true, "Optional repeated typed");
TestPattern("deploy --label? {l}* --env? {e}*", true, "Multiple optional repeated");

WriteLine
(
  """

  Wrong Modifier Order:
  """
);

TestPattern("docker --env*?", false, "Wrong order on flag");
TestPattern("build --define {key}*?", false, "Wrong order on parameter");
TestPattern("test --tag?* {t}", false, "Modifiers on flag not param");
TestPattern("run --port {p}?*", false, "Wrong order on param");

WriteLine
(
  """

  All Modifiers Combined:
  """
);

TestPattern("deploy --env? {var?}*", true, "Everything optional/repeated");
TestPattern("test --opt? {val?}*", true, "Full flexibility");
TestPattern("build --config? {mode?}* --verbose?", true, "Complex combination");
TestPattern("run --setting? {key?}* --debug? --trace?", true, "Multiple complex options");

WriteLine
(
  """

  Real-world Complex Patterns:
  """
);

TestPattern("docker run {img} --env? {e}* --volume? {v}* --detach?", true, "Docker-like");
TestPattern("kubectl apply -f? {file}* --namespace? {ns?}", true, "Kubectl-like");
TestPattern("npm install {pkg?} --save-dev? --global?", true, "NPM-like");
TestPattern("git commit -m {msg} --amend? --no-edit?", true, "Git-like");

WriteLine
(
  """

  With Positional Parameters:
  """
);

TestPattern("cmd {pos1} {pos2?} --opt? {val}*", true, "Positional with opt repeated");
TestPattern("exec {cmd} --env? {e}* {*args}", true, "Positional, optional, repeated, catch-all");
TestPattern("deploy {env} {tag?} --force? --label? {l}*", true, "Full combination");

WriteLine
(
  """

  Edge Cases:
  """
);

TestPattern("test --verbose?", true, "Just optional flag");
TestPattern("test --env {e}*", true, "Just repeated param");
TestPattern("test --verbose? --env {e}*", true, "Optional flag and repeated");
TestPattern("test --opt? {v?}* --flag? --param {p}*", true, "Maximum complexity");

WriteLine
(
  """

  ========================================
  Summary:
  The parser must handle combinations of modifiers:
  - ? makes flags/parameters optional
  - * makes parameters repeatable
  - Order matters: ? before * when combined
  - Modifiers apply to the correct element (flag vs param)
  """
);