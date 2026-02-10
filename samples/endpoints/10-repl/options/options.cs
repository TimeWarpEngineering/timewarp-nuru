#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - REPL OPTIONS SHOWCASE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════════════
//
// Comprehensive REPL configuration demonstrating all available options.
//
// DSL: Endpoint with full ReplOptions customization
//
// FEATURES:
//   - Custom prompts (static and dynamic)
//   - Welcome/goodbye messages
//   - History configuration
//   - Arrow key history navigation
// ═══════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

// Single Build() with AddRepl() - handles both CLI and REPL modes
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddRepl(options =>
  {
    // Prompts
    options.Prompt = "nuru> ";
    options.ContinuationPrompt = "... ";

    // Messages
    options.WelcomeMessage = """
      ╔════════════════════════════════════════════════╗
      ║     TimeWarp.Nuru Interactive Shell          ║
      ║     Type 'help' for available commands       ║
      ╚════════════════════════════════════════════════╝
      """;
    options.GoodbyeMessage = "Thank you for using Nuru!";

    // History
    options.PersistHistory = true;
    options.HistoryFilePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".nuru_history"
    );
    options.MaxHistorySize = 1000;

    // Behavior
    options.ContinueOnError = true;
    options.ShowExitCode = false;
    options.ShowTiming = true;
    options.EnableArrowHistory = true;
    options.EnableColors = true;
    options.PromptColor = "\x1b[32m"; // Green

    // Auto-start REPL when no arguments provided
    options.AutoStartWhenEmpty = true;
  })
  .Build();

// RunAsync handles both CLI and REPL modes automatically
return await app.RunAsync(args);
