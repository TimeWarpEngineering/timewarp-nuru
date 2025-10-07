#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Test: Required Flag with Optional Value
// Pattern: --flag {value?}
// The flag itself is REQUIRED but its value is optional
// This is different from --flag? {value} where the flag is optional

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Required Flag with Optional Value (--flag {value?})
  ============================================================
  Pattern: build --output {path?}
  Expectation: --output flag MUST be present, but value is optional
  """
);

NuruAppBuilder builder = new();

// Test route with required flag that has optional value
builder.AddRoute("build --output {path?}", (string? path) =>
{
    if (path is not null)
    {
        WriteLine($"✓ Build with output to: {path}");
    }
    else
    {
        WriteLine("✓ Build with --output flag (no path specified)");
    }
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: build --output dist
  Expected: Match, path="dist"
  """
);

try
{
    await app.RunAsync(["build", "--output", "dist"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 2: build --output
  Expected: Match, path=null
  """
);

try
{
    await app.RunAsync(["build", "--output"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 3: build
  Expected: No match (--output flag is required)
  """
);

try
{
    await app.RunAsync(["build"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Expected error: {ex.Message}");
}

WriteLine
(
  """

  ========================================
  NOTE: This pattern means the FLAG is required,
        but its VALUE is optional.
        Different from --flag? where flag itself is optional.
  """
);