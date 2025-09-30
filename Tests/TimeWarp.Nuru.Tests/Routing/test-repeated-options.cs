#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Repeated Options (--flag {value}*)
  ===========================================
  Pattern: docker run --env {vars}*
  Expectation: --env is REQUIRED and can be specified multiple times
  """
);

NuruAppBuilder builder = new();

// Test route with repeated option
builder.AddRoute("docker run --env {vars}*", (string[] vars) =>
{
    WriteLine($"✓ Docker run executed with {vars.Length} environment variables:");
    foreach (string v in vars)
    {
        WriteLine($"  - {v}");
    }
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: docker run
  Expected: No match (--env is required)
  """
);
int exitCode1 = await app.RunAsync(["docker", "run"]);
if (exitCode1 == 1)
{
    WriteLine("✓ PASSED: Correctly rejected - --env is required");
}
else
{
    WriteLine("✗ FAILED: Should not have matched without --env");
}

WriteLine
(
  """

  Test 2: docker run --env PATH=/usr/bin
  Expected: Match with ["PATH=/usr/bin"]
  """
);
try
{
    await app.RunAsync(["docker", "run", "--env", "PATH=/usr/bin"]);
    WriteLine("  (Handler output above shows success)");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  Test 3: docker run --env USER=john --env HOME=/home/john --env SHELL=/bin/bash
  Expected: Match with ["USER=john", "HOME=/home/john", "SHELL=/bin/bash"]
  """
);
try
{
    await app.RunAsync(["docker", "run", "--env", "USER=john", "--env", "HOME=/home/john", "--env", "SHELL=/bin/bash"]);
    WriteLine("  (Handler output above shows success)");
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
  This test demonstrates the --flag {value}* pattern where:
  - The flag is REQUIRED (no ? modifier)
  - The same flag can be specified multiple times (1 or more)
  - All values are collected into an array
  - Useful for environment variables, volumes, ports, etc.

  Note: For optional repeated flags, use --flag? {value}*
        This allows zero or more occurrences.
  """
);