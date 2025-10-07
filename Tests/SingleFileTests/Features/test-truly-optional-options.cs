#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

WriteLine("Testing if boolean options are truly optional:");
WriteLine("===============================================");
WriteLine();

NuruAppBuilder builder = new();

// Test: Single route with multiple boolean options
builder.AddRoute("build --verbose --debug --optimize", (bool verbose, bool debug, bool optimize) =>
{
    WriteLine($"Build: verbose={verbose}, debug={debug}, optimize={optimize}");
});

NuruApp app = builder.Build();

// Test all combinations
WriteLine("Test 1: build (no options)");
try
{
    await app.RunAsync(["build"]);
    WriteLine("✓ Matched with no options!");
}
catch (InvalidOperationException)
{
    WriteLine("✗ Did not match without options");
}

WriteLine("\nTest 2: build --verbose");
try
{
    await app.RunAsync(["build", "--verbose"]);
    WriteLine("✓ Matched with one option!");
}
catch (InvalidOperationException)
{
    WriteLine("✗ Did not match with one option");
}

WriteLine("\nTest 3: build --debug --optimize");
try
{
    await app.RunAsync(["build", "--debug", "--optimize"]);
    WriteLine("✓ Matched with two options!");
}
catch (InvalidOperationException)
{
    WriteLine("✗ Did not match with two options");
}

WriteLine("\nTest 4: build --verbose --debug --optimize");
try
{
    await app.RunAsync(["build", "--verbose", "--debug", "--optimize"]);
    WriteLine("✓ Matched with all options!");
}
catch (InvalidOperationException)
{
    WriteLine("✗ Did not match with all options");
}