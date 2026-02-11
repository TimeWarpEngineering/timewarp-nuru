#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - BASIC PIPELINE MIDDLEWARE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates the fundamental pipeline behavior pattern using
// TimeWarp.Nuru's INuruBehavior with HandleAsync(context, proceed) pattern.
//
// STRUCTURE:
//   - behaviors/: Pipeline behavior classes
//   - endpoints/: Command and query endpoints
//
// DSL: Endpoint with .AddBehavior() registration
//
// BEHAVIORS DEMONSTRATED:
//   - LoggingBehavior: Logs request entry and exit
//   - PerformanceBehavior: Times execution and warns on slow commands
//
// HOW PIPELINE BEHAVIORS WORK:
//   Behaviors execute in registration order, wrapping the handler like onion layers.
//   First behavior = outermost (called first, returns last).
//   Each behavior calls 'proceed()' to invoke the next behavior or handler.
//
// KEY CONCEPTS:
//   - Behaviors are Singleton (instantiated once, services via constructor)
//   - HandleAsync(context, proceed) gives full control over execution flow
//   - BehaviorContext provides: CommandName, CorrelationId, CancellationToken, Command
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using PipelineBasic.Behaviors;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  // Register behaviors - execute in order (first = outermost)
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(PerformanceBehavior))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);
