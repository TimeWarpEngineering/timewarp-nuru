#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// GROUP OPTIONS SAMPLE - Task 419
// ═══════════════════════════════════════════════════════════════════════════════
// Demonstrates GroupOption feature for shared options across route groups.
//
// This sample simulates a Git CLI with shared options like --verbose, --dry-run
// that apply to all commands within the git group.
//
// Usage examples:
//   dotnet run -- git status --verbose
//   dotnet run -- git commit -m "message" --dry-run
//   dotnet run -- git clone https://github.com/user/repo --verbose --config user.name
//
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
