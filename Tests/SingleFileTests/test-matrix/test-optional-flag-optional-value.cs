#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Optional Flag with Optional Value (--flag? {value?})
  =============================================================

  Pattern: --config? {mode?}
  Expectation: Both flag and value are optional
  """
);

NuruAppBuilder builder = new();

// Test route with optional flag and optional value
builder.AddRoute("build --config? {mode?}", (string? mode) =>
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
  Expected: Match with mode=null
  """
);
try
{
    await app.RunAsync(["build", "--config"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Currently fails because --config? syntax not implemented yet");
}

WriteLine
(
  """

  ========================================
  Summary:
  This test demonstrates the --flag? {value?} pattern where:
  - The flag itself is optional (can be omitted entirely)
  - The value is also optional (flag can be present without value)
  - This is the most flexible pattern, useful for optional modes/levels
  """
);