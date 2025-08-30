#!/usr/bin/dotnet --
#:package TimeWarp.Amuru
#:project ../../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

using TimeWarp.Amuru;
using TimeWarp.Nuru.Mcp.Tools;

Console.WriteLine("Testing GetSyntaxTool...\n");

// Test getting specific syntax elements
Console.WriteLine("=== Testing specific syntax elements ===\n");

string[] elements = ["literals", "parameters", "types", "optional", "catchall", "options", "descriptions"];

foreach (string element in elements)
{
    Console.WriteLine($"Testing element: {element}");
    string result = GetSyntaxTool.GetSyntax(element);

    if (string.IsNullOrEmpty(result))
    {
        Console.WriteLine($"  ❌ FAILED: Empty result for {element}");
    }
    else if (result.Contains("Unknown syntax element", StringComparison.Ordinal))
    {
        Console.WriteLine($"  ❌ FAILED: Element not found: {element}");
    }
    else
    {
        Console.WriteLine($"  ✅ Success: Got {result.Length} characters");
    }
}

// Test getting all syntax
Console.WriteLine("\n=== Testing 'all' element ===");
string allSyntax = GetSyntaxTool.GetSyntax("all");
if (allSyntax.Contains("Route Pattern Syntax Reference", StringComparison.Ordinal))
{
    Console.WriteLine("  ✅ Success: Got complete reference");
}
else
{
    Console.WriteLine("  ❌ FAILED: Missing complete reference");
}

// Test partial matching
Console.WriteLine("\n=== Testing partial matching ===");
string[] partialTests = ["param", "opt", "catch"];
foreach (string partial in partialTests)
{
    Console.WriteLine($"Testing partial: {partial}");
    string result = GetSyntaxTool.GetSyntax(partial);
    if (!result.Contains("Unknown syntax element", StringComparison.Ordinal))
    {
        Console.WriteLine("  ✅ Success: Partial match worked");
    }
    else
    {
        Console.WriteLine("  ❌ FAILED: Partial match failed");
    }
}

// Test GetPatternExamples
Console.WriteLine("\n=== Testing GetPatternExamples ===");
string[] features = ["basic", "typed", "optional", "catchall", "options", "complex"];

foreach (string feature in features)
{
    Console.WriteLine($"Testing feature: {feature}");
    string result = GetSyntaxTool.GetPatternExamples(feature);

    if (string.IsNullOrEmpty(result))
    {
        Console.WriteLine($"  ❌ FAILED: Empty result for {feature}");
    }
    else if (result.Contains("Unknown feature", StringComparison.Ordinal))
    {
        Console.WriteLine($"  ❌ FAILED: Feature not found: {feature}");
    }
    else if (result.Contains("```csharp", StringComparison.Ordinal))
    {
        Console.WriteLine("  ✅ Success: Got examples with code blocks");
    }
    else
    {
        Console.WriteLine("  ❌ FAILED: Missing code blocks in examples");
    }
}

// Test unknown element
Console.WriteLine("\n=== Testing unknown element ===");
string unknownResult = GetSyntaxTool.GetSyntax("foobar");
if (unknownResult.Contains("Unknown syntax element", StringComparison.Ordinal) && unknownResult.Contains("Available elements", StringComparison.Ordinal))
{
    Console.WriteLine("  ✅ Success: Proper error message for unknown element");
}
else
{
    Console.WriteLine("  ❌ FAILED: Improper error handling");
}

Console.WriteLine("\n✅ All GetSyntaxTool tests completed!");