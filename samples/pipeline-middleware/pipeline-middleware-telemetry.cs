#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-logging/timewarp-nuru-logging.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

// ═══════════════════════════════════════════════════════════════════════════════
// TELEMETRY PIPELINE MIDDLEWARE - DISTRIBUTED TRACING
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates OpenTelemetry-compatible distributed tracing using
// System.Diagnostics.Activity. The TelemetryBehavior creates Activity spans
// for observability in tracing backends (Jaeger, Zipkin, Aspire Dashboard).
//
// ACTIVITY TAGS CAPTURED:
//   - command.type: Full type name of the command
//   - command.name: Simple type name of the command
//   - error.type: Exception type (on failure)
//   - error.message: Exception message (on failure)
//
// RUN THIS SAMPLE:
//   ./pipeline-middleware-telemetry.cs trace database-query
//   ./pipeline-middleware-telemetry.cs trace api-call
//
// NOTE: Without an OpenTelemetry listener configured, Activity data is not
// captured. See aspire-telemetry sample for full OTLP integration.
// ═══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)
  // Trace command to demonstrate telemetry/distributed tracing
  .Map<TraceCommand>("trace {operation}")
    .WithDescription("Demonstrate OpenTelemetry-compatible distributed tracing with Activity")
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  // Register Mediator with telemetry behavior.
  // TelemetryBehavior should be outermost to capture full execution span.
  services.AddMediator(options =>
  {
    options.PipelineBehaviors =
    [
      typeof(TelemetryBehavior<,>),   // Outermost: captures full execution span
      typeof(LoggingBehavior<,>)
    ];
  });
}

// =============================================================================
// COMMANDS
// =============================================================================

/// <summary>
/// Trace command that demonstrates OpenTelemetry-compatible distributed tracing.
/// The TelemetryBehavior creates Activity spans for observability.
/// </summary>
public sealed class TraceCommand : IRequest
{
  /// <summary>Name of the operation being traced.</summary>
  public string Operation { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<TraceCommand>
  {
    public async ValueTask<Unit> Handle(TraceCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"[TRACE] Starting operation: {request.Operation}");

      // Simulate some work
      await Task.Delay(100, cancellationToken);

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

      WriteLine($"[TRACE] Operation '{request.Operation}' completed");
      return Unit.Value;
    }
  }
}

// =============================================================================
// PIPELINE BEHAVIORS
// =============================================================================

/// <summary>
/// Simple logging behavior for observability.
/// </summary>
public sealed class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  private readonly ILogger<LoggingBehavior<TMessage, TResponse>> Logger;

  public LoggingBehavior(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
  {
    Logger = logger;
  }

  public async ValueTask<TResponse> Handle
  (
    TMessage message,
    MessageHandlerDelegate<TMessage, TResponse> next,
    CancellationToken cancellationToken
  )
  {
    string requestName = typeof(TMessage).Name;
    Logger.LogInformation("[PIPELINE] Handling {RequestName}", requestName);
    TResponse response = await next(message, cancellationToken);
    Logger.LogInformation("[PIPELINE] Completed {RequestName}", requestName);
    return response;
  }
}

/// <summary>
/// Telemetry behavior that creates OpenTelemetry-compatible Activity spans for
/// distributed tracing of CLI command execution.
/// </summary>
/// <remarks>
/// This behavior uses System.Diagnostics.Activity which is the .NET standard for
/// distributed tracing. Activities integrate with OpenTelemetry exporters (Jaeger,
/// Zipkin, OTLP) for visualization in tracing backends.
///
/// Activity tags captured:
/// - command.type: Full type name of the command
/// - command.name: Simple type name of the command
///
/// Status is set to Ok on success, Error on exception.
/// </remarks>
public sealed class TelemetryBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  /// <summary>
  /// ActivitySource for CLI command tracing.
  /// In production, configure OpenTelemetry to listen to this source.
  /// </summary>
  private static readonly ActivitySource CommandActivitySource = new("TimeWarp.Nuru.Commands", "1.0.0");

  private readonly ILogger<TelemetryBehavior<TMessage, TResponse>> Logger;

  public TelemetryBehavior(ILogger<TelemetryBehavior<TMessage, TResponse>> logger)
  {
    Logger = logger;
  }

  public async ValueTask<TResponse> Handle
  (
    TMessage message,
    MessageHandlerDelegate<TMessage, TResponse> next,
    CancellationToken cancellationToken
  )
  {
    string commandName = typeof(TMessage).Name;
    string commandFullName = typeof(TMessage).FullName ?? commandName;

    // Start an Activity for this command execution
    using Activity? activity = CommandActivitySource.StartActivity(commandName, ActivityKind.Internal);

    // Set tags for the activity (visible in tracing tools)
    activity?.SetTag("command.type", commandFullName);
    activity?.SetTag("command.name", commandName);

    Logger.LogDebug("[TELEMETRY] Started activity for {CommandName}, TraceId: {TraceId}",
      commandName,
      activity?.TraceId.ToString() ?? "none");

    try
    {
      TResponse response = await next(message, cancellationToken);

      // Mark as successful
      activity?.SetStatus(ActivityStatusCode.Ok);

      Logger.LogDebug("[TELEMETRY] Activity completed successfully for {CommandName}", commandName);

      return response;
    }
    catch (Exception ex)
    {
      // Mark as error and record exception details
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      activity?.SetTag("error.type", ex.GetType().Name);
      activity?.SetTag("error.message", ex.Message);

      Logger.LogDebug("[TELEMETRY] Activity failed for {CommandName}: {ErrorMessage}",
        commandName,
        ex.Message);

      throw;
    }
  }
}
