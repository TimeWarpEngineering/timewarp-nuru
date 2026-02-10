#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - SYNTAX EXAMPLES ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
// This project demonstrates all route pattern syntax using the Endpoint DSL.
//
// DSL: Endpoint (class-based with [NuruRoute], [Parameter], [Option] attributes)
//
// IMPORTANT: This file is used by the TimeWarp.Nuru MCP Server
// ============================================================================
// The MCP server extracts code snippets to provide syntax documentation
// to AI assistants and IDE integrations.
//
// This file MUST compile successfully - it serves as both documentation
// and validation that all syntax examples are correct and working.
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
