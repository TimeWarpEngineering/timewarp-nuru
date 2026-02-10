#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - TESTING COLORED OUTPUT ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
// Demonstrates testing handlers that produce colored/styled output.
//
// DSL: Endpoint with ITerminal and ANSI color codes
//
// PATTERN: Verify ANSI codes in captured output from TestTerminal
// ═══════════════════════════════════════════════════════════════════════════════
#:package Shouldly
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using Shouldly;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

Console.WriteLine("=== Testing Colored Output (Endpoint DSL) ===\n");

using TestTerminal terminal = new();

NuruApp app = NuruApp.CreateBuilder()
  .UseTerminal(terminal)
  .DiscoverEndpoints()
  .Build();

// Test success output
Console.WriteLine("Testing success output...");
await app.RunAsync(["status"]);
terminal.OutputContains("✓").ShouldBeTrue();
terminal.OutputContains("Healthy".Green().ToString()).ShouldBeTrue();
Console.WriteLine("  ✓ Success indicators captured");

// Test error output
Console.WriteLine("\nTesting error output...");
terminal.Clear();
await app.RunAsync(["error", "test"]).ContinueWith(t => { /* Expected to throw */ });
terminal.ErrorContains("Error").ShouldBeTrue();
Console.WriteLine("  ✓ Error indicators captured");

// Test mixed output
Console.WriteLine("\nTesting mixed colored output...");
terminal.Clear();
await app.RunAsync(["report"]);
string output = terminal.Output;
output.ShouldContain("SUCCESS".Green().ToString());
output.ShouldContain("WARNING".Yellow().ToString());
output.ShouldContain("ERROR".Red().ToString());
Console.WriteLine("  ✓ All color codes captured");

Console.WriteLine("\n--- Full captured output ---");
Console.WriteLine(terminal.Output);
Console.WriteLine("=== Test Complete ===".BrightGreen().Bold());
