#!/usr/bin/dotnet --
// test-output-capture - Demonstrates testing CLI output capture using TestTerminal
// Uses new NuruAppBuilder() for testing scenarios - provides ITerminal injection without full Mediator
#:package Shouldly
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using Shouldly;
using TimeWarp.Nuru;

Console.WriteLine("=== Testing CLI Output Capture ===\n");

// Test 1: Basic output capture
Console.WriteLine("Test 1: Basic output capture");
{
  using TestTerminal terminal = new();

  NuruCoreApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("hello {name}")
      .WithHandler((string name, ITerminal t) => t.WriteLine($"Hello, {name}!"))
      .AsCommand()
      .Done()
    .Build();

  await app.RunAsync(["hello", "World"]);

  terminal.OutputContains("Hello, World!").ShouldBeTrue();
  Console.WriteLine($"  Output: {terminal.Output.Trim()}");
  Console.WriteLine("  PASSED".Green());
}

// Test 2: Multiple lines capture
Console.WriteLine("\nTest 2: Multiple lines capture");
{
  using TestTerminal terminal = new();

  NuruCoreApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("list")
      .WithHandler((ITerminal t) =>
      {
        t.WriteLine("Item 1");
        t.WriteLine("Item 2");
        t.WriteLine("Item 3");
      })
      .AsQuery()
      .Done()
    .Build();

  await app.RunAsync(["list"]);

  string[] lines = terminal.GetOutputLines();
  lines.Length.ShouldBe(3);
  lines[0].ShouldBe("Item 1");
  lines[1].ShouldBe("Item 2");
  lines[2].ShouldBe("Item 3");
  Console.WriteLine($"  Lines captured: {lines.Length}");
  Console.WriteLine("  PASSED".Green());
}

// Test 3: Error output capture (even when handler throws)
Console.WriteLine("\nTest 3: Error output capture with exception");
{
  using TestTerminal terminal = new();

  NuruCoreApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("validate")
      .WithHandler((ITerminal t) =>
      {
        t.WriteErrorLine("Error: Invalid input");
        throw new InvalidOperationException("Intentional error to verify terminal capture still works");
      })
      .AsCommand()
      .Done()
    .Build();

  await app.RunAsync(["validate"]);

  terminal.ErrorContains("Invalid input").ShouldBeTrue();
  Console.WriteLine($"  Error output: {terminal.ErrorOutput.Trim()}");
  Console.WriteLine("  PASSED".Green());
}

// Test 4: Combined output and error
Console.WriteLine("\nTest 4: Combined stdout and stderr");
{
  using TestTerminal terminal = new();

  NuruCoreApp app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("mixed")
      .WithHandler((ITerminal t) =>
      {
        t.WriteLine("Processing...");
        t.WriteErrorLine("Warning: Low memory");
        t.WriteLine("Done!");
      })
      .AsCommand()
      .Done()
    .Build();

  await app.RunAsync(["mixed"]);

  terminal.OutputContains("Processing...").ShouldBeTrue();
  terminal.OutputContains("Done!").ShouldBeTrue();
  terminal.ErrorContains("Warning: Low memory").ShouldBeTrue();
  Console.WriteLine($"  Stdout: {terminal.Output.Replace("\n", " ").Trim()}");
  Console.WriteLine($"  Stderr: {terminal.ErrorOutput.Trim()}");
  Console.WriteLine("  PASSED".Green());
}

Console.WriteLine("\n=== All Tests Complete ===".BrightGreen().Bold());
