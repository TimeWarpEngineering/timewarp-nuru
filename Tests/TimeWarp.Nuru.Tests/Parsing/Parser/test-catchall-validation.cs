#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

using static System.Console;
using TimeWarp.Nuru.Parsing;

WriteLine("Testing catch-all validation in options:");

try
{
  CompiledRoute route = RoutePatternParser.Parse("test --exclude {*pattern}");
  WriteLine("✗ UNEXPECTED: Should have failed - catch-all in option!");
}
catch (Exception ex)
{
  WriteLine($"✓ Correctly failed: {ex.Message}");
}

try
{
  CompiledRoute route = RoutePatternParser.Parse("test --exclude {pattern}*");
  WriteLine("✓ Correctly parsed: repeated option parameter");
}
catch (Exception ex)
{
  WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine("\nTesting optional catch-all syntax:");

try
{
  CompiledRoute route = RoutePatternParser.Parse("git add {*files?}");
  WriteLine("✓ Parsed {*files?}: optional catch-all syntax accepted by parser");
}
catch (Exception ex)
{
  WriteLine($"✗ Failed to parse {{*files?}}: {ex.Message}");
}