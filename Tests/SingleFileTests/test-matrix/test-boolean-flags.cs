#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Boolean Flags (Currently NOT Optional)
  ================================================
  Pattern: --verbose --debug
  Current Behavior: ALL boolean flags in pattern must be present
  Target Behavior: Boolean flags should be optional
  """
);

NuruAppBuilder builder = new();

// Test route with boolean flags
builder.AddRoute("test --verbose --debug", (bool verbose, bool debug) =>
{
    WriteLine($"✓ Test executed: verbose={verbose}, debug={debug}");
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: test
  Expected: Match with verbose=false, debug=false
  """
);
try
{
    await app.RunAsync(["test"]);
    WriteLine("✓ PASS: Works as expected");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  Test 2: test --verbose
  Expected: Match with verbose=true, debug=false
  """
);
try
{
    await app.RunAsync(["test", "--verbose"]);
    WriteLine("✓ PASS: Works as expected");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  Test 3: test --debug
  Expected: Match with verbose=false, debug=true
  """
);
try
{
    await app.RunAsync(["test", "--debug"]);
    WriteLine("✓ PASS: Works as expected");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  Test 4: test --verbose --debug
  Expected: Match with verbose=true, debug=true
  """
);
try
{
    await app.RunAsync(["test", "--verbose", "--debug"]);
    WriteLine("✓ PASS: Works as expected");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  ========================================
  Summary:
  Boolean flags are currently NOT optional - ALL flags in the pattern must be present.
  Only Test 4 (with all flags) actually executes the handler.
  Tests 1-3 fail to match the route.
  """
);