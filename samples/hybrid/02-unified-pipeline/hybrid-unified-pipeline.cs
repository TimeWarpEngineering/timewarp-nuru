#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// HYBRID - UNIFIED PIPELINE ⚠️ EDGE CASE
// ═══════════════════════════════════════════════════════════════════════════════
//
// Demonstrates that TimeWarp.Nuru has ONE unified behavior pipeline
// that applies to BOTH delegate routes AND endpoints.
//
// DSL: Hybrid - Shows unified pipeline working with both patterns
//
// KEY INSIGHT:
//   There is no separate "delegate pipeline" vs "endpoint pipeline".
//   Behaviors registered with .AddBehavior() apply to ALL routes uniformly.
//
// Based on: samples/11-unified-middleware/unified-middleware.cs
// ═══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  // =========================================================================
  // UNIFIED BEHAVIOR PIPELINE - Applies to BOTH Fluent routes AND Endpoints
  // =========================================================================
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(PerformanceBehavior))
  // =========================================================================
  // FLUENT ROUTES - Wrapped by the same pipeline
  // =========================================================================
  .Map("add {x:int} {y:int}")
    .WithHandler((int x, int y) => WriteLine($"Result: {x} + {y} = {x + y}"))
    .WithDescription("Add (Fluent route with unified pipeline)")
    .Done()
  .Map("multiply {x:int} {y:int}")
    .WithHandler((int x, int y) => WriteLine($"Result: {x} × {y} = {x * y}"))
    .WithDescription("Multiply (Fluent route with unified pipeline)")
    .Done()
  .Map("greet {name}")
    .WithHandler((string name) => WriteLine($"Hello, {name}!"))
    .WithDescription("Greet (Fluent route with unified pipeline)")
    .Done()
  // =========================================================================
  // ENDPOINT ROUTES - Also wrapped by the SAME pipeline
  // EchoCommand and SlowCommand (below) flow through the same behaviors
  // =========================================================================
  .DiscoverEndpoints()
  .Build();

WriteLine("=== Unified Pipeline Demo ===\n");
WriteLine("All routes (Fluent AND Endpoints) flow through the same pipeline.\n");
WriteLine("Commands:");
WriteLine("  add 5 3        - Fluent route");
WriteLine("  multiply 4 7   - Fluent route");
WriteLine("  greet World    - Fluent route");
WriteLine("  echo hello     - Endpoint route");
WriteLine("  slow 600       - Endpoint route (triggers performance warning)\n");

return await app.RunAsync(args);

// =============================================================================
// PIPELINE BEHAVIORS - Apply to ALL routes uniformly
// =============================================================================

public sealed class LoggingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    WriteLine($"[PIPELINE] Handling {context.CommandName}");
    await proceed();
    WriteLine($"[PIPELINE] Completed {context.CommandName}");
  }
}

public sealed class PerformanceBehavior : INuruBehavior
{
  private const int SlowThresholdMs = 500;

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    Stopwatch sw = Stopwatch.StartNew();
    await proceed();
    sw.Stop();

    if (sw.ElapsedMilliseconds > SlowThresholdMs)
      WriteLine($"[PERF] SLOW: {context.CommandName} took {sw.ElapsedMilliseconds}ms");
    else
      WriteLine($"[PERF] {context.CommandName} took {sw.ElapsedMilliseconds}ms");
  }
}

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("echo", Description = "Echo message (Endpoint with unified pipeline)")]
public sealed class EchoCommand : ICommand<Unit>
{
  [Parameter] public string Message { get; set; } = "";

  public sealed class Handler : ICommandHandler<EchoCommand, Unit>
  {
    public ValueTask<Unit> Handle(EchoCommand c, CancellationToken ct)
    {
      WriteLine($"Echo: {c.Message}");
      return default;
    }
  }
}

[NuruRoute("slow", Description = "Slow operation to trigger performance warning")]
public sealed class SlowCommand : ICommand<Unit>
{
  [Parameter] public int Delay { get; set; } = 600;

  public sealed class Handler : ICommandHandler<SlowCommand, Unit>
  {
    public async ValueTask<Unit> Handle(SlowCommand c, CancellationToken ct)
    {
      WriteLine($"Starting {c.Delay}ms operation...");
      await Task.Delay(c.Delay, ct);
      WriteLine("Operation complete");
      return default;
    }
  }
}
