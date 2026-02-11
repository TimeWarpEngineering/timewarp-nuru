#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - TESTING OUTPUT CAPTURE
// ═══════════════════════════════════════════════════════════════════════════════
// Demonstrates testing CLI output using Endpoint DSL with ITerminal injection.
//
// DSL: Endpoint with ITerminal dependency for testable output
//
// PATTERN: Use TestTerminal to capture and assert CLI output in unit tests
// ═══════════════════════════════════════════════════════════════════════════════
#:package Shouldly
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using Shouldly;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

Console.WriteLine("=== Testing CLI Output Capture (Endpoint DSL) ===\n");

// Create terminal for capture
using TestTerminal terminal = new();

// Build app with TestTerminal
NuruApp app = NuruApp.CreateBuilder()
  .UseTerminal(terminal)
  .DiscoverEndpoints()
  .Build();

// Run demo command
await app.RunAsync(["demo"]);

// Verify stdout capture
Console.WriteLine("Verifying stdout capture:");
terminal.OutputContains("Hello from stdout!").ShouldBeTrue();
Console.WriteLine("  ✓ Contains expected message");

string[] lines = terminal.GetOutputLines();
lines.Length.ShouldBeGreaterThanOrEqualTo(4);
Console.WriteLine($"  ✓ Captured {lines.Length} lines");

// Verify stderr capture
Console.WriteLine("\nVerifying stderr capture:");
terminal.ErrorContains("Warning").ShouldBeTrue();
Console.WriteLine("  ✓ Error output captured");

// Show captured output
Console.WriteLine("\n--- Captured stdout ---");
Console.WriteLine(terminal.Output);

Console.WriteLine("--- Captured stderr ---");
Console.WriteLine(terminal.ErrorOutput);

Console.WriteLine("=== Test Complete ===".BrightGreen().Bold());
