#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// GANDA FULL EDITION
// ═══════════════════════════════════════════════════════════════════════════════
// Full CLI with all commands: kanban, git, and all groups.
// Usage: dotnet run ganda.cs -- ganda kanban add "Task 1"
//        dotnet run ganda.cs -- ganda git commit -m "message"
//        dotnet run ganda.cs -- --help
// ═══════════════════════════════════════════════════════════════════════════════

#pragma warning disable CA2007

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
