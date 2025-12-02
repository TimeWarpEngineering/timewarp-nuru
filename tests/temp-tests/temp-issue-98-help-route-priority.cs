#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

// Reproduce Issue 98: Auto-generated --help routes match before optional flag routes

// Test scenario: Define a route with an optional flag
NuruCoreAppBuilder builder = NuruApp.CreateSlimBuilder(args);

builder.Map("recent --verbose?", (bool verbose) =>
{
    WriteLine($"Executing 'recent' with verbose={verbose}");
    return 0;
}, "Show recent items");

NuruCoreApp app = builder.Build();

// Print all registered routes
WriteLine("=== Registered Routes ===");
foreach (Endpoint endpoint in builder.EndpointCollection.Endpoints)
{
    CompiledRoute route = endpoint.CompiledRoute;
    WriteLine($"  Pattern: '{endpoint.RoutePattern}' | Specificity: {route.Specificity} | Options: {route.OptionMatchers.Count}");
    foreach (OptionMatcher option in route.OptionMatchers)
    {
        WriteLine($"    Option: {option.MatchPattern} | IsOptional: {option.IsOptional} | ExpectsValue: {option.ExpectsValue}");
    }
}

WriteLine();

// Test with no arguments - should show "recent" handler, not help
WriteLine($"Testing with args: [{string.Join(", ", args)}]");
WriteLine($"NOTE: If 'recent --help' shows usage, that's the BUG - recent --verbose should execute instead");
await app.RunAsync(args);
