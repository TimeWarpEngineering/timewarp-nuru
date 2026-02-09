#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - RETRY PIPELINE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates resilience with exponential backoff using
// filtered behaviors. Only commands implementing IRetryable are retried.
//
// DSL: Endpoint with filtered RetryBehavior registered via .AddBehavior()
//
// PATTERN DEMONSTRATED:
//   - Marker interface (IRetryable) for opt-in retry behavior
//   - Exponential backoff with jitter
//   - Configurable max retries via IRetryable.MaxRetries
//   - Circuit breaker pattern integration ready
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .AddBehavior(typeof(RetryBehavior<IRetryable>))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);

// =============================================================================
// MARKER INTERFACE FOR RETRYABLE COMMANDS
// =============================================================================

/// <summary>
/// Marker interface for commands that support retry.
/// Commands implement this to opt-in to retry behavior.
/// </summary>
public interface IRetryable
{
  int MaxRetries { get; }
}

// =============================================================================
// RETRY BEHAVIOR WITH EXPONENTIAL BACKOFF
// =============================================================================

/// <summary>
/// Retry behavior with exponential backoff and jitter.
/// Only applies to commands implementing IRetryable.
/// </summary>
public sealed class RetryBehavior<TFilter> : INuruBehavior<TFilter> where TFilter : IRetryable
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    if (context.Command is not IRetryable retryable)
    {
      await proceed();
      return;
    }

    int maxRetries = retryable.MaxRetries;
    int attempt = 0;

    while (true)
    {
      attempt++;

      try
      {
        await proceed();
        return; // Success!
      }
      catch (Exception ex) when (attempt <= maxRetries && IsRetryable(ex))
      {
        int delayMs = CalculateBackoff(attempt);
        WriteLine($"[RETRY] Attempt {attempt}/{maxRetries + 1} failed: {ex.Message}");
        WriteLine($"[RETRY] Waiting {delayMs}ms before retry...");
        await Task.Delay(delayMs);
      }
    }
  }

  private static bool IsRetryable(Exception ex)
  {
    // Only retry transient failures
    return ex is TimeoutException
        || ex is IOException
        || ex is HttpRequestException
        || ex is TaskCanceledException;
  }

  private static int CalculateBackoff(int attempt)
  {
    // Exponential backoff: 100ms, 200ms, 400ms, 800ms... + random jitter
    int baseDelay = (int)Math.Pow(2, attempt - 1) * 100;
    int jitter = Random.Shared.Next(0, 100);
    return Math.Min(baseDelay + jitter, 5000); // Cap at 5 seconds
  }
}

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

/// <summary>
/// Reliable endpoint - no retry needed (does NOT implement IRetryable)
/// </summary>
[NuruRoute("ping", Description = "Simple ping (no retry)")]
public sealed class PingCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<PingCommand, Unit>
  {
    public ValueTask<Unit> Handle(PingCommand command, CancellationToken ct)
    {
      WriteLine("Pong!");
      return default;
    }
  }
}

/// <summary>
/// Unreliable endpoint with 3 retries (implements IRetryable)
/// </summary>
[NuruRoute("flaky-api", Description = "Simulate flaky API call with auto-retry")]
public sealed class FlakyApiCommand : ICommand<Unit>, IRetryable
{
  [Parameter(Description = "API endpoint")]
  public string Endpoint { get; set; } = string.Empty;

  public int MaxRetries => 3;

  public sealed class Handler : ICommandHandler<FlakyApiCommand, Unit>
  {
    private static int FailureCount = 0;

    public ValueTask<Unit> Handle(FlakyApiCommand command, CancellationToken ct)
    {
      FailureCount++;

      // Fail first 2 attempts, succeed on 3rd
      if (FailureCount < 3)
      {
        throw new TimeoutException($"API call to {command.Endpoint} timed out (attempt {FailureCount})");
      }

      WriteLine($"✓ API call to {command.Endpoint} succeeded after {FailureCount} attempts");
      FailureCount = 0; // Reset for next run
      return default;
    }
  }
}

/// <summary>
/// Unreliable database operation with 5 retries (implements IRetryable)
/// </summary>
[NuruRoute("db-save", Description = "Save to database with retry on failure")]
public sealed class DbSaveCommand : ICommand<Unit>, IRetryable
{
  [Parameter(Description = "Data to save")]
  public string Data { get; set; } = string.Empty;

  public int MaxRetries => 5;

  public sealed class Handler : ICommandHandler<DbSaveCommand, Unit>
  {
    private static int FailureCount = 0;

    public ValueTask<Unit> Handle(DbSaveCommand command, CancellationToken ct)
    {
      FailureCount++;

      // Fail first 3 attempts with IOException
      if (FailureCount < 4)
      {
        throw new IOException($"Database connection failed (attempt {FailureCount})");
      }

      WriteLine($"✓ Saved '{command.Data}' to database after {FailureCount} attempts");
      FailureCount = 0; // Reset for next run
      return default;
    }
  }
}

/// <summary>
/// Network operation with 2 retries (implements IRetryable)
/// </summary>
[NuruRoute("fetch", Description = "Fetch data with retry on network errors")]
public sealed class FetchCommand : ICommand<string>, IRetryable
{
  [Parameter(Description = "URL to fetch")]
  public string Url { get; set; } = string.Empty;

  public int MaxRetries => 2;

  public sealed class Handler : ICommandHandler<FetchCommand, string>
  {
    private static int FailureCount = 0;

    public ValueTask<string> Handle(FetchCommand command, CancellationToken ct)
    {
      FailureCount++;

      if (FailureCount < 2)
      {
        throw new HttpRequestException($"Network error fetching {command.Url}");
      }

      string result = $"Data from {command.Url}";
      WriteLine($"✓ Fetched successfully after {FailureCount} attempts");
      FailureCount = 0;
      return new ValueTask<string>(result);
    }
  }
}
