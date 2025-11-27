#!/usr/bin/dotnet --
// pipeline-middleware - Demonstrates TimeWarp.Mediator pipeline behaviors for cross-cutting concerns
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj

using System.Diagnostics;
using TimeWarp.Nuru;
using TimeWarp.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static System.Console;

// Pipeline Middleware Sample
// ==========================
// This sample demonstrates TimeWarp.Mediator's pipeline behaviors (middleware)
// for implementing cross-cutting concerns like logging, performance monitoring,
// telemetry, validation, and more.
//
// Pipeline behaviors execute in registration order, wrapping the command handler
// like layers of an onion. Each behavior can execute code before and after the
// inner handler(s).

NuruApp app = new NuruAppBuilder()
  .UseConsoleLogging(LogLevel.Information)
  .AddDependencyInjection(config => config.RegisterServicesFromAssembly(typeof(EchoCommand).Assembly))
  .ConfigureServices
  (
    (services, config) =>
    {
      // Register pipeline behaviors in execution order (outermost to innermost)
      // The order here determines the order behaviors wrap the handler
      services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
      services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
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
    public Task Handle(EchoCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Echo: {request.Message}");
      return Task.CompletedTask;
    }
  }
}

/// <summary>Slow command that triggers the performance warning.</summary>
public sealed class SlowCommand : IRequest
{
  public int Delay { get; set; }

  public sealed class Handler : IRequestHandler<SlowCommand>
  {
    public async Task Handle(SlowCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Starting slow operation ({request.Delay}ms)...");
      await Task.Delay(request.Delay, cancellationToken);
      WriteLine("Slow operation completed.");
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
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : IRequest<TResponse>
{
  private readonly ILogger<LoggingBehavior<TRequest, TResponse>> Logger;

  public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
  {
    Logger = logger;
  }

  public async Task<TResponse> Handle
  (
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  )
  {
    string requestName = typeof(TRequest).Name;
    Logger.LogInformation("[PIPELINE] Handling {RequestName}", requestName);

    try
    {
      TResponse response = await next();
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
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : IRequest<TResponse>
{
  private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> Logger;
  private const int SlowThresholdMs = 500;

  public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
  {
    Logger = logger;
  }

  public async Task<TResponse> Handle
  (
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  )
  {
    Stopwatch stopwatch = Stopwatch.StartNew();

    TResponse response = await next();

    stopwatch.Stop();

    if (stopwatch.ElapsedMilliseconds > SlowThresholdMs)
    {
      string requestName = typeof(TRequest).Name;
      Logger.LogWarning
      (
        "[PERFORMANCE] {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
        requestName,
        stopwatch.ElapsedMilliseconds,
        SlowThresholdMs
      );
    }

    return response;
  }
}
