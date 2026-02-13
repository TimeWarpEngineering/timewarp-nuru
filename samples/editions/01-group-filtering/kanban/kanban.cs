#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// KANBAN EDITION - Subset of ganda CLI
// ═══════════════════════════════════════════════════════════════════════════════
// Only kanban commands. "ganda" prefix stripped.
// Usage: dotnet run kanban.cs -- kanban add "Task 1"
//        dotnet run kanban.cs -- kanban list
//        dotnet run kanban.cs -- --help
// ═══════════════════════════════════════════════════════════════════════════════

#pragma warning disable CA2007

using Editions.GroupFiltering;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints(typeof(KanbanGroup))
  .Build();

return await app.RunAsync(args);
