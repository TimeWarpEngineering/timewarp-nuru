#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-logging/timewarp-nuru-logging.csproj
#:package Microsoft.Extensions.Logging.Console

// ═══════════════════════════════════════════════════════════════════════════════
// BASIC PIPELINE MIDDLEWARE - LOGGING AND PERFORMANCE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates the fundamental pipeline behavior pattern using
// TimeWarp.Nuru's INuruBehavior for cross-cutting concerns.
//
// BEHAVIORS DEMONSTRATED:
//   - LoggingBehavior: Logs request entry and exit (uses BehaviorContext)
//   - PerformanceBehavior: Times execution and warns on slow commands (uses custom State)
//
// HOW PIPELINE BEHAVIORS WORK:
//   Behaviors execute in registration order, wrapping the handler like onion layers.
//   First behavior = outermost (OnBefore first, OnAfter last).
//   Each behavior can implement OnBefore, OnAfter, and/or OnError.
//
// KEY CONCEPTS:
//   - Behaviors are Singleton (instantiated once, services via constructor)
//   - Per-request state uses nested State class that inherits from BehaviorContext
//   - BehaviorContext provides: CommandName, CorrelationId, Stopwatch, CancellationToken
//
// RUN THIS SAMPLE:
//   ./pipeline-middleware-basic.cs echo "Hello, World!"
//   ./pipeline-middleware-basic.cs slow 600
// ═══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using TimeWarp.Nuru;
using Microsoft.Extensions.Logging;
using static System.Console;

#pragma warning disable NURU_H002 // Handler uses closure - intentional for demo

NuruCoreApp app = NuruApp.CreateBuilder(args)
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
/// Demonstrates the basic before/after/error pattern for pipeline behaviors.
/// Uses BehaviorContext directly (no custom state needed).
/// </summary>
public sealed class LoggingBehavior : INuruBehavior
{
  // Note: In production, you'd inject ILogger<LoggingBehavior> via constructor.
  // For this demo, we use Console output for simplicity.

  public ValueTask OnBeforeAsync(BehaviorContext context)
  {
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Handling {context.CommandName}");
    return ValueTask.CompletedTask;
  }

  public ValueTask OnAfterAsync(BehaviorContext context)
  {
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Completed {context.CommandName}");
    return ValueTask.CompletedTask;
  }

  public ValueTask OnErrorAsync(BehaviorContext context, Exception exception)
  {
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Error handling {context.CommandName}: {exception.Message}");
    return ValueTask.CompletedTask;
  }
}

/// <summary>
/// Performance behavior that times command execution and warns on slow commands.
/// Demonstrates using custom State class for per-request state.
/// The Stopwatch is inherited from BehaviorContext and auto-started.
/// </summary>
public sealed class PerformanceBehavior : INuruBehavior
{
  private const int SlowThresholdMs = 500;

  // Custom State class - inherits BehaviorContext to get CommandName, CorrelationId, Stopwatch, etc.
  // The Stopwatch is automatically started when the context is created.
  public sealed class State : BehaviorContext
  {
    // No additional state needed - we use the inherited Stopwatch
  }

  public ValueTask OnBeforeAsync(BehaviorContext context)
  {
    // Stopwatch is already running (auto-started when context was created)
    return ValueTask.CompletedTask;
  }

  public ValueTask OnAfterAsync(BehaviorContext context)
  {
    context.Stopwatch.Stop();
    long elapsed = context.Stopwatch.ElapsedMilliseconds;

    if (elapsed > SlowThresholdMs)
    {
      WriteLine($"[PERFORMANCE] {context.CommandName} took {elapsed}ms (threshold: {SlowThresholdMs}ms) - SLOW!");
    }
    else
    {
      WriteLine($"[PERFORMANCE] {context.CommandName} completed in {elapsed}ms");
    }

    return ValueTask.CompletedTask;
  }

  public ValueTask OnErrorAsync(BehaviorContext context, Exception exception)
  {
    context.Stopwatch.Stop();
    WriteLine($"[PERFORMANCE] {context.CommandName} failed after {context.Stopwatch.ElapsedMilliseconds}ms");
    return ValueTask.CompletedTask;
  }
}
