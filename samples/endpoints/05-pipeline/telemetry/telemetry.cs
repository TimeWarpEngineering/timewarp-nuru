#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - TELEMETRY PIPELINE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Demonstrates OpenTelemetry-compatible distributed tracing using INuruBehavior.
//
// STRUCTURE:
//   - behaviors/: Telemetry behavior
//   - endpoints/: Commands with Activity spans
//
// BEHAVIOR DEMONSTRATED:
//   - TelemetryBehavior: Creates Activity spans for observability
//   - Compatible with OpenTelemetry exporters
//   - Adds tags for command name and correlation ID
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using PipelineTelemetry.Behaviors;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .AddBehavior(typeof(TelemetryBehavior))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);
