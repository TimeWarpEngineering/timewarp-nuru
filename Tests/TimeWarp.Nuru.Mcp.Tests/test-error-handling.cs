#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

using System;
using System.Threading.Tasks;
using TimeWarp.Nuru.Mcp.Tools;
using static System.Console;

WriteLine("Testing ErrorHandlingTool...\n");

// Test GetErrorHandlingInfoAsync with different areas
WriteLine("=== Testing GetErrorHandlingInfoAsync with different areas ===\n");

string[] areas = ["overview", "architecture", "philosophy"];
foreach (string area in areas)
{
    WriteLine($"Testing area: {area}");
    string result = await ErrorHandlingTool.GetErrorHandlingInfoAsync(area);

    if (string.IsNullOrEmpty(result))
    {
        WriteLine($"  ❌ FAILED: Empty result for {area}");
    }
    else if (result.Contains("Unknown area", StringComparison.Ordinal))
    {
        if (area == "invalid-area")
        {
            WriteLine("  ✅ Success: Properly handled invalid area");
        }
        else
        {
            WriteLine($"  ❌ FAILED: Area not found: {area}");
        }
    }
    else
    {
        WriteLine($"  ✅ Success: Got {result.Length} characters");
    }
}

// Test GetErrorScenariosAsync with different scenarios
WriteLine("\n=== Testing GetErrorScenariosAsync with different scenarios ===\n");

string[] scenarios = ["parsing", "binding", "conversion", "execution", "matching", "all"];
foreach (string scenario in scenarios)
{
    WriteLine($"Testing scenario: {scenario}");
    string result = await ErrorHandlingTool.GetErrorScenariosAsync(scenario);

    if (string.IsNullOrEmpty(result))
    {
        WriteLine($"  ❌ FAILED: Empty result for {scenario}");
    }
    else if (result.Contains("Unknown scenario", StringComparison.Ordinal))
    {
        if (scenario == "invalid-scenario")
        {
            WriteLine("  ✅ Success: Properly handled invalid scenario");
        }
        else
        {
            WriteLine($"  ❌ FAILED: Scenario not found: {scenario}");
        }
    }
    else
    {
        WriteLine($"  ✅ Success: Got {result.Length} characters");
    }
}

// Test GetErrorHandlingBestPracticesAsync
WriteLine("\n=== Testing GetErrorHandlingBestPracticesAsync ===\n");

string bestPractices = await ErrorHandlingTool.GetErrorHandlingBestPracticesAsync();
if (string.IsNullOrEmpty(bestPractices))
{
    WriteLine("  ❌ FAILED: Empty result for best practices");
}
else if (bestPractices.Contains("Error retrieving", StringComparison.Ordinal))
{
    // Now that the documentation is available on GitHub, we should be able to fetch it
    WriteLine($"  ❌ FAILED: Could not fetch documentation from GitHub: {bestPractices}");
}
else
{
    WriteLine($"  ✅ Success: Got {bestPractices.Length} characters");
}

// Test error handling for invalid inputs
WriteLine("\n=== Testing error handling for invalid inputs ===\n");

// Test invalid area
WriteLine("Testing invalid area:");
string invalidAreaResult = await ErrorHandlingTool.GetErrorHandlingInfoAsync("invalid-area");
if (invalidAreaResult.Contains("Unknown area", StringComparison.Ordinal))
{
    WriteLine("  ✅ Success: Properly handled invalid area");
}
else
{
    WriteLine("  ❌ FAILED: Did not properly handle invalid area");
}

// Test invalid scenario
WriteLine("\nTesting invalid scenario:");
string invalidScenarioResult = await ErrorHandlingTool.GetErrorScenariosAsync("invalid-scenario");
if (invalidScenarioResult.Contains("Unknown scenario", StringComparison.Ordinal) ||
    invalidScenarioResult.Contains("Available scenarios", StringComparison.Ordinal))
{
    // This is expected behavior when the scenario is invalid
    WriteLine("  ✅ Success: Properly handled invalid scenario");
    WriteLine($"  Response: {invalidScenarioResult}");
}
else if (invalidScenarioResult.Contains("Failed to fetch documentation from GitHub", StringComparison.Ordinal))
{
    // Now that the documentation is available on GitHub, we should be able to fetch it
    WriteLine("  ❌ FAILED: Could not fetch documentation from GitHub");
    WriteLine($"  Response: {invalidScenarioResult}");
}
else
{
    WriteLine("  ❌ FAILED: Did not properly handle invalid scenario");
    WriteLine($"  Response: {invalidScenarioResult}");
}

// Test caching functionality
WriteLine("\n=== Testing caching functionality ===\n");

// First call to prime the cache
WriteLine("First call to prime the cache:");
string firstCall = await ErrorHandlingTool.GetErrorHandlingInfoAsync("overview");
WriteLine($"  Got {firstCall.Length} characters");

// Second call should use the cache
WriteLine("\nSecond call should use the cache:");
string secondCall = await ErrorHandlingTool.GetErrorHandlingInfoAsync("overview");
WriteLine($"  Got {secondCall.Length} characters");

// Verify both calls return the same content
if (firstCall == secondCall)
{
    WriteLine("  ✅ Success: Cache returned consistent results");
}
else
{
    WriteLine("  ❌ FAILED: Cache returned inconsistent results");
}

// Test force refresh
WriteLine("\nTesting force refresh:");
string forcedRefresh = await ErrorHandlingTool.GetErrorHandlingInfoAsync("overview", true);
WriteLine($"  Got {forcedRefresh.Length} characters");

// The content should still be the same even with forced refresh
if (firstCall == forcedRefresh)
{
    WriteLine("  ✅ Success: Forced refresh returned consistent results");
}
else
{
    WriteLine("  ❌ FAILED: Forced refresh returned inconsistent results");
}

WriteLine("\n✅ All ErrorHandlingTool tests completed!");
