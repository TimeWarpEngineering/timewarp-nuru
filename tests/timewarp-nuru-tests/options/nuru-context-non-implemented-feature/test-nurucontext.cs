#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing NuruContext Access
  ==========================
  Pattern: Various patterns with NuruContext parameter
  Expectation: Access to raw args, unmatched args, metadata
  """
);

NuruAppBuilder builder = new();

// Test route with NuruContext
builder.Map("analyze {file} --verbose", (string file, bool verbose, NuruContext context) =>
{
    WriteLine($"✓ Analyze executed:");
    WriteLine($"  File: {file}");
    WriteLine($"  Verbose: {verbose}");
    WriteLine($"  Raw Args: [{string.Join(", ", context.RawArgs.Select(a => $"\"{a}\""))}]");
    WriteLine($"  Unmatched Args: [{string.Join(", ", context.UnmatchedArgs.Select(a => $"\"{a}\""))}]");
    WriteLine($"  Route Pattern: {context.RoutePattern}");
});

// Test route with optional params and context
builder.Map("deploy {env} {tag?}", (string env, string? tag, NuruContext context) =>
{
    WriteLine($"✓ Deploy executed:");
    WriteLine($"  Environment: {env}");
    WriteLine($"  Tag: {tag ?? "(latest)"}");
    WriteLine($"  Was Tag Provided: {context.WasProvided("tag")}");
    WriteLine($"  All Provided Params: {string.Join(", ", context.ProvidedParameters)}");
});

NuruCoreApp app = builder.Build();

WriteLine
(
  """

  Test 1: analyze src/main.cs --verbose --debug --trace
  Expected: Context shows unmatched args [--debug, --trace]
  """
);
try
{
    await app.RunAsync(["analyze", "src/main.cs", "--verbose", "--debug", "--trace"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  NuruContext not implemented yet");
}

WriteLine
(
  """

  Test 2: deploy staging
  Expected: Context shows tag was not provided
  """
);
try
{
    await app.RunAsync(["deploy", "staging"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  NuruContext not implemented yet");
}

WriteLine
(
  """

  Test 3: deploy production v2.0
  Expected: Context shows tag was provided
  """
);
try
{
    await app.RunAsync(["deploy", "production", "v2.0"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  NuruContext not implemented yet");
}

WriteLine
(
  """

  ========================================
  Summary:
  NuruContext provides access to:
  - Raw command line arguments
  - Unmatched/extra arguments
  - Which optional parameters were actually provided
  - Route metadata and pattern info
  - Useful for gradual migration and debugging
  """
);