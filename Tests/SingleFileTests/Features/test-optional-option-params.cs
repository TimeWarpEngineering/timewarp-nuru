#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

WriteLine("Testing optional option parameters:");
WriteLine("===================================");
WriteLine();

NuruAppBuilder builder = new();

// Test 1: Option with optional string parameter
builder.AddRoute("build --config {mode?}", (string? mode) =>
{
    if (mode is null)
        WriteLine("✓ Test 1: Build with --config (no value provided)");
    else
        WriteLine($"✓ Test 1: Build with --config {mode}");
});

// Test 2: Option with optional typed parameter
builder.AddRoute("serve --port {port:int?}", (int? port) =>
{
    if (port is null)
        WriteLine("✓ Test 2: Serve with --port (no value provided)");
    else
        WriteLine($"✓ Test 2: Serve on port {port}");
});

// Test 3: Multiple options with optional parameters
builder.AddRoute("deploy --env {environment?} --version {ver?}", (string? environment, string? ver) =>
{
    WriteLine($"✓ Test 3: Deploy - env: {environment ?? "(none)"}, version: {ver ?? "(none)"}");
});

// Test 4: Mix of required and optional option parameters
builder.AddRoute("backup --source {src} --dest {dst?}", (string src, string? dst) =>
{
    WriteLine($"✓ Test 4: Backup from {src} to {dst ?? "(default location)"}");
});

// Test 5: Boolean option (inherently optional)
builder.AddRoute("test --verbose", (bool verbose) =>
{
    WriteLine($"✓ Test 5: Test with verbose={verbose}");
});

// Test 6: Option that doesn't exist in route should not match
builder.AddRoute("simple", () =>
{
    WriteLine("✓ Test 6: Simple command (no options)");
});

NuruApp app = builder.Build();

WriteLine("Test 1a: build --config debug");
await app.RunAsync(["build", "--config", "debug"]);

WriteLine("\nTest 1b: build --config (no value after --config)");
try
{
    await app.RunAsync(["build", "--config"]);
}
catch (InvalidOperationException ex)
{
    WriteLine($"  Expected behavior - {ex.Message}");
}

WriteLine("\nTest 1c: build (without --config at all)");
try
{
    await app.RunAsync(["build"]);
}
catch (InvalidOperationException ex)
{
    WriteLine($"  ERROR: Route should match without optional option - {ex.Message}");
}

WriteLine("\nTest 2a: serve --port 8080");
await app.RunAsync(["serve", "--port", "8080"]);

WriteLine("\nTest 2b: serve --port (no value)");
try
{
    await app.RunAsync(["serve", "--port"]);
}
catch (InvalidOperationException ex)
{
    WriteLine($"  Expected behavior - {ex.Message}");
}

WriteLine("\nTest 2c: serve (without --port)");
try
{
    await app.RunAsync(["serve"]);
}
catch (InvalidOperationException ex)
{
    WriteLine($"  ERROR: Route should match without optional option - {ex.Message}");
}

WriteLine("\nTest 3a: deploy --env production --version v1.2.3");
await app.RunAsync(["deploy", "--env", "production", "--version", "v1.2.3"]);

WriteLine("\nTest 3b: deploy --env staging");
await app.RunAsync(["deploy", "--env", "staging"]);

WriteLine("\nTest 3c: deploy --version v2.0.0");
await app.RunAsync(["deploy", "--version", "v2.0.0"]);

WriteLine("\nTest 3d: deploy (no options)");
try
{
    await app.RunAsync(["deploy"]);
}
catch (InvalidOperationException ex)
{
    WriteLine($"  ERROR: Route should match without optional options - {ex.Message}");
}

WriteLine("\nTest 4a: backup --source /data --dest /backup");
await app.RunAsync(["backup", "--source", "/data", "--dest", "/backup"]);

WriteLine("\nTest 4b: backup --source /data (optional dest omitted)");
await app.RunAsync(["backup", "--source", "/data"]);

WriteLine("\nTest 4c: backup (missing required --source)");
try
{
    await app.RunAsync(["backup"]);
}
catch (InvalidOperationException ex)
{
    WriteLine($"  Expected error for missing required option - {ex.Message}");
}

WriteLine("\nTest 5a: test --verbose");
await app.RunAsync(["test", "--verbose"]);

WriteLine("\nTest 5b: test (without --verbose)");
await app.RunAsync(["test"]);

WriteLine("\nTest 6: simple");
await app.RunAsync(["simple"]);

WriteLine("\nTest 7: Trying to use --config on 'simple' command (should fail)");
try
{
    await app.RunAsync(["simple", "--config", "debug"]);
}
catch (InvalidOperationException ex)
{
    WriteLine($"  Expected - no matching route: {ex.Message}");
}

WriteLine("\n============================");
WriteLine("Optional option tests complete!");
WriteLine("\nKey findings:");
WriteLine("- Options with optional parameters require the option flag to be present");
WriteLine("- If the option flag is present, the parameter value may be optional");
WriteLine("- To make the entire option optional, just omit it from some routes");
WriteLine("- Boolean options are inherently optional (present = true, absent = false)");