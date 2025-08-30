#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

using TimeWarp.Nuru.Mcp.Tools;
using static System.Console;

WriteLine("Testing ValidateRouteTool:");
WriteLine();

// Test various route patterns
TestRoutePattern("status");
TestRoutePattern("git commit");
TestRoutePattern("deploy {env}");
TestRoutePattern("deploy {env} {tag?}");
TestRoutePattern("delay {ms:int}");
TestRoutePattern("docker {*args}");
TestRoutePattern("build --verbose");
TestRoutePattern("build --config {mode}");
TestRoutePattern("deploy {env|Environment} --dry-run,-d|Preview");

// Test invalid route patterns
WriteLine("\nTesting invalid route patterns:");
TestRoutePattern("deploy {env");
TestRoutePattern("prompt <input>");

void TestRoutePattern(string routePattern)
{
    WriteLine($"Route Pattern: '{routePattern}'");
    WriteLine(new string('-', 50));
    string result = ValidateRouteTool.ValidateRoute(routePattern);
    WriteLine(result);
    WriteLine();
}