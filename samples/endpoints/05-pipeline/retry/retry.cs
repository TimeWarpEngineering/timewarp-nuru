#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - RETRY PIPELINE
// ═══════════════════════════════════════════════════════════════════════════════
//
// Demonstrates resilience with exponential backoff using filtered behaviors.
// Only commands implementing IRetryable are retried.
//
// STRUCTURE:
//   - behaviors/: Retry behavior and marker interface
//   - endpoints/: Reliable and unreliable endpoints
//
// PATTERN DEMONSTRATED:
//   - Marker interface (IRetryable) for opt-in retry behavior
//   - Exponential backoff with jitter
//   - Configurable max retries via IRetryable.MaxRetries
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using PipelineRetry.Behaviors;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .AddBehavior(typeof(RetryBehavior))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);
