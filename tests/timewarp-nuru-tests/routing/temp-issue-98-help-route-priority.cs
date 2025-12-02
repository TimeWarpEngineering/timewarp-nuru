#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

// Reproduce Issue 98: Auto-generated --help routes match before optional flag routes

// Test scenario: Define a route with an optional flag
var builder = NuruApp.CreateBuilder(args);

builder.Map("recent --verbose?", (bool verbose) => 
{
    WriteLine($"Executing 'recent' with verbose={verbose}");
    return 0;
}, "Show recent items");

var app = builder.Build();

// Test with no arguments - should show "recent" handler, not help
WriteLine($"Testing with args: [{string.Join(", ", args)}]");
await app.RunAsync(args);
