#!/usr/bin/dotnet --

// Test optional flag alias with descriptions (issue reproduction)
using TimeWarp.Nuru.Parsing;

Console.WriteLine("Testing optional flag alias WITH descriptions");
Console.WriteLine("==============================================\n");

// Test 1: Without description (baseline - should work)
try
{
    CompiledRoute route1 = PatternParser.Parse("test --output,-o? {file}");
    Console.WriteLine("✓ Test 1 PASSED: --output,-o? {file}");
}
catch (Exception ex)
{
    Console.WriteLine("✗ Test 1 FAILED: --output,-o? {file}");
    Console.WriteLine("  Error: " + ex.Message + "\n");
}

// Test 2: With description on parameter (real-world case)
try
{
    CompiledRoute route2 = PatternParser.Parse("test --output,-o? {file|Save to file}");
    Console.WriteLine("✓ Test 2 PASSED: --output,-o? {file|Save to file}");
}
catch (Exception ex)
{
    Console.WriteLine("✗ Test 2 FAILED: --output,-o? {file|Save to file}");
    Console.WriteLine("  Error: " + ex.Message + "\n");
}

// Test 3: With optional parameter and description (user's exact case)
try
{
    CompiledRoute route3 = PatternParser.Parse("test --output,-o? {file?|Save SVG to file}");
    Console.WriteLine("✓ Test 3 PASSED: --output,-o? {file?|Save SVG to file}");
}
catch (Exception ex)
{
    Console.WriteLine("✗ Test 3 FAILED: --output,-o? {file?|Save SVG to file}");
    Console.WriteLine("  Error: " + ex.Message + "\n");
}

// Test 4: Full complex pattern from user
try
{
    CompiledRoute route4 = PatternParser.Parse(
        "{input|Text to generate} " +
        "--output,-o? {file?|Save SVG} " +
        "--no-env|Generate without circle"
    );
    Console.WriteLine("✓ Test 4 PASSED: Full user pattern");
}
catch (Exception ex)
{
    Console.WriteLine("✗ Test 4 FAILED: Full user pattern");
    Console.WriteLine("  Error: " + ex.Message + "\n");
}
