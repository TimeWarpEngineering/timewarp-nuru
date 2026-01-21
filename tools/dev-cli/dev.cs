#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// DEV CLI - TIMEWARP.NURU DEVELOPMENT TOOL
// ═══════════════════════════════════════════════════════════════════════════════
//
// This is the unified development CLI for TimeWarp.Nuru that provides:
// - CI/CD orchestration commands for GitHub Actions
// - Development workflow automation
// - AOT-compiled binary for fast execution
// - Endpoints for flexible command organization
//
// Usage:
//   As runfile:  dotnet tools/dev-cli/dev.cs <command>
//   As AOT:      ./dev <command>
//
// Architecture:
//   - Uses TimeWarp.Nuru with endpoints for command registration
//   - TimeWarp.Amuru for cross-platform shell and .NET operations
//   - AOT-compatible design for maximum performance
//
// Commands (Phase 1 - CI/CD Orchestration):
//   dev ci                 - Run full CI/CD pipeline (auto-detects mode)
//   dev ci --mode pr       - PR workflow: build -> verify-samples -> test
//   dev ci --mode release  - Release workflow: build -> check-version -> pack -> push
//   dev build              - Build all TimeWarp.Nuru projects
//   dev clean              - Clean solution and artifacts
//   dev test               - Run CI test suite
//   dev verify-samples     - Verify sample compilation
//   dev check-version      - Check if version already published
//   dev self-install       - AOT compile and install dev CLI to ./bin
//
// To bootstrap:
//   dotnet run tools/dev-cli/dev.cs -- self-install
//   direnv allow
//   dev --help
// ═══════════════════════════════════════════════════════════════════════════════

NuruApp app = NuruApp.CreateBuilder()
  .WithName("dev")
  .WithDescription("Development CLI for TimeWarp.Nuru")
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
