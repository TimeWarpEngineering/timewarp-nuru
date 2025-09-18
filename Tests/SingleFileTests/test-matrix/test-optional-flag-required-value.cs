#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Optional Flag with Required Value (--flag? {value})
  ============================================================

  Pattern: --config? {mode}
  Expectation: Flag is optional, but if present MUST have a value
  """
);

NuruAppBuilder builder = new();

// Test route with optional flag that requires a value if present
builder.AddRoute("build --config? {mode}", (string? mode) =>
{
    WriteLine($"✓ Build executed with config: {mode ?? "(none)"}");
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: build
  Expected: Match with mode=null
  """
);
try
{
    await app.RunAsync(["build"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Currently fails because --config? syntax not implemented yet");
}

WriteLine
(
  """

  Test 2: build --config debug
  Expected: Match with mode="debug"
  """
);
try
{
    await app.RunAsync(["build", "--config", "debug"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Currently fails because --config? syntax not implemented yet");
}

WriteLine
(
  """

  Test 3: build --config
  Expected: No match (error - flag requires value)
  """
);
try
{
    await app.RunAsync(["build", "--config"]);
    WriteLine("✗ UNEXPECTED: Should have failed - flag requires value!");
}
catch (Exception ex)
{
    WriteLine($"✓ Correctly failed: {ex.Message}");
}

WriteLine
(
  """

  ========================================
  Summary:
  This test demonstrates the --flag? {value} pattern where:
  - The flag itself is optional (can be omitted entirely)
  - But if the flag IS present, it MUST have a value
  - This is useful for optional configuration that needs a value when specified
  """
);