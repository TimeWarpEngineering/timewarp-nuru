#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Test: Optional Flag Syntax Support
// Tests the --flag? syntax where flags can be marked as optional using ?
// This allows a single route to handle all combinations of optional flags
// instead of requiring multiple route definitions

using TimeWarp.Nuru;

Console.WriteLine("Testing Optional Flag Syntax (--flag?) Support");
Console.WriteLine("==============================================");
Console.WriteLine();

// Test 1: Build command with optional verbose flag
Console.WriteLine("Test 1: Build command with optional --verbose? flag");
NuruApp app1 = new NuruAppBuilder()
    .AddRoute("build --verbose?", (bool verbose) =>
    {
        Console.WriteLine($"✓ Build executed with verbose={verbose}");
    })
    .Build();

Console.WriteLine("  1a. Testing: build");
await app1.RunAsync(["build"]);

Console.WriteLine("  1b. Testing: build --verbose");
await app1.RunAsync(["build", "--verbose"]);

Console.WriteLine();

// Test 2: Deploy command with optional dry-run flag
Console.WriteLine("Test 2: Deploy command with optional --dry-run? flag");
NuruApp app2 = new NuruAppBuilder()
    .AddRoute("deploy {env} --dry-run?", (string env, bool dryRun) =>
    {
        Console.WriteLine($"✓ Deploy to {env} with dry-run={dryRun}");
    })
    .Build();

Console.WriteLine("  2a. Testing: deploy prod");
await app2.RunAsync(["deploy", "prod"]);

Console.WriteLine("  2b. Testing: deploy prod --dry-run");
await app2.RunAsync(["deploy", "prod", "--dry-run"]);

Console.WriteLine();

// Test 3: Multiple optional flags
Console.WriteLine("Test 3: Test command with multiple optional flags");
NuruApp app3 = new NuruAppBuilder()
    .AddRoute("test --verbose? --coverage? --watch?",
        (bool verbose, bool coverage, bool watch) =>
    {
        Console.WriteLine($"✓ Test: verbose={verbose}, coverage={coverage}, watch={watch}");
    })
    .Build();

Console.WriteLine("  3a. Testing: test");
await app3.RunAsync(["test"]);

Console.WriteLine("  3b. Testing: test --verbose");
await app3.RunAsync(["test", "--verbose"]);

Console.WriteLine("  3c. Testing: test --coverage --watch");
await app3.RunAsync(["test", "--coverage", "--watch"]);

Console.WriteLine("  3d. Testing: test --verbose --coverage --watch");
await app3.RunAsync(["test", "--verbose", "--coverage", "--watch"]);

Console.WriteLine();

// Test 4: Optional flag with value
Console.WriteLine("Test 4: Build command with optional --config? flag that takes a value");
NuruApp app4 = new NuruAppBuilder()
    .AddRoute("build --config? {mode}", (string? mode) =>
    {
        Console.WriteLine($"✓ Build with config={mode ?? "null"}");
    })
    .Build();

Console.WriteLine("  4a. Testing: build");
await app4.RunAsync(["build"]);

Console.WriteLine("  4b. Testing: build --config debug");
await app4.RunAsync(["build", "--config", "debug"]);

Console.WriteLine();

// Test 5: Mix of required and optional flags
Console.WriteLine("Test 5: Mixed required and optional flags");
NuruApp app5 = new NuruAppBuilder()
    .AddRoute("deploy {env} --version {ver} --force? --dry-run?",
        (string env, string ver, bool force, bool dryRun) =>
    {
        Console.WriteLine($"✓ Deploy {env} v{ver}: force={force}, dry-run={dryRun}");
    })
    .Build();

Console.WriteLine("  5a. Testing: deploy prod --version 1.0");
await app5.RunAsync(["deploy", "prod", "--version", "1.0"]);

Console.WriteLine("  5b. Testing: deploy prod --version 1.0 --force");
await app5.RunAsync(["deploy", "prod", "--version", "1.0", "--force"]);

Console.WriteLine("  5c. Testing: deploy prod --version 1.0 --dry-run");
await app5.RunAsync(["deploy", "prod", "--version", "1.0", "--dry-run"]);

Console.WriteLine("  5d. Testing: deploy prod --version 1.0 --force --dry-run");
await app5.RunAsync(["deploy", "prod", "--version", "1.0", "--force", "--dry-run"]);

Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine("Optional Flag Syntax Tests Complete!");
Console.WriteLine();
Console.WriteLine("NOTE: Some tests fail due to parameter binding not yet supporting");
Console.WriteLine("      optional flags (Phase 6). Route matching (Phase 3) works correctly.");