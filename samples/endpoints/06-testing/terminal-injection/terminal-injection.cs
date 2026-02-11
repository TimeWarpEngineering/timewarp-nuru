#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - TERMINAL DEPENDENCY INJECTION
// ═══════════════════════════════════════════════════════════════════════════════
// Demonstrates ITerminal injection into endpoint handlers for testable output.
//
// DSL: Endpoint with constructor injection of ITerminal
//
// PATTERN: Inject ITerminal into Handler constructor for testable output
// ═══════════════════════════════════════════════════════════════════════════════
#:package Shouldly
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using Shouldly;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

Console.WriteLine("=== Testing ITerminal Dependency Injection (Endpoint DSL) ===\n");

// Create test terminal
using TestTerminal terminal = new();

// Build app with terminal
NuruApp app = NuruApp.CreateBuilder()
  .UseTerminal(terminal)
  .DiscoverEndpoints()
  .Build();

// Test 1: Simple output
Console.WriteLine("Test 1: Simple output");
await app.RunAsync(["greet", "World"]);
terminal.OutputContains("Hello, World!").ShouldBeTrue();
Console.WriteLine("  ✓ Simple output works");

// Test 2: Styled output
Console.WriteLine("\nTest 2: Styled output");
terminal.Clear();
await app.RunAsync(["info"]);
terminal.OutputContains("INFO").ShouldBeTrue();
Console.WriteLine("  ✓ Styled output works");

// Test 3: Error output
Console.WriteLine("\nTest 3: Error output");
terminal.Clear();
try
{
  await app.RunAsync(["fail"]);
}
catch { /* Expected */ }
terminal.ErrorContains("Failed").ShouldBeTrue();
Console.WriteLine("  ✓ Error output works");

// Test 4: Multiple terminals
Console.WriteLine("\nTest 4: Multiple command invocations");
terminal.Clear();
await app.RunAsync(["greet", "Alice"]);
await app.RunAsync(["greet", "Bob"]);
terminal.OutputContains("Alice").ShouldBeTrue();
terminal.OutputContains("Bob").ShouldBeTrue();
Console.WriteLine("  ✓ Multiple invocations captured");

Console.WriteLine("\n--- All captured output ---");
Console.WriteLine(terminal.Output);
if (!string.IsNullOrEmpty(terminal.ErrorOutput))
{
  Console.WriteLine("\n--- Error output ---");
  Console.WriteLine(terminal.ErrorOutput);
}

Console.WriteLine("=== Test Complete ===".BrightGreen().Bold());
