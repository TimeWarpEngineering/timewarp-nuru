#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-logging/timewarp-nuru-logging.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// TELEMETRY PIPELINE MIDDLEWARE - DISTRIBUTED TRACING
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates OpenTelemetry-compatible distributed tracing using
// System.Diagnostics.Activity and TimeWarp.Nuru's INuruBehavior pattern.
//
// The TelemetryBehavior creates Activity spans for observability in tracing
// backends (Jaeger, Zipkin, Aspire Dashboard).
//
// KEY CONCEPT: Custom State Class
//   The TelemetryBehavior uses a nested State class to hold the Activity
//   across OnBeforeAsync → OnAfterAsync/OnErrorAsync method calls.
//   This demonstrates per-request state management in behaviors.
//
// ACTIVITY TAGS CAPTURED:
//   - command.type: Full type name of the command
//   - command.name: Simple type name of the command
//   - correlation.id: Request correlation ID from BehaviorContext
//   - error.type: Exception type (on failure)
//   - error.message: Exception message (on failure)
//
// BEHAVIOR EXECUTION ORDER:
//   OnBefore: TelemetryBehavior → LoggingBehavior → Handler
//   OnAfter:  LoggingBehavior → TelemetryBehavior (Activity disposed last)
//
// RUN THIS SAMPLE:
//   ./03-pipeline-middleware-telemetry.cs trace database-query
//   ./03-pipeline-middleware-telemetry.cs trace api-call
//
// NOTE: Without an OpenTelemetry listener configured, Activity data is not
// captured. See aspire-telemetry sample for full OTLP integration.
// ═══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using TimeWarp.Nuru;
using static System.Console;

#pragma warning disable NURU_H002 // Handler uses closure - intentional for demo

NuruCoreApp app = NuruApp.CreateBuilder(args)
  // Register behaviors - TelemetryBehavior outermost to capture full span
  .AddBehavior(typeof(TelemetryBehavior))
  .AddBehavior(typeof(LoggingBehavior))
  // Trace command to demonstrate telemetry/distributed tracing
  .Map("trace {operation}")
    .WithDescription("Demonstrate OpenTelemetry-compatible distributed tracing with Activity")
    .WithHandler(async (string operation) =>
    {
      WriteLine($"[TRACE] Starting operation: {operation}");

      // Simulate some work
      await Task.Delay(100);

      // Show current Activity information if available
      Activity? current = Activity.Current;
      if (current != null)
      {
        WriteLine($"[TRACE] Activity ID: {current.Id}");
        WriteLine($"[TRACE] Activity Name: {current.DisplayName}");
        WriteLine($"[TRACE] TraceId: {current.TraceId}");
        WriteLine($"[TRACE] SpanId: {current.SpanId}");
      }
      else
      {
        WriteLine("[TRACE] No Activity listener configured - Activity data not captured");
        WriteLine("[TRACE] In production, configure OpenTelemetry to capture these traces");
      }

      WriteLine($"[TRACE] Operation '{operation}' completed");
    })
    .Done()
  .Build();

#pragma warning restore NURU_H002

return await app.RunAsync(args);

// =============================================================================
// PIPELINE BEHAVIORS
// =============================================================================

/// <summary>
/// Telemetry behavior that creates OpenTelemetry-compatible Activity spans for
/// distributed tracing of CLI command execution.
/// </summary>
/// <remarks>
/// This behavior uses System.Diagnostics.Activity which is the .NET standard for
/// distributed tracing. Activities integrate with OpenTelemetry exporters (Jaeger,
/// Zipkin, OTLP) for visualization in tracing backends.
///
/// The nested State class holds the Activity instance across method calls,
/// demonstrating the per-request state pattern for behaviors.
/// </remarks>
public sealed class TelemetryBehavior : INuruBehavior
{
  /// <summary>
  /// ActivitySource for CLI command tracing.
  /// In production, configure OpenTelemetry to listen to this source.
  /// </summary>
  private static readonly ActivitySource CommandActivitySource = new("TimeWarp.Nuru.Commands", "1.0.0");

  /// <summary>
  /// Custom State class to hold Activity across OnBefore/OnAfter/OnError calls.
  /// The generator detects this nested State class and creates instances of it.
  /// </summary>
  public sealed class State : BehaviorContext
  {
    public Activity? Activity { get; set; }
  }

  public ValueTask OnBeforeAsync(BehaviorContext context)
  {
    if (context is State state)
    {
      // Start activity and set initial tags
      state.Activity = CommandActivitySource.StartActivity(state.CommandName, ActivityKind.Internal);
      state.Activity?.SetTag("command.type", state.CommandTypeName);
      state.Activity?.SetTag("command.name", state.CommandName);
      state.Activity?.SetTag("correlation.id", state.CorrelationId);

      WriteLine($"[TELEMETRY] Started activity for {state.CommandName}, TraceId: {state.Activity?.TraceId.ToString() ?? "none"}");
    }
    return ValueTask.CompletedTask;
  }

  public ValueTask OnAfterAsync(BehaviorContext context)
  {
    if (context is State state)
    {
      state.Activity?.SetStatus(ActivityStatusCode.Ok);
      WriteLine($"[TELEMETRY] Activity completed successfully for {state.CommandName}");
      state.Activity?.Dispose();
    }
    return ValueTask.CompletedTask;
  }

  public ValueTask OnErrorAsync(BehaviorContext context, Exception exception)
  {
    if (context is State state)
    {
      state.Activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
      state.Activity?.SetTag("error.type", exception.GetType().Name);
      state.Activity?.SetTag("error.message", exception.Message);
      WriteLine($"[TELEMETRY] Activity failed for {state.CommandName}: {exception.Message}");
      state.Activity?.Dispose();
    }
    return ValueTask.CompletedTask;
  }
}

/// <summary>
/// Simple logging behavior for observability.
/// Registered after TelemetryBehavior so it runs inside the Activity span.
/// </summary>
public sealed class LoggingBehavior : INuruBehavior
{
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
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Error in {context.CommandName}: {exception.GetType().Name}");
    return ValueTask.CompletedTask;
  }
}
