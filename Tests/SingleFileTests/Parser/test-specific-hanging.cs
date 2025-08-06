#!/usr/bin/dotnet --
#:project ../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

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
    WriteLine("SUCCESS: Pattern parsed without hanging!");
}
catch (ArgumentException ex)
{
    WriteLine($"ERROR (expected): {ex.Message}");
}
catch (Exception ex)
{
    WriteLine($"UNEXPECTED ERROR: {ex.GetType().Name}: {ex.Message}");
}