#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

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
// KEY CONCEPT: Using Statement
//   With HandleAsync pattern, we can use 'using' for the Activity lifecycle.
//   No custom State class needed - much simpler than OnBefore/OnAfter pattern!
//
// ACTIVITY TAGS CAPTURED:
//   - command.type: Type name of the command
//   - command.name: Route pattern
//   - correlation.id: Request correlation ID
//   - error.type: Exception type (on failure)
//   - error.message: Exception message (on failure)
//
// BEHAVIOR EXECUTION ORDER:
//   TelemetryBehavior wraps LoggingBehavior wraps Handler
//   Activity span covers the entire nested execution.
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
/// With HandleAsync pattern, we use 'using' for natural Activity lifecycle management.
/// No custom State class needed - much simpler!
/// </remarks>
public sealed class TelemetryBehavior : INuruBehavior
{
  /// <summary>
  /// ActivitySource for CLI command tracing.
  /// In production, configure OpenTelemetry to listen to this source.
  /// </summary>
  private static readonly ActivitySource CommandActivitySource = new("TimeWarp.Nuru.Commands", "1.0.0");

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    // Start activity with 'using' - automatically disposed at end of scope
    using Activity? activity = CommandActivitySource.StartActivity(context.CommandName, ActivityKind.Internal);

    // Set initial tags
    activity?.SetTag("command.type", context.CommandTypeName);
    activity?.SetTag("command.name", context.CommandName);
    activity?.SetTag("correlation.id", context.CorrelationId);

    WriteLine($"[TELEMETRY] Started activity for {context.CommandName}, TraceId: {activity?.TraceId.ToString() ?? "none"}");

    try
    {
      await proceed();
      activity?.SetStatus(ActivityStatusCode.Ok);
      WriteLine($"[TELEMETRY] Activity completed successfully for {context.CommandName}");
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      activity?.SetTag("error.type", ex.GetType().Name);
      activity?.SetTag("error.message", ex.Message);
      WriteLine($"[TELEMETRY] Activity failed for {context.CommandName}: {ex.Message}");
      throw;
    }
  }
}

/// <summary>
/// Simple logging behavior for observability.
/// Registered after TelemetryBehavior so it runs inside the Activity span.
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
      WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Error in {context.CommandName}: {ex.GetType().Name}");
      throw;
    }
  }
}
