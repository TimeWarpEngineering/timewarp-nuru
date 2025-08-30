#!/usr/bin/dotnet --
#:package TimeWarp.Amuru
#:project ../../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

using TimeWarp.Nuru.Mcp.Tools;

Console.WriteLine("Testing GenerateHandlerTool...\n");

// Test direct handler generation
Console.WriteLine("=== Direct Handler Generation ===\n");

string[] testPatterns = [
    "status",
    "greet {name}",
    "deploy {env} {tag?}",
    "wait {seconds:int}",
    "build --verbose",
    "test {project} --filter {pattern}",
    "docker {*args}",
    "backup {source} --output,-o {dest} --compress,-c"
];

foreach (string pattern in testPatterns)
{
    Console.WriteLine($"Pattern: {pattern}");
    Console.WriteLine("Generated code:");
    Console.WriteLine(GenerateHandlerTool.GenerateHandler(pattern, false));
    Console.WriteLine();
}

// Test mediator handler generation
Console.WriteLine("=== Mediator Handler Generation ===\n");

string[] mediatorPatterns = [
    "deploy {env}",
    "backup {source} {dest?}",
    "test {project} --verbose --filter {pattern}"
];

foreach (string pattern in mediatorPatterns)
{
    Console.WriteLine($"Pattern: {pattern}");
    Console.WriteLine("Generated code:");
    Console.WriteLine(GenerateHandlerTool.GenerateHandler(pattern, true));
    Console.WriteLine();
}

// Test error handling
Console.WriteLine("=== Error Handling ===\n");

string[] invalidPatterns = [
    "{missing-literal}",
    "test {param {nested}",
    "invalid --"
];

foreach (string pattern in invalidPatterns)
{
    Console.WriteLine($"Invalid pattern: {pattern}");
    string result = GenerateHandlerTool.GenerateHandler(pattern);
    if (result.Contains("// Error", StringComparison.Ordinal))
    {
        Console.WriteLine("  ✅ Error handled correctly");
    }
    else
    {
        Console.WriteLine("  ❌ Error not handled");
    }
}

Console.WriteLine("\n✅ All GenerateHandlerTool tests completed!");