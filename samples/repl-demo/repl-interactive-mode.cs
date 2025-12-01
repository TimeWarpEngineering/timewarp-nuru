#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

// ═══════════════════════════════════════════════════════════════════════════════
// INTERACTIVE MODE DEMO - CLI + REPL DUAL MODE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) which provides:
// - Full DI container setup
// - Configuration support
// - Auto-help generation
// - REPL support with tab completion
// - Interactive mode route (--interactive, -i) built-in
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
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

NuruAppOptions nuruAppOptions = new()
{
  ConfigureRepl = options =>
  {
    options.Prompt = "demo> ";
    options.WelcomeMessage =
      "Welcome to Interactive Mode!\n" +
      "Type 'help' for available commands, 'exit' to quit.";
    options.GoodbyeMessage = "Goodbye!";
  }
};

NuruCoreApp app = NuruApp.CreateBuilder(args, nuruAppOptions)
  .ConfigureServices(services => services.AddMediator())
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
  .Build();

// Run the app - either executes a single command or enters REPL
return await app.RunAsync(args);
