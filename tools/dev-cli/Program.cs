// ═══════════════════════════════════════════════════════════════════════════════
// DEV CLI - TIMEWARP.NURU DEVELOPMENT TOOL
// ═══════════════════════════════════════════════════════════════════════════════
//
// This is the unified development CLI for TimeWarp.Nuru that provides:
// - CI/CD orchestration commands for GitHub Actions
// - Development workflow automation
// - AOT-compiled binary for fast execution
// - Attributed routes for flexible command organization
//
// Architecture:
//   - Uses TimeWarp.Nuru with attributed routes for command registration
//   - Mediator pattern for clean separation of commands and handlers
//   - TimeWarp.Amuru for cross-platform shell and .NET operations
//   - AOT-compatible design for maximum performance
//
// Commands (Phase 1 - CI/CD Orchestration):
//   dev build              - Build all TimeWarp.Nuru projects
//   dev test ci           - Run CI test suite
//   dev verify-samples    - Verify sample compilation
//   dev check-version     - Check if version already published
//   dev ci                - Run full CI/CD pipeline
//   dev release publish    - Publish release packages
// ═══════════════════════════════════════════════════════════════════════════════

using Mediator;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services => services.AddMediator())
  .AddAutoHelp()
  .WithMetadata("dev", "Development CLI for TimeWarp.Nuru")
  .Build();

await app.RunAsync(args);