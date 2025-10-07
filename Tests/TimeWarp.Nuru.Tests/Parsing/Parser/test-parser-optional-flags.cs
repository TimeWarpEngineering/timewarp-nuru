#!/usr/bin/dotnet --

WriteLine
(
  """
  Testing Parser Support for Optional Flag Modifier (?)
  ======================================================
  Testing if RoutePatternParser can handle the ? modifier on flags
  """
);

// Test helper
void TestPattern(string pattern, bool shouldSucceed, string description)
{
    Write($"  {pattern,-40} - {description,-35} ");
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

  Valid Optional Flag Patterns:
  """
);

TestPattern("build --verbose?", true, "Optional boolean flag");
TestPattern("deploy --dry-run?", true, "Optional flag with dash");
TestPattern("test -v?", true, "Short optional flag");
TestPattern("build --config? {mode}", true, "Optional flag, required value");
TestPattern("deploy --env? {name?}", true, "Optional flag, optional value");
TestPattern("git commit --amend? -m {msg}", true, "Mixed optional and required");

WriteLine
(
  """

  Invalid Optional Flag Patterns:
  """
);

TestPattern("build --verbose??", false, "Double question mark");
TestPattern("deploy ?--dry-run", false, "Question mark before flag");
TestPattern("test --?verbose", false, "Question mark in wrong position");
TestPattern("build -?v", false, "Question mark before short flag");

WriteLine
(
  """

  Complex Patterns with Multiple Optional Flags:
  """
);

TestPattern("deploy {env} --force? --dry-run?", true, "Multiple optional flags");
TestPattern("build --verbose? --debug? --release?", true, "All flags optional");
TestPattern("test {file} --watch? --coverage? -v?", true, "Mixed long and short optional");

WriteLine
(
  """

  Mixed Required and Optional:
  """
);

TestPattern("deploy --env {e} --version? {v?}", true, "Required and optional mixed");
TestPattern("build --output {path} --verbose?", true, "Required with value, optional bool");
TestPattern("test --config {c} --watch? --debug?", true, "One required, two optional");

WriteLine
(
  """

  ========================================
  Summary:
  The parser needs to recognize ? as a flag modifier.
  This modifier should set IsOptional=true on the OptionMatcher.
  The ? must appear at the end of the flag name, before any parameter.
  """
);