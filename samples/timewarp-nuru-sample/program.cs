// ═══════════════════════════════════════════════════════════════════════════════
// TIMEWARP.NURU SAMPLE - GENERAL REFERENCE APPLICATION
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) which provides:
// - Full DI container setup
// - Configuration support
// - Auto-help generation
// - REPL support with tab completion
// - All extensions enabled by default
//
// PATTERNS DEMONSTRATED:
// 1. Delegate handlers - simple lambdas for quick commands
// 2. Attributed routes - [NuruRoute] with nested Handler class (TODO)
//
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;
using static System.Console;

// Build the app with canonical CreateBuilder pattern
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(_ => { /* Services registered here are available in handlers */ })
  // Default route when no command is specified
  .Map("")
    .WithHandler(() => WriteLine("Welcome to the Nuru sample app! Use --help to see available commands."))
    .WithDescription("Default welcome message")
    .AsQuery()
    .Done()
  .Map("status")
    .WithHandler(() => WriteLine("✓ System is running"))
    .WithDescription("Check system status")
    .AsQuery()
    .Done()
  .Map("echo {message}")
    .WithHandler((string message) => WriteLine($"Echo: {message}"))
    .WithDescription("Echo a message back")
    .AsQuery()
    .Done()
  .Map("proxy {command}")
    .WithHandler((string command) => WriteLine($"Would execute: {command}"))
    .WithDescription("Proxy command execution")
    .AsCommand()
    .Done()
  .Map("add {value1:double} {value2:double}")
    .WithHandler((double value1, double value2) => value1 + value2)
    .WithDescription("Add two numbers")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args).ConfigureAwait(false);
