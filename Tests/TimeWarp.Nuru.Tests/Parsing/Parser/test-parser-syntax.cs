#!/usr/bin/dotnet --

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
    CompiledRoute route = PatternParser.Parse(pattern);
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
    PatternException exception = Should.Throw<PatternException>(() =>
    {
      CompiledRoute route = PatternParser.Parse(pattern);
    });

    // Check if the actual error message contains what we expect
    if (exception.Message.Contains(expectedError, StringComparison.InvariantCultureIgnoreCase))
    {
      WriteLine("✓ Failed correctly");
      passed++;
    }
    else
    {
      // Wrong error message
      WriteLine("⚠ Wrong error message");
      WriteLine($"    Expected substring: {expectedError}");
      WriteLine($"    Full message: {exception.Message}");
      WriteLine();
      failed++;
    }
  }
  catch (ShouldAssertException)
  {
    // Should.Throw failed - means no exception was thrown
    WriteLine("✗ SHOULD HAVE FAILED!");
    WriteLine($"    Expected: {expectedError}");
    failed++;
  }
}

void TestInvalidByType(string pattern, Type expectedParseErrorType)
{
  Write($"  {pattern,-45} ");

  try
  {
    CompiledRoute route = PatternParser.Parse(pattern);
    WriteLine("✗ SHOULD HAVE THROWN!");
    WriteLine($"    Expected: {expectedParseErrorType.Name}");
    WriteLine($"    Parser produced: {route.PositionalMatchers.Count} positional, {route.OptionMatchers.Count} options");
    if (route.OptionMatchers.Count > 0)
    {
      WriteLine($"    First option: {route.OptionMatchers[0].MatchPattern}");
    }

    failed++;
  }
  catch (PatternException ex)
  {
    // Parser correctly threw an exception - now validate the error type
    if (ex.ParseErrors is null || ex.ParseErrors.Count == 0)
    {
      WriteLine("⚠ No ParseErrors in exception");
      WriteLine($"    Expected: {expectedParseErrorType.Name}");
      failed++;
      return;
    }

    ParseError parseError = ex.ParseErrors[0];
    if (parseError.GetType() == expectedParseErrorType)
    {
      WriteLine("✓ Failed correctly");
      passed++;
    }
    else
    {
      WriteLine("⚠ Wrong error type");
      WriteLine($"    Expected: {expectedParseErrorType.Name}");
      WriteLine($"    Actual: {parseError.GetType().Name}");
      failed++;
    }
  }
  catch (Exception ex)
  {
    WriteLine("✗ Unexpected exception type");
    WriteLine($"    Expected: RoutePatternException with {expectedParseErrorType.Name}");
    WriteLine($"    Actual: {ex.GetType().Name}");
    failed++;
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
TestInvalidByType("--flag??", typeof(InvalidCharacterError));
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