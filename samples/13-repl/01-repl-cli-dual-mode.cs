#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

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
using static System.Console;

NuruApp app = NuruApp.CreateBuilder(args)
  .WithDescription("Demo app supporting both CLI and interactive REPL modes")

  // Define application commands
  .Map("greet {name}")
    .WithHandler((string name) => WriteLine($"Hello, {name}!"))
    .WithDescription("Greet someone by name")
    .AsCommand()
    .Done()
  .Map("status")
    .WithHandler(() => WriteLine("System status: OK"))
    .WithDescription("Show system status")
    .AsQuery()
    .Done()
  .Map("add {a:int} {b:int}")
    .WithHandler((int a, int b) => WriteLine($"{a} + {b} = {a + b}"))
    .WithDescription("Add two numbers")
    .AsQuery()
    .Done()
  .Map("time")
    .WithHandler(() => WriteLine($"Current time: {DateTime.Now:HH:mm:ss}"))
    .WithDescription("Show current time")
    .AsQuery()
    .Done()

  // Enable REPL with custom configuration
  .AddRepl(options =>
  {
    options.Prompt = "demo> ";
    options.WelcomeMessage =
      "Welcome to Interactive Mode!\n" +
      "Type '--help' for available commands, 'exit' to quit.";
    options.GoodbyeMessage = "Goodbye!";
  })
  .Build();

// Run the app - either executes a single command or enters REPL
return await app.RunAsync(args);
