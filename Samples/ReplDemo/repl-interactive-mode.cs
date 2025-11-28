#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

// ============================================================================
// Interactive Mode Demo for TimeWarp.Nuru
// ============================================================================
// Demonstrates an app that supports both CLI and REPL modes:
//
// CLI mode (single command execution):
//   ./repl-interactive-mode.cs greet Alice
//   ./repl-interactive-mode.cs status
//   ./repl-interactive-mode.cs add 5 3
//
// Interactive mode (enter REPL):
//   ./repl-interactive-mode.cs --interactive
//   ./repl-interactive-mode.cs -i
//
// This pattern allows users to run quick one-off commands or enter
// an interactive session for extended use.
// ============================================================================

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using static System.Console;

NuruCoreApp app = new NuruAppBuilder()
  .WithMetadata
  (
    name: "interactive-demo",
    description: "Demo app supporting both CLI and interactive REPL modes"
  )

  // Define application commands
  .Map
  (
    "greet {name}",
    (string name) => WriteLine($"Hello, {name}!"),
    "Greet someone by name"
  )
  .Map
  (
    "status",
    () => WriteLine("System status: OK"),
    "Show system status"
  )
  .Map
  (
    "add {a:int} {b:int}",
    (int a, int b) => WriteLine($"{a} + {b} = {a + b}"),
    "Add two numbers"
  )
  .Map
  (
    "time",
    () => WriteLine($"Current time: {DateTime.Now:HH:mm:ss}"),
    "Show current time"
  )

  // Add REPL support with configuration
  .AddReplSupport
  (
    options =>
    {
      options.Prompt = "demo> ";
      options.WelcomeMessage =
        "Welcome to Interactive Mode!\n" +
        "Type 'help' for available commands, 'exit' to quit.";
      options.GoodbyeMessage = "Goodbye!";
    }
  )

  // Add the interactive route (--interactive, -i)
  .AddInteractiveRoute()

  .Build();

// Run the app - either executes a single command or enters REPL
return await app.RunAsync(args);
