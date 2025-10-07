#!/usr/bin/dotnet --

WriteLine
(
  """
  Testing Required Flag with Required Value (--flag {value})
  ===========================================================
  Pattern: --config {mode}
  Expectation: Flag is required and must have a value
  This should ALREADY WORK in current implementation
  """
);

NuruAppBuilder builder = new();

// Test route with required flag and required value
builder.AddRoute("build --config {mode}", (string mode) =>
{
    WriteLine($"✓ Build executed with config: {mode}");
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: build --config debug
  Expected: Match with mode="debug"
  """
);
try
{
    await app.RunAsync(["build", "--config", "debug"]);
    WriteLine("✓ PASS: Works as expected");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  Test 2: build --config
  Expected: No match (error - missing value)
  """
);
try
{
    await app.RunAsync(["build", "--config"]);
    WriteLine("✗ UNEXPECTED: Should have failed - missing value!");
}
catch
{
    WriteLine("✓ PASS: Correctly failed - value is required");
}

WriteLine
(
  """

  Test 3: build
  Expected: No match (missing required flag)
  """
);
try
{
    await app.RunAsync(["build"]);
    WriteLine("✗ UNEXPECTED: Should have failed - flag is required!");
}
catch
{
    WriteLine("✓ PASS: Correctly failed - flag is required");
}

WriteLine
(
  """

  ========================================
  Summary:
  This test verifies current behavior for required flags.
  All tests should PASS with current implementation.
  """
);