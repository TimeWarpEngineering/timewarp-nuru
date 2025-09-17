#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

WriteLine("Testing mixed option types (boolean vs value options):");
WriteLine("======================================================");
WriteLine();

NuruAppBuilder builder = new();

// Test: Mix of boolean options and value options
builder.AddRoute("deploy --dry-run --env {environment} --force",
    (bool dryRun, string environment, bool force) =>
{
    WriteLine($"Deploy: dry-run={dryRun}, env={environment}, force={force}");
});

NuruApp app = builder.Build();

WriteLine("Test 1: deploy --env production");
try
{
    await app.RunAsync(["deploy", "--env", "production"]);
    WriteLine("✓ Matched with required value option only");
}
catch (InvalidOperationException)
{
    WriteLine("✗ Did not match - seems --env is required");
}

WriteLine("\nTest 2: deploy --dry-run --env staging");
try
{
    await app.RunAsync(["deploy", "--dry-run", "--env", "staging"]);
    WriteLine("✓ Matched with one boolean + required value option");
}
catch (InvalidOperationException)
{
    WriteLine("✗ Did not match");
}

WriteLine("\nTest 3: deploy --env production --force");
try
{
    await app.RunAsync(["deploy", "--env", "production", "--force"]);
    WriteLine("✓ Matched with required value option + one boolean");
}
catch (InvalidOperationException)
{
    WriteLine("✗ Did not match");
}

WriteLine("\nTest 4: deploy --dry-run --env production --force");
try
{
    await app.RunAsync(["deploy", "--dry-run", "--env", "production", "--force"]);
    WriteLine("✓ Matched with all options");
}
catch (InvalidOperationException)
{
    WriteLine("✗ Did not match");
}

WriteLine("\nTest 5: deploy (missing required --env)");
try
{
    await app.RunAsync(["deploy"]);
    WriteLine("✗ UNEXPECTED: Matched without required --env option!");
}
catch (InvalidOperationException)
{
    WriteLine("✓ Correctly failed - --env is required");
}

WriteLine("\nTest 6: deploy --dry-run --force (missing required --env)");
try
{
    await app.RunAsync(["deploy", "--dry-run", "--force"]);
    WriteLine("✗ UNEXPECTED: Matched without required --env option!");
}
catch (InvalidOperationException)
{
    WriteLine("✓ Correctly failed - --env is required");
}