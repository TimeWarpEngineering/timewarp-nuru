#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - SHELL TAB COMPLETION
// ═══════════════════════════════════════════════════════════════════════════════════════════════
//
// Static shell tab completion for Bash, Zsh, PowerShell, and Fish.
//
// DSL: Endpoint with completion script generation
//
// USAGE:
//   ./completion.cs completion bash  # Generate bash completions
//   ./completion.cs completion zsh   # Generate zsh completions
//   ./completion.cs completion ps   # Generate PowerShell completions
// ═══════════════════════════════════════════════════════════════════════════════════════════════
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
