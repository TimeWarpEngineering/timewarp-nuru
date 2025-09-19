#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Repeated Options (--flag {value}*)
  ===========================================
  Pattern: docker run --env {var}*
  Expectation: Can specify --env multiple times
  """
);

NuruAppBuilder builder = new();

// Test route with repeated option
builder.AddRoute("docker run --env {var}*", (string[] vars) =>
{
    WriteLine($"✓ Docker run executed with {vars.Length} environment variables:");
    foreach (var v in vars)
    {
        WriteLine($"  - {v}");
    }
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: docker run
  Expected: Match with empty array
  """
);
try
{
    await app.RunAsync(["docker", "run"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Currently fails because * syntax not implemented yet");
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
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Currently fails because * syntax not implemented yet");
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
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Currently fails because * syntax not implemented yet");
}

WriteLine
(
  """

  ========================================
  Summary:
  This test demonstrates the --flag {value}* pattern where:
  - The same flag can be specified multiple times
  - All values are collected into an array
  - Useful for environment variables, volumes, ports, etc.
  """
);