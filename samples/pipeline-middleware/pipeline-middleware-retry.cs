#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-logging/timewarp-nuru-logging.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

// ═══════════════════════════════════════════════════════════════════════════════
// RETRY PIPELINE MIDDLEWARE - RESILIENCE PATTERNS
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates resilience patterns using retry with exponential
// backoff. Only commands implementing IRetryable will have retry logic applied.
//
// RETRY PATTERN:
//   - Uses marker interface IRetryable to opt-in to retry behavior
//   - Exponential backoff: 2^attempt seconds between retries
//   - Retries only on transient exceptions (network, timeout, I/O)
//
// RUN THIS SAMPLE:
//   ./pipeline-middleware-retry.cs flaky 0    # Succeeds immediately
//   ./pipeline-middleware-retry.cs flaky 2    # Fails twice, then succeeds
//   ./pipeline-middleware-retry.cs flaky 5    # Fails all retries (max 3)
// ═══════════════════════════════════════════════════════════════════════════════

using System.Net;
using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)
  // Flaky command that simulates transient failures with retry
  .Map<FlakyCommand>("flaky {failCount:int}")
    .WithDescription("Simulate transient failures (retries up to 3 times with exponential backoff)")
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  // Register Mediator with retry behavior.
  // The behavior checks for IRetryable at runtime.
  services.AddMediator(options =>
  {
    options.PipelineBehaviors =
    [
      typeof(LoggingBehavior<,>),
      typeof(RetryBehavior<,>)
    ];
  });
}

// =============================================================================
// COMMANDS
// =============================================================================

/// <summary>
/// Flaky command that simulates transient failures.
/// Demonstrates retry behavior with exponential backoff for resilience.
/// The command fails the first N times (based on FailCount) then succeeds.
/// </summary>
public sealed class FlakyCommand : IRequest, IRetryable
{
  /// <summary>Number of times to fail before succeeding.</summary>
  public int FailCount { get; set; }

  /// <summary>Maximum retry attempts (from IRetryable).</summary>
  public int MaxRetries => 3;

  /// <summary>Track attempt count across retries (static for demo purposes).</summary>
  private static int AttemptCount;

  public sealed class Handler : IRequestHandler<FlakyCommand>
  {
    public ValueTask<Unit> Handle(FlakyCommand request, CancellationToken cancellationToken)
    {
      AttemptCount++;

      if (AttemptCount <= request.FailCount)
      {
        WriteLine($"[FLAKY] Attempt {AttemptCount}: Simulating transient failure...");
        // Reset on last allowed failure so demo can be run multiple times
        if (AttemptCount >= request.FailCount)
        {
          AttemptCount = 0;
        }
        throw new HttpRequestException("Simulated transient network error");
      }

      WriteLine($"[FLAKY] Attempt {AttemptCount}: Success!");
      AttemptCount = 0; // Reset for next run
      return default;
    }
  }
}

// =============================================================================
// MARKER INTERFACE
// =============================================================================

/// <summary>
/// Marker interface for commands that should retry on transient failures.
/// Only commands implementing this interface will have retry logic applied
/// by the RetryBehavior.
/// </summary>
public interface IRetryable
{
  /// <summary>Maximum number of retry attempts (default: 3).</summary>
  int MaxRetries => 3;
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
/// Retry behavior that implements exponential backoff for transient failures.
/// This behavior only applies retry logic to commands that implement IRetryable,
/// demonstrating resilience patterns for CLI apps interacting with external services.
/// </summary>
/// <remarks>
/// Retries on transient exceptions:
/// - HttpRequestException (network errors)
/// - TimeoutException (operation timeouts)
/// - IOException (I/O failures)
///
/// Uses exponential backoff: 2^attempt seconds between retries.
/// </remarks>
public sealed class RetryBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  private readonly ILogger<RetryBehavior<TMessage, TResponse>> Logger;

  public RetryBehavior(ILogger<RetryBehavior<TMessage, TResponse>> logger)
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
    // Only apply retry logic to commands that implement IRetryable
    if (message is not IRetryable retryable)
    {
      return await next(message, cancellationToken);
    }

    int maxRetries = retryable.MaxRetries;
    string requestName = typeof(TMessage).Name;

    for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
    {
      try
      {
        return await next(message, cancellationToken);
      }
      catch (Exception ex) when (IsTransientException(ex) && attempt <= maxRetries)
      {
        TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
        Logger.LogWarning
        (
          "[RETRY] {RequestName} attempt {Attempt}/{MaxAttempts} failed: {ErrorMessage}. Retrying in {DelaySeconds}s...",
          requestName,
          attempt,
          maxRetries + 1,
          ex.Message,
          delay.TotalSeconds
        );
        await Task.Delay(delay, cancellationToken);
      }
    }

    // This should not be reached - the last attempt either succeeds or throws
    throw new InvalidOperationException($"Retry logic error for {requestName}");
  }

  /// <summary>
  /// Determines if an exception is transient and should trigger a retry.
  /// </summary>
  private static bool IsTransientException(Exception ex) =>
    ex is HttpRequestException or TimeoutException or IOException;
}
