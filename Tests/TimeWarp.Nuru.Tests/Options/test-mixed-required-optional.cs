#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru;
using static System.Console;

WriteLine("Testing Mixed Required and Optional Options");
WriteLine("============================================");
WriteLine();
WriteLine("Pattern: deploy --env {env} --version? {ver?} --dry-run");
WriteLine("Expectation:");
WriteLine("  --env is REQUIRED (no ? on flag)");
WriteLine("  --version is OPTIONAL (? on flag)");
WriteLine("  --dry-run is OPTIONAL (boolean flags always optional)");
WriteLine();

NuruAppBuilder builder = new();

// Test route with mixed required and optional options
builder.Map("deploy --env {env} --version? {ver?} --dry-run",
    (string env, string? ver, bool dryRun) =>
{
    WriteLine("✓ Deploy executed:");
    WriteLine("  Environment: " + env);
    WriteLine($"  Version: {ver ?? "(latest)"}");
    WriteLine("  Dry Run: " + dryRun);
});

NuruCoreApp app = builder.Build();

// Test 1: All options provided
WriteLine("Test 1: deploy --env prod --version v1.0 --dry-run");
WriteLine("Expected: Match with all values");
try
{
    await app.RunAsync(["deploy", "--env", "prod", "--version", "v1.0", "--dry-run"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Currently fails because optional flags not implemented yet");
}

WriteLine
(
  """

  Test 2: deploy --env prod
  Expected: Match with env="prod", version=null, dryRun=false
  """
);
try
{
    await app.RunAsync(["deploy", "--env", "prod"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Currently fails because all options are required");
}

WriteLine
(
  """

  Test 3: deploy --version v1.0
  Expected: No match (missing required --env)
  """
);
int result = await app.RunAsync(["deploy", "--version", "v1.0"]);
if (result != 0)
{
    WriteLine("✓ Correctly failed - missing required --env");
}
else
{
    WriteLine("✗ UNEXPECTED: Should have failed!");
}

WriteLine
(
  """

  Test 4: deploy --env staging --dry-run
  Expected: Match with env="staging", version=null, dryRun=true
  """
);
await app.RunAsync(["deploy", "--env", "staging", "--dry-run"]);

WriteLine();
WriteLine("========================================");
WriteLine("Summary:");
WriteLine("This test demonstrates mixing required and optional flags where:");
WriteLine("- Required flags (no ?) must be present or route won't match");
WriteLine("- Optional flags (with ?) can be omitted");
WriteLine("- Boolean flags are always optional");
WriteLine("- This enables gradual command interception with sensible defaults");