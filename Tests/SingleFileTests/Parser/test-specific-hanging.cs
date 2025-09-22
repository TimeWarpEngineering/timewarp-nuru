#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru.Parsing;
using static System.Console;

// Enable debug output
Environment.SetEnvironmentVariable("NURU_DEBUG", "true");

WriteLine("Testing specific hanging patterns one at a time...");
WriteLine("Run with: ./test-specific-hanging.cs [pattern]");
WriteLine();

string pattern = args.Length > 0 ? args[0] : "build config}";

WriteLine($"Testing pattern: '{pattern}'");
WriteLine();

try
{
    CompiledRoute route = RoutePatternParser.Parse(pattern);
    WriteLine("FAIL: Pattern parsed without error (should have thrown ArgumentException)!");
    return 1; // Test failed - should have thrown
}
catch (ArgumentException ex)
{
    WriteLine($"PASS: Got expected error: {ex.Message}");
    return 0; // Test passed - got expected exception
}
catch (Exception ex)
{
    WriteLine($"FAIL: Wrong exception type: {ex.GetType().Name}: {ex.Message}");
    return 1; // Test failed - wrong exception type
}