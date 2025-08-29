#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

using TimeWarp.Nuru.Mcp.Tools;
using static System.Console;

WriteLine("Testing ValidateRouteTool:");
WriteLine();

// Test various patterns
TestPattern("status");
TestPattern("git commit");
TestPattern("deploy {env}");
TestPattern("deploy {env} {tag?}");
TestPattern("delay {ms:int}");
TestPattern("docker {*args}");
TestPattern("build --verbose");
TestPattern("build --config {mode}");
TestPattern("deploy {env|Environment} --dry-run,-d|Preview");

// Test invalid patterns
WriteLine("\nTesting invalid patterns:");
TestPattern("deploy {env");
TestPattern("prompt <input>");

void TestPattern(string pattern)
{
    WriteLine($"Pattern: '{pattern}'");
    WriteLine(new string('-', 50));
    string result = ValidateRouteTool.ValidateRoute(pattern);
    WriteLine(result);
    WriteLine();
}