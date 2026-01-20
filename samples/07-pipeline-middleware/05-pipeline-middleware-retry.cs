#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// RETRY PIPELINE MIDDLEWARE - RESILIENCE PATTERNS
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates resilience patterns using retry with exponential
// backoff. Only routes implementing IRetryable will have retry logic applied.
//
// KEY CONCEPT: Filtered Behaviors with INuruBehavior<TFilter>
//   RetryBehavior implements INuruBehavior<IRetryable> which means it ONLY
//   executes for routes that implement IRetryable via .Implements<T>().
//
// RETRY PATTERN:
//   - Uses INuruBehavior<IRetryable> for selective application
//   - Exponential backoff: 2^attempt seconds between retries (capped at 2s for demo)
//   - Retries only on transient exceptions (network, timeout, I/O)
//
// RUN THIS SAMPLE:
//   ./05-pipeline-middleware-retry.cs flaky 0    # Succeeds immediately
//   ./05-pipeline-middleware-retry.cs flaky 2    # Fails twice, then succeeds
//   ./05-pipeline-middleware-retry.cs flaky 5    # Fails all retries (max 3)
//   ./05-pipeline-middleware-retry.cs echo Hello # No retry (doesn't implement IRetryable)
// ═══════════════════════════════════════════════════════════════════════════════

using System.Net;
using TimeWarp.Nuru;
using static System.Console;

#pragma warning disable NURU_H002 // Handler uses closure - intentional for demo

NuruApp app = NuruApp.CreateBuilder(args)
  // Register behaviors - LoggingBehavior applies to ALL routes, RetryBehavior only to IRetryable
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(RetryBehavior))
  // Flaky command that simulates transient failures with retry
  .Map("flaky {failCount:int}")
    .WithDescription("Simulate transient failures (retries up to 3 times with exponential backoff)")
    .Implements<IRetryable>(x => x.MaxRetries = 3)
    .WithHandler((int failCount) =>
    {
      FlakyState.AttemptCount++;

      if (FlakyState.AttemptCount <= failCount)
      {
        WriteLine($"[FLAKY] Attempt {FlakyState.AttemptCount}: Simulating transient failure...");
        throw new HttpRequestException("Simulated transient network error");
      }

      WriteLine($"[FLAKY] Attempt {FlakyState.AttemptCount}: Success!");
      FlakyState.AttemptCount = 0; // Reset for next run
    })
    .Done()
  // Echo command - no retry (doesn't implement IRetryable)
  .Map("echo {message}")
    .WithDescription("Echo a message (no retry behavior)")
    .WithHandler((string message) => WriteLine($"Echo: {message}"))
    .Done()
  .Build();

#pragma warning restore NURU_H002

return await app.RunAsync(args);

// =============================================================================
// MARKER INTERFACE
// =============================================================================

/// <summary>
/// Marker interface for commands that should retry on transient failures.
/// Only routes implementing this interface (via .Implements&lt;IRetryable&gt;())
/// will have retry logic applied by the RetryBehavior.
/// </summary>
public interface IRetryable
{
  /// <summary>Maximum number of retry attempts.</summary>
  int MaxRetries { get; set; }
}

/// <summary>Static state for tracking retry attempts in demo.</summary>
public static class FlakyState
{
  public static int AttemptCount;
}

// =============================================================================
// PIPELINE BEHAVIORS
// =============================================================================

/// <summary>
/// Simple logging behavior for observability.
/// Applies to ALL routes (implements INuruBehavior, not INuruBehavior&lt;T&gt;).
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

/// <summary>
/// Retry behavior that implements exponential backoff for transient failures.
/// This behavior ONLY applies to routes that implement IRetryable via .Implements&lt;T&gt;(),
/// demonstrating filtered behavior application.
/// </summary>
/// <remarks>
/// Retries on transient exceptions:
/// - HttpRequestException (network errors)
/// - TimeoutException (operation timeouts)
/// - IOException (I/O failures)
///
/// Uses exponential backoff: 2^attempt seconds between retries (capped at 2s for demo).
/// </remarks>
public sealed class RetryBehavior : INuruBehavior<IRetryable>
{
  public async ValueTask HandleAsync(BehaviorContext<IRetryable> context, Func<ValueTask> proceed)
  {
    // context.Command is already IRetryable - no casting needed!
    int maxRetries = context.Command.MaxRetries;

    for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
    {
      try
      {
        await proceed();
        return; // Success - exit retry loop
      }
      catch (Exception ex) when (IsTransientException(ex) && attempt <= maxRetries)
      {
        // Cap delay at 2 seconds for demo purposes
        TimeSpan delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 2));
        WriteLine
        (
          $"[RETRY] {context.CommandName} attempt {attempt}/{maxRetries + 1} failed: {ex.Message}. " +
          $"Retrying in {delay.TotalSeconds}s..."
        );
        await Task.Delay(delay, context.CancellationToken);
      }
    }

    // This should not be reached - the last attempt either succeeds or throws
    throw new InvalidOperationException($"Retry logic error for {context.CommandName}");
  }

  /// <summary>
  /// Determines if an exception is transient and should trigger a retry.
  /// </summary>
  private static bool IsTransientException(Exception ex) =>
    ex is HttpRequestException or TimeoutException or IOException;
}
