#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - FILTERED AUTHORIZATION PIPELINE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Demonstrates filtered behaviors using INuruBehavior<TFilter>.
// Only commands implementing IRequireAuthorization are protected.
//
// STRUCTURE:
//   - behaviors/: Authorization behavior and marker interface
//   - endpoints/: Public and protected endpoints
//
// PATTERN DEMONSTRATED:
//   - Marker interface (IRequireAuthorization) for opt-in behavior
//   - Type-safe filtered behavior with INuruBehavior<TFilter>
//   - Authorization checks before command execution
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using PipelineFilteredAuth.Behaviors;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .AddBehavior(typeof(AuthorizationBehavior))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);
