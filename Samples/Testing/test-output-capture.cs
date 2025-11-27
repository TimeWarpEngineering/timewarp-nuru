#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Demonstrates testing CLI output capture using TestTerminal

using TimeWarp.Nuru;

Console.WriteLine("=== Testing CLI Output Capture ===\n");

// Test 1: Basic output capture
Console.WriteLine("Test 1: Basic output capture");
{
  using var terminal = new TestTerminal();

  var app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("hello {name}", (string name, ITerminal t) =>
      t.WriteLine($"Hello, {name}!"))
    .Build();

  await app.RunAsync(["hello", "World"]);

  // Assert output contains expected text
  bool passed = terminal.OutputContains("Hello, World!");
  Console.WriteLine($"  Output: {terminal.Output.Trim()}");
  Console.WriteLine($"  Result: {(passed ? "PASSED".Green() : "FAILED".Red())}\n");
}

// Test 2: Multiple lines capture
Console.WriteLine("Test 2: Multiple lines capture");
{
  using var terminal = new TestTerminal();

  var app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("list", (ITerminal t) =>
    {
      t.WriteLine("Item 1");
      t.WriteLine("Item 2");
      t.WriteLine("Item 3");
    })
    .Build();

  await app.RunAsync(["list"]);

  string[] lines = terminal.GetOutputLines();
  bool passed = lines.Length == 3 &&
    lines[0] == "Item 1" &&
    lines[1] == "Item 2" &&
    lines[2] == "Item 3";

  Console.WriteLine($"  Lines captured: {lines.Length}");
  Console.WriteLine($"  Result: {(passed ? "PASSED".Green() : "FAILED".Red())}\n");
}

// Test 3: Error output capture
Console.WriteLine("Test 3: Error output capture");
{
  using var terminal = new TestTerminal();

  var app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("validate", (ITerminal t) =>
    {
      t.WriteErrorLine("Error: Invalid input");
      return 1;
    })
    .Build();

  int exitCode = await app.RunAsync(["validate"]);

  bool passed = terminal.ErrorContains("Invalid input") && exitCode == 1;
  Console.WriteLine($"  Error output: {terminal.ErrorOutput.Trim()}");
  Console.WriteLine($"  Exit code: {exitCode}");
  Console.WriteLine($"  Result: {(passed ? "PASSED".Green() : "FAILED".Red())}\n");
}

// Test 4: Combined output and error
Console.WriteLine("Test 4: Combined stdout and stderr");
{
  using var terminal = new TestTerminal();

  var app = new NuruAppBuilder()
    .UseTerminal(terminal)
    .Map("mixed", (ITerminal t) =>
    {
      t.WriteLine("Processing...");
      t.WriteErrorLine("Warning: Low memory");
      t.WriteLine("Done!");
    })
    .Build();

  await app.RunAsync(["mixed"]);

  bool passed =
    terminal.OutputContains("Processing...") &&
    terminal.OutputContains("Done!") &&
    terminal.ErrorContains("Warning: Low memory");

  Console.WriteLine($"  Stdout: {terminal.Output.Replace("\n", " ").Trim()}");
  Console.WriteLine($"  Stderr: {terminal.ErrorOutput.Trim()}");
  Console.WriteLine($"  Result: {(passed ? "PASSED".Green() : "FAILED".Red())}\n");
}

Console.WriteLine("=== All Tests Complete ===".BrightGreen().Bold());
