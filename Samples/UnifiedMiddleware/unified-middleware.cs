#!/usr/bin/dotnet --
// unified-middleware - Demonstrates unified middleware for both delegate and Mediator routes
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

// Unified Middleware Sample
// =========================
// This sample demonstrates that pipeline behaviors apply uniformly to BOTH:
// - Delegate routes (simple lambdas)
// - Mediator routes (IRequest commands)
//
// When DI is enabled and pipeline behaviors are registered for DelegateRequest,
// delegate routes are wrapped in DelegateRequest and sent through IMediator.Send().
// The DelegateRequestHandler (defined in TimeWarp.Nuru) is discovered by the
// Mediator source generator, and pipeline behaviors execute automatically.
//
// This ensures cross-cutting concerns apply consistently regardless of route type.
//
// Try these commands:
//   ./unified-middleware.cs add 5 3        # Delegate route - shows pipeline logging
//   ./unified-middleware.cs echo "hello"   # Mediator route - shows pipeline logging
//   ./unified-middleware.cs slow 600       # Mediator route - triggers slow warning
//   ./unified-middleware.cs multiply 4 7   # Delegate route - shows pipeline logging

NuruApp app = new NuruAppBuilder()
  .UseConsoleLogging(LogLevel.Information)
  .AddDependencyInjection()
  .ConfigureServices
  (
    (services, config) =>
    {
      // Register Mediator - source generator discovers handlers in THIS assembly
      services.AddMediator();

      // Register pipeline behaviors for Mediator commands (explicit IRequest)
      services.AddSingleton<IPipelineBehavior<EchoCommand, Unit>, LoggingBehavior<EchoCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<EchoCommand, Unit>, PerformanceBehavior<EchoCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<SlowCommand, Unit>, LoggingBehavior<SlowCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<SlowCommand, Unit>, PerformanceBehavior<SlowCommand, Unit>>();

      // Register pipeline behaviors for delegate routes (unified middleware)
      // These apply to ALL delegate-based routes!
      services.AddSingleton<IPipelineBehavior<DelegateRequest, DelegateResponse>, DelegateLoggingBehavior>();
      services.AddSingleton<IPipelineBehavior<DelegateRequest, DelegateResponse>, DelegatePerformanceBehavior>();
    }
  )
  // =========================================================================
  // DELEGATE ROUTES - These now receive pipeline behaviors too!
  // =========================================================================
  .Map
  (
    pattern: "add {x:int} {y:int}",
    handler: (int x, int y) =>
    {
      int result = x + y;
      WriteLine($"Result: {x} + {y} = {result}");
      return result;
    },
    description: "Add two numbers (delegate route with pipeline)"
  )
  .Map
  (
    pattern: "multiply {x:int} {y:int}",
    handler: (int x, int y) =>
    {
      int result = x * y;
      WriteLine($"Result: {x} × {y} = {result}");
      return result;
    },
    description: "Multiply two numbers (delegate route with pipeline)"
  )
  .Map
  (
    pattern: "greet {name}",
    handler: (string name) => WriteLine($"Hello, {name}!"),
    description: "Greet someone (delegate route with pipeline)"
  )
  // =========================================================================
  // MEDIATOR ROUTES - These have pipeline behaviors as usual
  // =========================================================================
  .Map<EchoCommand>
  (
    pattern: "echo {message}",
    description: "Echo a message back (Mediator route with pipeline)"
  )
  .Map<SlowCommand>
  (
    pattern: "slow {delay:int}",
    description: "Simulate slow operation in ms (Mediator route with pipeline)"
  )
  .AddAutoHelp()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// PIPELINE BEHAVIORS FOR DELEGATE ROUTES
// =============================================================================

/// <summary>
/// Logging behavior for delegate routes.
/// This demonstrates unified middleware - same pattern as Mediator behaviors.
/// </summary>
public sealed class DelegateLoggingBehavior : IPipelineBehavior<DelegateRequest, DelegateResponse>
{
  private readonly ILogger<DelegateLoggingBehavior> Logger;
  private readonly RouteExecutionContext ExecutionContext;

