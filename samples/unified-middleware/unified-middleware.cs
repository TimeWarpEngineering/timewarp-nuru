#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-logging/timewarp-nuru-logging.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

// ═══════════════════════════════════════════════════════════════════════════════
// UNIFIED MIDDLEWARE - ONE PIPELINE FOR ALL ROUTES
// ═══════════════════════════════════════════════════════════════════════════════
//
// KEY INSIGHT: There is ONE Mediator pipeline, not two separate pipelines.
//
// Delegate routes are wrapped in DelegateRequest (which implements IRequest<T>)
// and sent through IMediator.Send(). This means open generic behaviors like
// LoggingBehavior<,> automatically apply to ALL requests:
//   - DelegateRequest (for delegate routes)
//   - EchoCommand, SlowCommand, etc. (for mediator routes)
//
// REQUIRED PACKAGES:
//   #:package Mediator.Abstractions    - Interfaces (IRequest, IRequestHandler, IPipelineBehavior)
//   #:package Mediator.SourceGenerator - Generates AddMediator() in YOUR assembly
//
// TRY THESE COMMANDS:
//   ./unified-middleware.cs add 5 3        # Shows "[PIPELINE] Handling DelegateRequest"
//   ./unified-middleware.cs echo "hello"   # Shows "[PIPELINE] Handling EchoCommand"
//   ./unified-middleware.cs slow 600       # Shows "[PIPELINE] Handling SlowCommand" + slow warning
//   ./unified-middleware.cs multiply 4 7   # Shows "[PIPELINE] Handling DelegateRequest"
//
// NOTICE: The same LoggingBehavior<,> handles both DelegateRequest and EchoCommand.
// No separate "delegate behaviors" are needed - open generics catch everything!
//
// COMMON ERROR:
//   "No service for type 'Mediator.IMediator' has been registered"
//   SOLUTION: Install BOTH packages AND call services.AddMediator(options => {...})
// ═══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)
  // =========================================================================
  // DELEGATE ROUTES - Wrapped in DelegateRequest, flows through same pipeline
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
  // MEDIATOR ROUTES - Specific IRequest types, flows through same pipeline
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
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  // Register Mediator with open generic pipeline behaviors.
  // These apply to ALL IRequest<T> types including DelegateRequest.
  // Behaviors execute in array order: first = outermost (wraps everything).
  services.AddMediator(options =>
  {
    options.PipelineBehaviors =
    [
      typeof(LoggingBehavior<,>),      // Applies to ALL requests (DelegateRequest, EchoCommand, etc.)
      typeof(PerformanceBehavior<,>),  // Applies to ALL requests (DelegateRequest, EchoCommand, etc.)
    ];
  });
}

// =============================================================================
// OPEN GENERIC PIPELINE BEHAVIORS - Apply to ALL IRequest<T> types
// =============================================================================
// DelegateRequest implements IRequest<DelegateResponse>, so these behaviors
// automatically apply to delegate routes without any special configuration.
// =============================================================================

/// <summary>
/// Logging behavior that applies to ALL request types.
/// For delegate routes, TMessage is DelegateRequest.
/// For mediator routes, TMessage is the specific command type (e.g., EchoCommand).
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
/// Performance behavior that applies to ALL request types.
/// Warns when any request (delegate or mediator) exceeds threshold.
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
