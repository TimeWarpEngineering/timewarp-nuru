#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - COMPLETE PIPELINE MIDDLEWARE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Complete enterprise-grade reference implementation combining ALL pipeline behaviors.
//
// STRUCTURE:
//   - behaviors/: All pipeline behavior classes (global and filtered)
//   - endpoints/: Command and query endpoints demonstrating filters
//
// DSL: Endpoint with full behavior pipeline registered via .AddBehavior()
//
// BEHAVIORS INCLUDED:
//   1. TelemetryBehavior       - OpenTelemetry distributed tracing (global)
//   2. PerformanceBehavior     - Timing and slow command warnings (global)
//   3. LoggingBehavior         - Request/response logging (global)
//   4. AuthorizationBehavior   - Filtered authorization (IRequireAuthorization)
//   5. RetryBehavior           - Exponential backoff (IRetryable)
//   6. ExceptionHandlingBehavior - Consistent error handling (global)
//
// ORDER MATTERS: Behaviors execute in registration order (first = outermost)
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using PipelineCombined.Behaviors;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  // Outermost: Telemetry (captures everything)
  .AddBehavior(typeof(DistributedTelemetryBehavior))
  // Next: Performance monitoring
  .AddBehavior(typeof(PerformanceBehavior))
  // Next: Logging
  .AddBehavior(typeof(LoggingBehavior))
  // Next: Authorization (filtered to IRequireAuthorization)
  .AddBehavior(typeof(AuthorizationBehavior))
  // Next: Retry (filtered to IRetryable)
  .AddBehavior(typeof(RetryBehavior))
  // Innermost: Exception handling
  .AddBehavior(typeof(ExceptionHandlingBehavior))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);
