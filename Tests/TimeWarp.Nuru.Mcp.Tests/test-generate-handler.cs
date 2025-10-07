#!/usr/bin/dotnet --
#:package TimeWarp.Amuru
#:project ../../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

using TimeWarp.Nuru.Mcp.Tools;
using static System.Console;

WriteLine("Testing GenerateHandlerTool...\n");

// Test direct handler generation
WriteLine("=== Direct Handler Generation ===\n");

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
  WriteLine($"Pattern: {pattern}");
  WriteLine("Generated code:");
  WriteLine(GenerateHandlerTool.GenerateHandler(pattern, false));
  WriteLine();
}

// Test mediator handler generation
WriteLine("=== Mediator Handler Generation ===\n");

string[] mediatorPatterns = [
    "deploy {env}",
    "backup {source} {dest?}",
    "test {project} --verbose --filter {pattern}"
];

foreach (string pattern in mediatorPatterns)
{
  WriteLine($"Pattern: {pattern}");
  WriteLine("Generated code:");
  WriteLine(GenerateHandlerTool.GenerateHandler(pattern, true));
  WriteLine();
}

// Test error handling
WriteLine("=== Error Handling ===\n");

string[] invalidPatterns = [
    "{missing-literal}",
    "test {param {nested}",
    "invalid --"
];

foreach (string pattern in invalidPatterns)
{
  WriteLine($"Invalid pattern: {pattern}");
    string result = GenerateHandlerTool.GenerateHandler(pattern);
    if (result.Contains("// Error", StringComparison.Ordinal))
    {
    WriteLine("  ✅ Error handled correctly");
    }
    else
    {
    WriteLine("  ❌ Error not handled");
    }
}

WriteLine("\n✅ All GenerateHandlerTool tests completed!");