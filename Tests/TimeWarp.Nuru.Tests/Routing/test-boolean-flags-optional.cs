#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Boolean Flags
  =====================
  Pattern: test --verbose --debug
  Expectation: Boolean flags should always be optional
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
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Boolean flags should be optional per design");
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
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Boolean flags should be optional per design");
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
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Boolean flags should be optional per design");
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
  Per design documentation, boolean flags should always be optional.
  Tests demonstrate that all flags in the pattern must be present.
  """
);