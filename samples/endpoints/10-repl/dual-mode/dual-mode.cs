#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - REPL DUAL MODE (CLI + Interactive) ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════════════
//
// CLI app that works both as command-line tool and interactive REPL.
//
// DSL: Endpoint with automatic mode detection via AutoStartWhenEmpty
//
// USAGE:
//   ./custom-keys.cs echo Hello          # CLI mode
//   ./custom-keys.cs                     # REPL mode (no args)
//   ./custom-keys.cs --interactive       # REPL mode (explicit flag)
// ═══════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

// Single Build() with AddRepl() - handles both CLI and REPL modes automatically
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddRepl(options =>
  {
    options.Prompt = "dual> ";
    options.WelcomeMessage = """
      ╔════════════════════════════════════╗
      ║  Nuru Dual-Mode Demo               ║
      ║  Type commands or 'exit' to quit   ║
      ╚════════════════════════════════════╝
      """;
    options.PersistHistory = true;
    options.HistoryFilePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".nuru_dual_history"
    );
    options.AutoStartWhenEmpty = true;
  })
  .Build();

// RunAsync automatically handles mode detection:
// - With args: runs as CLI
// - Without args: starts REPL (due to AutoStartWhenEmpty)
// - With --interactive: starts REPL
return await app.RunAsync(args);
