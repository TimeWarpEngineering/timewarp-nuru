#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - BASIC PIPELINE MIDDLEWARE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates the fundamental pipeline behavior pattern using
// TimeWarp.Nuru's INuruBehavior with HandleAsync(context, proceed) pattern.
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

using System.Diagnostics;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  // Register behaviors - execute in order (first = outermost)
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(PerformanceBehavior))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);

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
    Stopwatch stopwatch = Stopwatch.StartNew();

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

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("echo", Description = "Echo a message back (demonstrates pipeline)")]
public sealed class EchoCommand : ICommand<Unit>
{
  [Parameter(Description = "Message to echo")]
  public string Message { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<EchoCommand, Unit>
  {
    public ValueTask<Unit> Handle(EchoCommand command, CancellationToken ct)
    {
      WriteLine($"Echo: {command.Message}");
      return default;
    }
  }
}

[NuruRoute("slow", Description = "Simulate slow operation (ms) to demonstrate performance behavior")]
public sealed class SlowCommand : ICommand<Unit>
{
  [Parameter(Description = "Milliseconds to delay")]
  public int Delay { get; set; }

  public sealed class Handler : ICommandHandler<SlowCommand, Unit>
  {
    public async ValueTask<Unit> Handle(SlowCommand command, CancellationToken ct)
    {
      WriteLine($"Starting slow operation ({command.Delay}ms)...");
      await Task.Delay(command.Delay, ct);
      WriteLine("Slow operation completed.");
      return default;
    }
  }
}
