#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CA1303 // Do not pass literals to WriteLine

using TimeWarp.Nuru.Parsing;

Console.WriteLine("Testing catch-all validation in options:");

try
{
    CompiledRoute route = RoutePatternParser.Parse("test --exclude {*pattern}");
    Console.WriteLine("✗ UNEXPECTED: Should have failed - catch-all in option!");
}
catch (Exception ex)
{
    Console.WriteLine($"✓ Correctly failed: {ex.Message}");
}

try
{
    CompiledRoute route = RoutePatternParser.Parse("test --exclude {pattern}*");
    Console.WriteLine("✓ Correctly parsed: repeated option parameter");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ FAILED: {ex.Message}");
}