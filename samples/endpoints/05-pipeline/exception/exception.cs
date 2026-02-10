#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - EXCEPTION HANDLING PIPELINE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Demonstrates consistent exception handling using INuruBehavior.
//
// STRUCTURE:
//   - behaviors/: Exception handling behavior
//   - endpoints/: Various exception scenarios
//
// BEHAVIOR DEMONSTRATED:
//   - ExceptionHandlingBehavior: Catches and categorizes exceptions
//   - Provides user-friendly error messages
//   - Logs detailed error information
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using PipelineException.Behaviors;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .AddBehavior(typeof(ExceptionHandlingBehavior))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);