  public DelegateLoggingBehavior(ILogger<DelegateLoggingBehavior> logger, RouteExecutionContext executionContext)
  {
    Logger = logger;
    ExecutionContext = executionContext;
  }

  public async ValueTask<DelegateResponse> Handle
  (
    DelegateRequest message,
    MessageHandlerDelegate<DelegateRequest, DelegateResponse> next,
    CancellationToken cancellationToken
  )
  {
    // Access route metadata from the execution context
    Logger.LogInformation
    (
      "[DELEGATE PIPELINE] Handling route: {RoutePattern}",
      ExecutionContext.RoutePattern
    );

    try
    {
      DelegateResponse response = await next(message, cancellationToken);
      Logger.LogInformation("[DELEGATE PIPELINE] Completed route: {RoutePattern}", ExecutionContext.RoutePattern);
      return response;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[DELEGATE PIPELINE] Error handling route: {RoutePattern}", ExecutionContext.RoutePattern);
      throw;
    }
  }
}

/// <summary>
/// Performance behavior for delegate routes.
/// Warns when delegate execution exceeds threshold.
/// </summary>
public sealed class DelegatePerformanceBehavior : IPipelineBehavior<DelegateRequest, DelegateResponse>
{
  private readonly ILogger<DelegatePerformanceBehavior> Logger;
  private readonly RouteExecutionContext ExecutionContext;
  private const int SlowThresholdMs = 500;

  public DelegatePerformanceBehavior(ILogger<DelegatePerformanceBehavior> logger, RouteExecutionContext executionContext)
  {
    Logger = logger;
    ExecutionContext = executionContext;
  }

  public async ValueTask<DelegateResponse> Handle
  (
    DelegateRequest message,
    MessageHandlerDelegate<DelegateRequest, DelegateResponse> next,
    CancellationToken cancellationToken
  )
  {
    Stopwatch stopwatch = Stopwatch.StartNew();

    DelegateResponse response = await next(message, cancellationToken);

    stopwatch.Stop();

    if (stopwatch.ElapsedMilliseconds > SlowThresholdMs)
    {
      Logger.LogWarning
      (
        "[DELEGATE PERFORMANCE] Route {RoutePattern} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
        ExecutionContext.RoutePattern,
        stopwatch.ElapsedMilliseconds,
        SlowThresholdMs
      );
    }
    else
    {
      Logger.LogInformation
      (
        "[DELEGATE PERFORMANCE] Route {RoutePattern} completed in {ElapsedMs}ms",
        ExecutionContext.RoutePattern,
        stopwatch.ElapsedMilliseconds
      );
    }

    return response;
  }
}

// =============================================================================
// PIPELINE BEHAVIORS FOR MEDIATOR ROUTES (same as before)
// =============================================================================

/// <summary>
/// Logging behavior for Mediator commands.
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
    Logger.LogInformation("[MEDIATOR PIPELINE] Handling {RequestName}", requestName);

    try
    {
      TResponse response = await next(message, cancellationToken);
      Logger.LogInformation("[MEDIATOR PIPELINE] Completed {RequestName}", requestName);
      return response;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[MEDIATOR PIPELINE] Error handling {RequestName}", requestName);
      throw;
    }
  }
}

/// <summary>
/// Performance behavior for Mediator commands.
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
        "[MEDIATOR PERFORMANCE] {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
        requestName,
        stopwatch.ElapsedMilliseconds,
        SlowThresholdMs
      );
    }
    else
    {
      Logger.LogInformation
      (
        "[MEDIATOR PERFORMANCE] {RequestName} completed in {ElapsedMs}ms",
        requestName,
        stopwatch.ElapsedMilliseconds
      );
    }

    return response;
  }
}

// =============================================================================
// MEDIATOR COMMANDS
// =============================================================================

/// <summary>Simple echo command.</summary>
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

/// <summary>Slow command to demonstrate performance monitoring.</summary>
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
