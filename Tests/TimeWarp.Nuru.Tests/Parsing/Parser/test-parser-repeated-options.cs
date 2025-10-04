#!/usr/bin/dotnet --

WriteLine
(
  """
  Testing Parser Support for Repeated Option Modifier (*)
  ========================================================
  Testing if RoutePatternParser can handle the * modifier on parameters
  """
);

// Test helper
void TestPattern(string pattern, bool shouldSucceed, string description)
{
    Write($"  {pattern,-45} - {description,-35} ");
    try
    {
        CompiledRoute route = PatternParser.Parse(pattern);
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

  Valid Repeated Option Patterns:
  """
);

TestPattern("docker --env {var}*", true, "Repeated string parameter");
TestPattern("build --define {key}*", true, "Repeated definitions");
TestPattern("run --port {num:int}*", true, "Repeated typed parameter");
TestPattern("test --exclude {pattern}*", true, "Repeated exclusions");
TestPattern("deploy --tag {t}*", true, "Short parameter name");
TestPattern("exec --env {e}* --volume {v}*", true, "Multiple repeated options");

WriteLine
(
  """

  Invalid Repeated Option Patterns:
  """
);

TestPattern("docker --env {var}**", false, "Double asterisk");
TestPattern("build --env* {var}", false, "Asterisk on flag not parameter");
TestPattern("run *--port {num}", false, "Asterisk before flag");
TestPattern("test --exclude {*pattern}", false, "Asterisk inside parameter");
TestPattern("deploy --tag* {t}*", false, "Asterisk on both flag and param");

WriteLine
(
  """

  Repeated with Types:
  """
);

TestPattern("server --port {p:int}*", true, "Repeated integers");
TestPattern("calc --value {v:double}*", true, "Repeated doubles");
TestPattern("schedule --date {d:DateTime}*", true, "Repeated DateTimes");
TestPattern("config --flag {f:bool}*", true, "Repeated booleans");

WriteLine
(
  """

  Complex Patterns with Repeated Options:
  """
);

TestPattern("docker run {image} --env {e}* --volume {v}*", true, "Multiple repeated");
TestPattern("kubectl --label {l}* --annotation {a}*", true, "Two repeated options");
TestPattern("build --src {s}* --exclude {e}* --output {o}", true, "Mixed repeated and single");

WriteLine
(
  """

  Combined with Catch-all:
  """
);

TestPattern("exec --env {e}* -- {*cmd}", true, "Repeated option with catch-all");
TestPattern("run --opt {o}* {*args}", true, "Repeated then catch-all");
TestPattern("docker --env {e}* --volume {v}* {*cmd}", true, "Multiple repeated with catch-all");

WriteLine
(
  """

  ========================================
  Summary:
  The parser needs to recognize * as a parameter modifier.
  This modifier should indicate the parameter can be repeated.
  The * must appear at the end of the parameter definition.
  When repeated, values are collected into an array.
  """
);