#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// FLUENT DSL - BASIC PIPELINE MIDDLEWARE (Logging and Performance)
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates the fundamental pipeline behavior pattern using
// TimeWarp.Nuru's INuruBehavior with HandleAsync(context, proceed) pattern.
//
// DSL: Fluent API with .AddBehavior()
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
//
// RUN THIS SAMPLE:
//   ./fluent-pipeline-basic.cs echo "Hello, World!"
//   ./fluent-pipeline-basic.cs slow 600
// ═══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using TimeWarp.Nuru;
using static System.Console;

#pragma warning disable NURU_H002 // Handler uses closure - intentional for demo

NuruApp app = NuruApp.CreateBuilder()
  // Register behaviors - execute in order (first = outermost)
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(PerformanceBehavior))
  // Simple command to demonstrate pipeline execution
  .Map("echo {message}")
    .WithDescription("Echo a message back (demonstrates pipeline)")
    .WithHandler((string message) => WriteLine($"Echo: {message}"))
    .Done()
  // Slow command to trigger performance warning
  .Map("slow {delay:int}")
    .WithDescription("Simulate slow operation (ms) to demonstrate performance behavior")
    .WithHandler(async (int delay) =>
    {
      WriteLine($"Starting slow operation ({delay}ms)...");
      await Task.Delay(delay);
      WriteLine("Slow operation completed.");
    })
    .Done()
  .Build();

#pragma warning restore NURU_H002

return await app.RunAsync(args);

// =============================================================================
// PIPELINE BEHAVIORS
// =============================================================================

/// <summary>
/// Logging behavior that logs request entry and exit.
/// Demonstrates the basic before/after pattern using HandleAsync.
/// </summary>
public sealed class LoggingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Handling {context.CommandName}");

    try
    {
      await proceed();
      WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Completed {context.CommandName}");
    }
    catch (Exception ex)
    {
      WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Error handling {context.CommandName}: {ex.Message}");
      throw;
    }
  }
}

/// <summary>
/// Performance behavior that times command execution and warns on slow commands.
/// Uses local Stopwatch - no need for custom State class.
/// </summary>
public sealed class PerformanceBehavior : INuruBehavior
{
  private const int SlowThresholdMs = 500;

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    var stopwatch = Stopwatch.StartNew();

    try
    {
      await proceed();
    }
    finally
    {
      stopwatch.Stop();
      long elapsed = stopwatch.ElapsedMilliseconds;

      if (elapsed > SlowThresholdMs)
      {
        WriteLine($"[PERFORMANCE] {context.CommandName} took {elapsed}ms (threshold: {SlowThresholdMs}ms) - SLOW!");
      }
      else
      {
        WriteLine($"[PERFORMANCE] {context.CommandName} completed in {elapsed}ms");
      }
    }
  }
}
