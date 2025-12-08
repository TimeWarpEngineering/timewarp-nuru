#!/usr/bin/dotnet --
// test-real-app - Demonstrates zero-configuration testing using TestTerminalContext
// This pattern allows testing real CLI apps by calling their Main() directly
#:package Shouldly
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using Shouldly;
using TimeWarp.Nuru;

Console.WriteLine("=== Testing Real Apps with TestTerminalContext ===\n");

// Test 1: Basic usage pattern
Console.WriteLine("Test 1: Basic TestTerminalContext usage");
{
  using TestTerminal terminal = new();
  TestTerminalContext.Current = terminal;

  // Call the "real" app's entry point
  await SampleApp.Main(["greet", "World"]);

  terminal.OutputContains("Hello, World!").ShouldBeTrue();
  Console.WriteLine($"  Output: {terminal.Output.Trim()}");
  Console.WriteLine("  PASSED".Green());

  TestTerminalContext.Current = null;
}

// Test 2: Parallel test isolation
Console.WriteLine("\nTest 2: Parallel test isolation");
{
  // Run multiple tests in parallel - each gets its own terminal
  Task[] tasks =
  [
    TestGreeting("Alice"),
    TestGreeting("Bob"),
    TestGreeting("Charlie"),
  ];

  await Task.WhenAll(tasks);
  Console.WriteLine("  All parallel tests passed!".Green());
}

static async Task TestGreeting(string name)
{
  using TestTerminal terminal = new();
  TestTerminalContext.Current = terminal;

  // For parallel tests, build a fresh app each time
  // This ensures each test has complete isolation
  await SampleApp.RunFresh(["greet", name]);

  terminal.OutputContains($"Hello, {name}!").ShouldBeTrue();
  Console.WriteLine($"    {name}: {terminal.Output.Trim()}".Green());

  TestTerminalContext.Current = null;
}

// Test 3: Testing error output
Console.WriteLine("\nTest 3: Error output capture");
{
  using TestTerminal terminal = new();
  TestTerminalContext.Current = terminal;

  int exitCode = await SampleApp.RunFresh(["unknown-command"]);

  exitCode.ShouldBe(1);
  terminal.ErrorContains("No matching command found").ShouldBeTrue();
  Console.WriteLine($"  Exit code: {exitCode}");
  Console.WriteLine("  PASSED".Green());

  TestTerminalContext.Current = null;
}

// Test 4: Testing with options
Console.WriteLine("\nTest 4: Commands with options");
{
  using TestTerminal terminal = new();
  TestTerminalContext.Current = terminal;

  await SampleApp.RunFresh(["deploy", "prod", "--dry-run"]);

  terminal.OutputContains("DRY RUN").ShouldBeTrue();
  terminal.OutputContains("prod").ShouldBeTrue();
  Console.WriteLine($"  Output: {terminal.Output.Trim()}");
  Console.WriteLine("  PASSED".Green());

  TestTerminalContext.Current = null;
}

Console.WriteLine("\n=== All Tests Complete ===".BrightGreen().Bold());

// Sample CLI app that would normally be in a separate project
public static class SampleApp
{
  // For testing scenarios, this pattern builds a fresh app each time
  // to ensure TestTerminalContext isolation works correctly
  public static async Task<int> RunFresh(string[] args)
  {
    NuruCoreApp app = BuildApp();
    return await app.RunAsync(args);
  }

  // If your app is a static Program.Main, you'd call it like this:
  // await Program.Main(args);
  // But in that case, make sure your app creates a fresh builder each time.
  public static async Task<int> Main(string[] args)
  {
    // This demonstrates the pattern for an actual Main method
    // Each call builds fresh to support TestTerminalContext
    return await RunFresh(args);
  }

  private static NuruCoreApp BuildApp()
  {
    NuruCoreAppBuilder builder = NuruCoreApp.CreateSlimBuilder();
    builder.AddAutoHelp();

    builder.Map("greet {name}", (string name, ITerminal terminal) =>
      terminal.WriteLine($"Hello, {name}!"));

    builder.Map("deploy {env} --dry-run", (string env, ITerminal terminal) =>
      terminal.WriteLine($"[DRY RUN] Would deploy to {env}"));

    builder.Map("deploy {env}", (string env, ITerminal terminal) =>
      terminal.WriteLine($"Deploying to {env}..."));

    return builder.Build();
  }
}
