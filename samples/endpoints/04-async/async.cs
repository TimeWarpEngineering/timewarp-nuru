#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ASYNC EXAMPLES - ENDPOINT DSL
// ═══════════════════════════════════════════════════════════════════════════════
// Demonstrates async command handlers using the Endpoint DSL pattern.
//
// PATTERNS:
//   - Async handlers with ValueTask
//   - CancellationToken support
//   - Task-based async operations
//   - Async I/O simulation
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
