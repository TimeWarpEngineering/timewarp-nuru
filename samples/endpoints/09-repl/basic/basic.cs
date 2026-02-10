#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - REPL BASIC DEMO ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════════════
//
// Interactive REPL mode with Endpoint DSL using auto-discovered endpoints.
//
// DSL: Endpoint with .AddRepl() for interactive mode
//
// PATTERNS:
//   - CLI app that can run in REPL mode
//   - All endpoints available interactively
//   - Type 'exit' or Ctrl+C to quit
// ═══════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .AddRepl()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
