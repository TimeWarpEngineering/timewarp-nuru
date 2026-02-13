#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// GIT EDITION - Subset of ganda CLI
// ═══════════════════════════════════════════════════════════════════════════════
// Only git commands. "ganda" prefix stripped.
// Usage: dotnet run git.cs -- git commit -m "message"
//        dotnet run git.cs -- git status
//        dotnet run git.cs -- --help
// ═══════════════════════════════════════════════════════════════════════════════

#pragma warning disable CA2007

using Editions.GroupFiltering;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints(typeof(GitGroup))
  .Build();

return await app.RunAsync(args);
