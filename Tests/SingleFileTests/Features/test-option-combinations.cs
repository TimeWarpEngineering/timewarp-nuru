#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

WriteLine("Testing if ONE route handles all option combinations:");
WriteLine("=====================================================");
WriteLine();

NuruAppBuilder builder = new();

// Single route with 4 optional option groups
builder.AddRoute("deploy --env {env?} --version {ver?} --config {cfg?} --force",
    (string? env, string? ver, string? cfg, bool force) =>
{
    WriteLine("✓ MATCHED! Deploy executed with:");
    WriteLine($"  - Environment: {env ?? "(not provided)"}");
    WriteLine($"  - Version: {ver ?? "(not provided)"}");
    WriteLine($"  - Config: {cfg ?? "(not provided)"}");
    WriteLine($"  - Force: {force}");
});

// Also add a fallback to see what doesn't match
builder.AddRoute("deploy", () =>
{
    WriteLine("✗ Fell back to plain 'deploy' route (options didn't match)");
});

NuruApp app = builder.Build();

// Test a few key combinations
WriteLine("Test 1: deploy");
WriteLine("--------------");
await app.RunAsync(["deploy"]);

WriteLine("\nTest 2: deploy --force");
WriteLine("----------------------");
await app.RunAsync(["deploy", "--force"]);

WriteLine("\nTest 3: deploy --env prod");
WriteLine("--------------------------");
await app.RunAsync(["deploy", "--env", "prod"]);

WriteLine("\nTest 4: deploy --env prod --force");
WriteLine("----------------------------------");
await app.RunAsync(["deploy", "--env", "prod", "--force"]);

WriteLine("\nTest 5: deploy --version v1.0 --config prod.json");
WriteLine("-------------------------------------------------");
await app.RunAsync(["deploy", "--version", "v1.0", "--config", "prod.json"]);

WriteLine("\nTest 6: deploy --env staging --version v2.0 --config staging.json --force");
WriteLine("--------------------------------------------------------------------------");
await app.RunAsync(["deploy", "--env", "staging", "--version", "v2.0", "--config", "staging.json", "--force"]);

WriteLine("\n" + new string('=', 60));
WriteLine("Analysis:");
WriteLine("If all tests show '✓ MATCHED!', then ONE route handles all combinations!");
WriteLine("If any show '✗ Fell back', then we'd need multiple routes.");