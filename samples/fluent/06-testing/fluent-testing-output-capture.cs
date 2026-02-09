#!/usr/bin/dotnet --
// fluent-testing-output-capture - Demonstrates testing CLI output using Fluent DSL
// Uses NuruApp.CreateBuilder() with UseTerminal() for testable CLI apps
//
// DSL: Fluent API with .UseTerminal()
//
// NOTE: For multiple test scenarios, use the Jaribu test framework pattern.
// This sample demonstrates a single comprehensive test.
#:package Shouldly
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using Shouldly;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

Console.WriteLine("=== Testing CLI Output Capture (Fluent DSL) ===\n");

// Create terminal for capture
using TestTerminal terminal = new();

// Build app with multiple routes to demonstrate different output patterns
NuruApp app = NuruApp.CreateBuilder()
  .UseTerminal(terminal)
  .Map("demo")
    .WithHandler((ITerminal t) =>
    {
      // Demonstrate stdout
      t.WriteLine("Hello from stdout!");
      t.WriteLine("Line 1");
      t.WriteLine("Line 2");
      t.WriteLine("Line 3");
      
      // Demonstrate stderr
      t.WriteErrorLine("Warning: This is a warning");
      
      // Demonstrate styled output
      t.WriteLine("Success!".Green());
    })
    .AsCommand()
    .Done()
  .Build();

// Run the demo command
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
