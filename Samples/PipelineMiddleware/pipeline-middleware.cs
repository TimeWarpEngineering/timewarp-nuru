#!/usr/bin/dotnet --
// pipeline-middleware - Demonstrates Mediator pipeline behaviors for cross-cutting concerns
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

using System.Diagnostics;
using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static System.Console;

// Pipeline Middleware Sample
// ==========================
// This sample demonstrates martinothamar/Mediator pipeline behaviors (middleware)
// for implementing cross-cutting concerns like logging, performance monitoring,
// telemetry, validation, and more.
//
// Pipeline behaviors execute in registration order, wrapping the command handler
// like layers of an onion. Each behavior can execute code before and after the
// inner handler(s).

NuruApp app = new NuruAppBuilder()
  .UseConsoleLogging(LogLevel.Information)
  .AddDependencyInjection()
  .ConfigureServices
  (
    (services, config) =>
    {
      // Register Mediator - source generator discovers handlers in THIS assembly
      services.AddMediator();

      // Register pipeline behaviors in execution order (outermost to innermost)
      // The order here determines the order behaviors wrap the handler
      //
      // Note: For AOT/runfile scenarios, use explicit generic registrations rather than
      // open generic registration (typeof(IPipelineBehavior<,>)) to avoid trimmer issues.
      services.AddSingleton<IPipelineBehavior<EchoCommand, Unit>, LoggingBehavior<EchoCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<EchoCommand, Unit>, PerformanceBehavior<EchoCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<SlowCommand, Unit>, LoggingBehavior<SlowCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<SlowCommand, Unit>, PerformanceBehavior<SlowCommand, Unit>>();
    }
  )
  // Simple command to demonstrate pipeline
  .Map<EchoCommand>
  (
    pattern: "echo {message}",
    description: "Echo a message back (demonstrates pipeline)"
  )
  // Slow command to trigger performance warning
  .Map<SlowCommand>
  (
    pattern: "slow {delay:int}",
    description: "Simulate slow operation (ms) to demonstrate performance behavior"
  )
  .AddAutoHelp()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// COMMANDS
// =============================================================================

/// <summary>Simple echo command to demonstrate pipeline execution.</summary>
public sealed class EchoCommand : IRequest
{
  public string Message { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<EchoCommand>
  {
    public ValueTask<Unit> Handle(EchoCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Echo: {request.Message}");
      return default;
    }
  }
}

/// <summary>Slow command that triggers the performance warning.</summary>
public sealed class SlowCommand : IRequest
{
  public int Delay { get; set; }

  public sealed class Handler : IRequestHandler<SlowCommand>
  {
    public async ValueTask<Unit> Handle(SlowCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Starting slow operation ({request.Delay}ms)...");
      await Task.Delay(request.Delay, cancellationToken);
      WriteLine("Slow operation completed.");
      return Unit.Value;
    }
  }
}

// =============================================================================
// PIPELINE BEHAVIORS
// =============================================================================

/// <summary>
/// Logging behavior that logs request entry and exit.
/// This is the outermost behavior, so it wraps everything else.
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

    try
    {
      TResponse response = await next(message, cancellationToken);
      Logger.LogInformation("[PIPELINE] Completed {RequestName}", requestName);
      return response;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[PIPELINE] Error handling {RequestName}", requestName);
      throw;
    }
  }
}

/// <summary>
/// Performance behavior that times command execution and warns on slow commands.
/// Demonstrates cross-cutting performance monitoring.
/// </summary>
public sealed class PerformanceBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  private readonly ILogger<PerformanceBehavior<TMessage, TResponse>> Logger;
  private const int SlowThresholdMs = 500;

  public PerformanceBehavior(ILogger<PerformanceBehavior<TMessage, TResponse>> logger)
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
    Stopwatch stopwatch = Stopwatch.StartNew();

    TResponse response = await next(message, cancellationToken);

    stopwatch.Stop();

    string requestName = typeof(TMessage).Name;

    if (stopwatch.ElapsedMilliseconds > SlowThresholdMs)
    {
      Logger.LogWarning
      (
        "[PERFORMANCE] {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
        requestName,
        stopwatch.ElapsedMilliseconds,
        SlowThresholdMs
      );
    }
    else
    {
      Logger.LogInformation
      (
        "[PERFORMANCE] {RequestName} completed in {ElapsedMs}ms",
        requestName,
        stopwatch.ElapsedMilliseconds
      );
    }

    return response;
  }
}
