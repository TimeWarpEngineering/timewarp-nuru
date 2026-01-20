#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// PIPELINE MIDDLEWARE - COMBINED CROSS-CUTTING CONCERNS
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates ALL pipeline behavior patterns using TimeWarp.Nuru's
// source-generated INuruBehavior approach:
//
// BEHAVIORS DEMONSTRATED:
//   1. TelemetryBehavior      - OpenTelemetry-compatible distributed tracing (global)
//   2. LoggingBehavior        - Request entry/exit logging (global)
//   3. ExceptionHandlingBehavior - Consistent error handling (global)
//   4. AuthorizationBehavior  - Permission checks (filtered: IRequireAuthorization)
//   5. RetryBehavior          - Exponential backoff retry (filtered: IRetryable)
//   6. PerformanceBehavior    - Execution timing warnings (global)
//
// KEY CONCEPTS:
//   - Global behaviors (INuruBehavior) apply to ALL routes
//   - Filtered behaviors (INuruBehavior<T>) apply only to routes with .Implements<T>()
//   - Behaviors execute in registration order (first = outermost)
//
// RUN THIS SAMPLE:
//   ./06-pipeline-middleware-combined.cs echo "Hello"      # Basic pipeline demo
//   ./06-pipeline-middleware-combined.cs slow 600          # Performance warning
//   ./06-pipeline-middleware-combined.cs admin delete-all  # Auth denied
//   CLI_AUTHORIZED=1 ./06-pipeline-middleware-combined.cs admin delete-all  # Auth granted
//   ./06-pipeline-middleware-combined.cs flaky 2           # Retry demo
//   ./06-pipeline-middleware-combined.cs error validation  # Exception handling
//   ./06-pipeline-middleware-combined.cs trace api-call    # Telemetry demo
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using TimeWarp.Nuru;
using static System.Console;

#pragma warning disable NURU_H002 // Handler uses closure - intentional for demo

NuruApp app = NuruApp.CreateBuilder(args)
  // Register behaviors in order: first = outermost (wraps everything)
  .AddBehavior(typeof(TelemetryBehavior))       // Outermost: captures full execution span
  .AddBehavior(typeof(LoggingBehavior))         // Logs entry/exit for all commands
  .AddBehavior(typeof(ExceptionHandlingBehavior)) // Catches unhandled exceptions
  .AddBehavior(typeof(AuthorizationBehavior))   // Filtered: only IRequireAuthorization routes
  .AddBehavior(typeof(RetryBehavior))           // Filtered: only IRetryable routes (inside exception handling)
  .AddBehavior(typeof(PerformanceBehavior))     // Innermost: warns on slow commands

  // Simple echo command - demonstrates basic pipeline
  .Map("echo {message}")
    .WithDescription("Echo a message back (demonstrates pipeline)")
    .WithHandler((string message) => WriteLine($"Echo: {message}"))
    .Done()

  // Slow command - triggers performance warning
  .Map("slow {delay:int}")
    .WithDescription("Simulate slow operation (ms) to demonstrate performance behavior")
    .WithHandler(async (int delay) =>
    {
      WriteLine($"Starting slow operation ({delay}ms)...");
      await Task.Delay(delay);
      WriteLine("Slow operation completed.");
    })
    .Done()

  // Admin command - requires authorization via IRequireAuthorization
  .Map("admin {action}")
    .WithDescription("Admin operation requiring authorization (set CLI_AUTHORIZED=1)")
    .Implements<IRequireAuthorization>(x => x.RequiredPermission = "admin:execute")
    .WithHandler((string action) =>
    {
      WriteLine($"Executing admin action: {action}");
      WriteLine("Admin operation completed successfully.");
    })
    .Done()

  // Flaky command - demonstrates retry with exponential backoff
  .Map("flaky {failCount:int}")
    .WithDescription("Simulate transient failures (retries up to 3 times)")
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

  // Error command - demonstrates exception handling
  .Map("error {errorType}")
    .WithDescription("Throw different exception types (validation, auth, argument, unknown)")
    .WithHandler((string errorType) =>
    {
      WriteLine($"Attempting operation that will throw: {errorType}");
      throw errorType.ToLowerInvariant() switch
      {
        "validation" => new ValidationException("Email address is not in a valid format"),
        "auth" => new UnauthorizedAccessException("You do not have permission"),
        "argument" => new ArgumentException("The provided value is out of range", "errorType"),
        "unknown" or _ => new InvalidOperationException("An unexpected internal error occurred")
      };
    })
    .Done()

  // Trace command - demonstrates telemetry/distributed tracing
  .Map("trace {operation}")
    .WithDescription("Demonstrate OpenTelemetry-compatible distributed tracing")
    .WithHandler(async (string operation) =>
    {
      WriteLine($"[TRACE] Starting operation: {operation}");
      await Task.Delay(100);

      Activity? current = Activity.Current;
      if (current != null)
      {
        WriteLine($"[TRACE] Activity ID: {current.Id}");
        WriteLine($"[TRACE] TraceId: {current.TraceId}");
        WriteLine($"[TRACE] SpanId: {current.SpanId}");
      }
      else
      {
        WriteLine("[TRACE] No Activity listener configured - Activity data not captured");
      }
      WriteLine($"[TRACE] Operation '{operation}' completed");
    })
    .Done()
  .Build();

#pragma warning restore NURU_H002

return await app.RunAsync(args);

