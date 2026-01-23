#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// UNIFIED PIPELINE MIDDLEWARE - ONE PIPELINE FOR ALL ROUTES
// ═══════════════════════════════════════════════════════════════════════════════
//
// KEY INSIGHT: TimeWarp.Nuru has ONE behavior pipeline that applies to ALL routes.
//
// This sample demonstrates that:
//   1. DELEGATE ROUTES (inline lambdas via .WithHandler()) and
//   2. ENDPOINTS ([NuruRoute] classes with nested Handler)
//
// BOTH flow through the SAME INuruBehavior pipeline. There is no separate
// "delegate pipeline" vs "command pipeline" - behaviors are unified.
//
// ═══════════════════════════════════════════════════════════════════════════════
// HOW IT WORKS
// ═══════════════════════════════════════════════════════════════════════════════
//
// 1. Register behaviors with .AddBehavior(typeof(MyBehavior))
//    - Behaviors implement INuruBehavior
//    - Execute in registration order (first = outermost)
//    - Each behavior wraps the next via HandleAsync(context, proceed)
//
// 2. Define routes using EITHER pattern:
//
//    DELEGATE ROUTES (simple, inline):
//      .Map("add {x:int} {y:int}")
//        .WithHandler((int x, int y) => Console.WriteLine($"{x} + {y} = {x + y}"))
//        .Done()
//
//    ENDPOINTS (testable, DI-friendly):
//      [NuruRoute("echo {message}")]
//      public sealed class EchoCommand : ICommand<Unit>
//      {
//        [Parameter(Order = 0)]
//        public string Message { get; set; } = "";
//
//        public sealed class Handler : ICommandHandler<EchoCommand, Unit>
//        {
//          public ValueTask<Unit> Handle(EchoCommand cmd, CancellationToken ct) { ... }
//        }
//      }
//
// 3. The source generator discovers [NuruRoute] classes automatically
//    and generates invocation code that includes the behavior pipeline.
//
// ═══════════════════════════════════════════════════════════════════════════════
// BEHAVIORS DEMONSTRATED
// ═══════════════════════════════════════════════════════════════════════════════
//
// LoggingBehavior:
//   - Logs "[PIPELINE] Handling {CommandName}" before execution
//   - Logs "[PIPELINE] Completed {CommandName}" after execution
//   - Catches and logs exceptions
//
// PerformanceBehavior:
//   - Times command execution
//   - Warns when commands exceed 500ms threshold
//
// ═══════════════════════════════════════════════════════════════════════════════
// TRY THESE COMMANDS
// ═══════════════════════════════════════════════════════════════════════════════
//
// Delegate routes (inline handlers):
//   ./unified-middleware.cs add 5 3           # Shows pipeline wrapping delegate
//   ./unified-middleware.cs multiply 4 7      # Shows pipeline wrapping delegate
//   ./unified-middleware.cs greet World       # Shows pipeline wrapping delegate
//
// Endpoints ([NuruRoute] commands):
//   ./unified-middleware.cs echo "hello"      # Shows pipeline wrapping command
//   ./unified-middleware.cs slow 600          # Shows performance warning (>500ms)
//
// NOTICE: The SAME LoggingBehavior and PerformanceBehavior wrap BOTH types!
// The pipeline is truly unified - no special configuration needed.
//
// ═══════════════════════════════════════════════════════════════════════════════
// WHEN TO USE EACH PATTERN
// ═══════════════════════════════════════════════════════════════════════════════
//
// DELEGATE ROUTES are ideal for:
//   - Simple commands with no dependencies
//   - Quick prototyping
//   - Scripts and one-off tools
//
// ENDPOINTS are ideal for:
//   - Commands needing dependency injection
//   - Unit-testable handlers
//   - Complex business logic
//   - Reusable command libraries
//
// The unified pipeline means you can MIX both styles freely in the same app!
//
// ═══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  // =========================================================================
  // REGISTER BEHAVIORS - Apply to ALL routes (delegate routes AND endpoints)
  // =========================================================================
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(PerformanceBehavior))
  // =========================================================================
  // DELEGATE ROUTES - Inline lambdas, wrapped by the same behavior pipeline
  // =========================================================================
  .Map("add {x:int} {y:int}")
    .WithHandler((int x, int y) => WriteLine($"Result: {x} + {y} = {x + y}"))
    .WithDescription("Add two numbers (delegate route with pipeline)")
    .AsQuery()
    .Done()
  .Map("multiply {x:int} {y:int}")
    .WithHandler((int x, int y) => WriteLine($"Result: {x} × {y} = {x * y}"))
    .WithDescription("Multiply two numbers (delegate route with pipeline)")
    .AsQuery()
    .Done()
  .Map("greet {name}")
    .WithHandler((string name) => WriteLine($"Hello, {name}!"))
    .WithDescription("Greet someone (delegate route with pipeline)")
    .AsCommand()
    .Done()
  // =========================================================================
  // ENDPOINTS - [NuruRoute] classes below are auto-discovered
  // The EchoCommand and SlowCommand classes are picked up by the generator
  // and wrapped by the SAME behavior pipeline as the delegate routes above.
  // =========================================================================
  .Build();

return await app.RunAsync(args);

// =============================================================================
// PIPELINE BEHAVIORS - Apply to ALL routes uniformly
// =============================================================================

/// <summary>
/// Logging behavior that logs request entry and exit.
/// Applies to both delegate routes and endpoints.
/// </summary>
public sealed class LoggingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    WriteLine($"[PIPELINE] Handling {context.CommandName}");

    try
    {
      await proceed();
      WriteLine($"[PIPELINE] Completed {context.CommandName}");
    }
    catch (Exception ex)
    {
      WriteLine($"[PIPELINE] Error handling {context.CommandName}: {ex.Message}");
      throw;
    }
  }
}

/// <summary>
/// Performance behavior that times command execution and warns on slow commands.
/// Applies to both delegate routes and endpoints.
/// </summary>
public sealed class PerformanceBehavior : INuruBehavior
{
  private const int SlowThresholdMs = 500;

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    Stopwatch stopwatch = Stopwatch.StartNew();

    await proceed();

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

// =============================================================================
// ENDPOINTS - Auto-discovered by the source generator
// =============================================================================
// These [NuruRoute] classes are found at compile-time and integrated into
// the same routing and behavior pipeline as the delegate routes above.
// =============================================================================

/// <summary>
/// Echo command - demonstrates endpoint flowing through unified pipeline.
/// </summary>
[NuruRoute("echo", Description = "Echo a message back (endpoint with pipeline)")]
public sealed class EchoCommand : ICommand<Unit>
{
  [Parameter]
  public string Message { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<EchoCommand, Unit>
  {
    public ValueTask<Unit> Handle(EchoCommand command, CancellationToken cancellationToken)
    {
      WriteLine($"Echo: {command.Message}");
      return default;
    }
  }
}

/// <summary>
/// Slow command - demonstrates performance monitoring in unified pipeline.
/// Use delay > 500ms to trigger performance warning.
/// </summary>
[NuruRoute("slow", Description = "Simulate slow operation in ms (endpoint with pipeline)")]
public sealed class SlowCommand : ICommand<Unit>
{
  [Parameter]
  public int Delay { get; set; }

  public sealed class Handler : ICommandHandler<SlowCommand, Unit>
  {
    public async ValueTask<Unit> Handle(SlowCommand command, CancellationToken cancellationToken)
    {
      WriteLine($"Starting slow operation ({command.Delay}ms)...");
      await Task.Delay(command.Delay, cancellationToken);
      WriteLine("Slow operation completed.");
      return Unit.Value;
    }
  }
}
