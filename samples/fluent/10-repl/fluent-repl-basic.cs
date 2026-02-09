#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// FLUENT DSL - INTERACTIVE REPL MODE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder() with REPL support using Fluent DSL.
//
// DSL: Fluent API with .AddRepl()
//
// Features:
// - Full DI container setup
// - Configuration support
// - Auto-help generation
// - REPL support with tab completion
// - Interactive mode route (--interactive, -i) built-in
//
// CLI mode (single command execution):
//   ./fluent-repl-basic.cs greet Alice
//   ./fluent-repl-basic.cs status
//   ./fluent-repl-basic.cs add 5 3
//
// Interactive mode (enter REPL):
//   ./fluent-repl-basic.cs              (auto-starts via AutoStartWhenEmpty)
//   ./fluent-repl-basic.cs --interactive
//   ./fluent-repl-basic.cs -i
//
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .WithDescription("Demo app supporting both CLI and interactive REPL modes (Fluent DSL)")

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
    options.AutoStartWhenEmpty = true; // Auto-start REPL when no args provided
  })
  .Build();

// Run the app - either executes a single command or enters REPL
return await app.RunAsync(args);
