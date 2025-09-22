#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using System.Globalization;
using System.IO;
using TimeWarp.Nuru;
using static System.Console;

// Test descriptions
NuruApp app = new NuruAppBuilder()
    .AddRoute("hello {name|Your name} --upper,-u|Convert to uppercase",
        (string name, bool upper) => WriteLine(upper ? name.ToUpper(CultureInfo.InvariantCulture) : name))
    .AddRoute("hello {name|Your name}",
        (string name) => WriteLine(name))
    .AddAutoHelp()
    .Build();

int failures = 0;

// Test 1a: Route with short form works
try
{
    using StringWriter output = new();
    TextWriter originalOut = Console.Out;
    Console.SetOut(output);

    await app.RunAsync(["hello", "World", "-u"]);
    string result = output.ToString();

    Console.SetOut(originalOut);

    if (result.Contains("WORLD", StringComparison.Ordinal))
    {
        WriteLine("✓ Test 1a: Route with short form (-u) works");
    }
    else
    {
        WriteLine($"✗ Test 1a: Expected 'WORLD' but got: {result}");
        failures++;
    }
}
catch (Exception ex)
{
    WriteLine($"✗ Test 1a: Route failed: {ex.Message}");
    failures++;
}

// Test 1b: Route with long form works
try
{
    using StringWriter output = new();
    TextWriter originalOut = Console.Out;
    Console.SetOut(output);

    await app.RunAsync(["hello", "World", "--upper"]);
    string result = output.ToString();

    Console.SetOut(originalOut);

    if (result.Contains("WORLD", StringComparison.Ordinal))
    {
        WriteLine("✓ Test 1b: Route with long form (--upper) works");
    }
    else
    {
        WriteLine($"✗ Test 1b: Expected 'WORLD' but got: {result}");
        failures++;
    }
}
catch (Exception ex)
{
    WriteLine($"✗ Test 1b: Route failed: {ex.Message}");
    failures++;
}

// Test 2: Verify descriptions appear in help
try
{
    using StringWriter output = new();
    TextWriter originalOut = Console.Out;
    Console.SetOut(output);

    await app.RunAsync(["--help"]);
    string helpOutput = output.ToString();

    Console.SetOut(originalOut);

    if (helpOutput.Contains("Your name", StringComparison.Ordinal) && helpOutput.Contains("Convert to uppercase", StringComparison.Ordinal))
    {
        WriteLine("✓ Test 2: Descriptions appear in help output");
    }
    else
    {
        WriteLine("✗ Test 2: Descriptions missing from help output");
        WriteLine($"Help output: {helpOutput}");
        failures++;
    }
}
catch (Exception ex)
{
    WriteLine($"✗ Test 2: Help failed: {ex.Message}");
    failures++;
}

return failures;