// =============================================================================
// MARKER INTERFACES
// =============================================================================

/// <summary>
/// Marker interface for routes that require authorization.
/// Only routes with .Implements&lt;IRequireAuthorization&gt;() will have permission checks.
/// </summary>
public interface IRequireAuthorization
{
  string RequiredPermission { get; set; }
}

/// <summary>
/// Marker interface for routes that should retry on transient failures.
/// Only routes with .Implements&lt;IRetryable&gt;() will have retry logic applied.
/// </summary>
public interface IRetryable
{
  int MaxRetries { get; set; }
}

/// <summary>Static state for tracking retry attempts in demo.</summary>
public static class FlakyState
{
  public static int AttemptCount;
}

// =============================================================================
// GLOBAL BEHAVIORS (apply to ALL routes)
// =============================================================================

/// <summary>
/// Telemetry behavior - creates OpenTelemetry-compatible Activity spans.
/// Registered first = outermost, captures full execution span.
/// </summary>
public sealed class TelemetryBehavior : INuruBehavior
{
  private static readonly ActivitySource Source = new("TimeWarp.Nuru.Commands", "1.0.0");

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    using Activity? activity = Source.StartActivity(context.CommandName, ActivityKind.Internal);
    activity?.SetTag("command.name", context.CommandName);
    activity?.SetTag("correlation.id", context.CorrelationId);

    WriteLine($"[TELEMETRY] Started activity, TraceId: {activity?.TraceId.ToString() ?? "none"}");

    try
    {
      await proceed();
      activity?.SetStatus(ActivityStatusCode.Ok);
      WriteLine($"[TELEMETRY] Activity completed successfully");
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      activity?.SetTag("error.type", ex.GetType().Name);
      WriteLine($"[TELEMETRY] Activity failed: {ex.GetType().Name}");
      throw;
    }
  }
}

/// <summary>
/// Logging behavior - logs request entry/exit for all routes.
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
      WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Error: {ex.GetType().Name}");
      throw;
    }
  }
}

/// <summary>
/// Performance behavior - warns on slow command execution.
/// </summary>
public sealed class PerformanceBehavior : INuruBehavior
{
  private const int SlowThresholdMs = 500;

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    var stopwatch = Stopwatch.StartNew();

    try
    {
      await proceed();
    }
    finally
    {
      stopwatch.Stop();
      long elapsed = stopwatch.ElapsedMilliseconds;

      if (elapsed > SlowThresholdMs)
        WriteLine($"[PERFORMANCE] {context.CommandName} took {elapsed}ms - SLOW!");
      else
        WriteLine($"[PERFORMANCE] {context.CommandName} completed in {elapsed}ms");
    }
  }
}

/// <summary>
/// Exception handling behavior - provides consistent error handling.
/// Registered last = innermost, catches all exceptions from handler.
/// </summary>
public sealed class ExceptionHandlingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    try
    {
      await proceed();
    }
    catch (Exception ex)
    {
      string message = ex switch
      {
        ValidationException ve => $"Validation error: {ve.Message}",
        UnauthorizedAccessException ue => $"Access denied: {ue.Message}",
        ArgumentException ae => $"Invalid argument: {ae.Message}",
        _ => "Error: An unexpected error occurred."
      };

      Error.WriteLine($"[EXCEPTION] {message}");
      throw; // Re-throw to let caller handle exit code
    }
  }
}

// =============================================================================
// FILTERED BEHAVIORS (apply only to routes with matching .Implements<T>())
// =============================================================================

/// <summary>
/// Authorization behavior - checks permissions for IRequireAuthorization routes.
/// Uses INuruBehavior&lt;IRequireAuthorization&gt; for type-safe filtered application.
/// </summary>
public sealed class AuthorizationBehavior : INuruBehavior<IRequireAuthorization>
{
  public async ValueTask HandleAsync(BehaviorContext<IRequireAuthorization> context, Func<ValueTask> proceed)
  {
    // context.Command is already IRequireAuthorization - no casting needed!
    string permission = context.Command.RequiredPermission;
    WriteLine($"[AUTH] Checking permission: {permission}");

    string? authorized = Environment.GetEnvironmentVariable("CLI_AUTHORIZED");
    if (string.IsNullOrEmpty(authorized) || authorized != "1")
    {
      WriteLine($"[AUTH] Access denied - permission required: {permission}");
      throw new UnauthorizedAccessException(
        $"Access denied. Permission required: {permission}. Set CLI_AUTHORIZED=1 to authorize.");
    }

    WriteLine($"[AUTH] Access granted for permission: {permission}");
    await proceed();
  }
}

/// <summary>
/// Retry behavior - implements exponential backoff for IRetryable routes.
/// Uses INuruBehavior&lt;IRetryable&gt; for type-safe filtered application.
/// </summary>
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
        return; // Success
      }
      catch (Exception ex) when (IsTransient(ex) && attempt <= maxRetries)
      {
        TimeSpan delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 2));
        WriteLine($"[RETRY] Attempt {attempt}/{maxRetries + 1} failed: {ex.Message}. Retrying in {delay.TotalSeconds}s...");
        await Task.Delay(delay, context.CancellationToken);
      }
    }

    throw new InvalidOperationException($"Retry logic error for {context.CommandName}");
  }

  private static bool IsTransient(Exception ex) =>
    ex is HttpRequestException or TimeoutException or IOException;
}
