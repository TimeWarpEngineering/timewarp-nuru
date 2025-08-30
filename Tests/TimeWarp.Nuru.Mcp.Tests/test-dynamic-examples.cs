#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

using TimeWarp.Nuru.Mcp.Tools;
using static System.Console;

WriteLine("Testing Dynamic Example Discovery:");
WriteLine();

// Test listing examples (should try to fetch manifest)
WriteLine("Test 1: List all examples");
WriteLine(new string('-', 50));
string listResult = await GetExampleTool.ListExamplesAsync();
WriteLine(listResult);
WriteLine();

// Test getting a specific example
WriteLine("Test 2: Get basic example");
WriteLine(new string('-', 50));
string basicExample = await GetExampleTool.GetExampleAsync("basic");
WriteLine(basicExample.Length > 500 ?
    $"✅ Retrieved basic example ({basicExample.Length} characters)" :
    $"❌ Failed to retrieve example: {basicExample}");
WriteLine();

// Test cache status
WriteLine("Test 3: Check cache status");
WriteLine(new string('-', 50));
string cacheStatus = CacheManagementTool.CacheStatus();
WriteLine(cacheStatus);
WriteLine();

// Test that all examples in manifest can be retrieved
WriteLine("Test 4: Verify all examples are accessible");
WriteLine(new string('-', 50));
string[] exampleIds = ["basic", "async", "console-logging", "serilog", "mediator", "delegates"];
foreach (string id in exampleIds)
{
    string result = await GetExampleTool.GetExampleAsync(id);
    bool success = !result.StartsWith("Example", StringComparison.Ordinal) &&
                   !result.StartsWith("Error", StringComparison.Ordinal);
    WriteLine($"  {id}: {(success ? "✅" : "❌")}");
}

WriteLine();

// Test force refresh
WriteLine("Test 5: Test force refresh");
WriteLine(new string('-', 50));
string refreshedExample = await GetExampleTool.GetExampleAsync("basic", forceRefresh: true);
WriteLine(refreshedExample.Length > 500 ?
    "✅ Force refresh successful" :
    "❌ Force refresh failed");
WriteLine();

WriteLine("All tests completed!");