#!/usr/bin/dotnet --
// pipeline-middleware - Demonstrates Mediator pipeline behaviors for cross-cutting concerns
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

using System.Diagnostics;
using System.Net;
using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static System.Console;

// Pipeline Middleware Sample
// ==========================
// This sample demonstrates martinothamar/Mediator pipeline behaviors (middleware)
// for implementing cross-cutting concerns like logging, performance monitoring,
// authorization, retry/resilience, telemetry, validation, and more.
//
// Pipeline behaviors execute in registration order, wrapping the command handler
// like layers of an onion. Each behavior can execute code before and after the
// inner handler(s).
//
// Marker Interface Patterns:
// - IRequireAuthorization: Commands requiring permission checks (set CLI_AUTHORIZED=1)
// - IRetryable: Commands that should retry on transient failures with exponential backoff

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

      // Authorization behavior only applies to commands implementing IRequireAuthorization
      services.AddSingleton<IPipelineBehavior<AdminCommand, Unit>, LoggingBehavior<AdminCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<AdminCommand, Unit>, AuthorizationBehavior<AdminCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<AdminCommand, Unit>, PerformanceBehavior<AdminCommand, Unit>>();

      // Retry behavior for commands implementing IRetryable (resilience pattern)
      services.AddSingleton<IPipelineBehavior<FlakyCommand, Unit>, LoggingBehavior<FlakyCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<FlakyCommand, Unit>, RetryBehavior<FlakyCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<FlakyCommand, Unit>, PerformanceBehavior<FlakyCommand, Unit>>();
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
  // Admin command that requires authorization (set CLI_AUTHORIZED=1 to access)
  .Map<AdminCommand>
  (
    pattern: "admin {action}",
    description: "Admin operation requiring authorization (set CLI_AUTHORIZED=1)"
  )
  // Flaky command that simulates transient failures with retry
  .Map<FlakyCommand>
  (
    pattern: "flaky {failCount:int}",
    description: "Simulate transient failures (retries up to 3 times with exponential backoff)"
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

/// <summary>
/// Admin command that requires authorization.
/// Demonstrates marker interface pattern - only commands implementing
/// IRequireAuthorization will have permission checks applied.
/// </summary>
public sealed class AdminCommand : IRequest, IRequireAuthorization
{
  public string Action { get; set; } = string.Empty;

  /// <summary>The permission required to execute this command.</summary>
  public string RequiredPermission => "admin:execute";

  public sealed class Handler : IRequestHandler<AdminCommand>
  {
    public ValueTask<Unit> Handle(AdminCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Executing admin action: {request.Action}");
      WriteLine("Admin operation completed successfully.");
      return default;
    }
  }
}

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

// =============================================================================
// MARKER INTERFACES
// =============================================================================

/// <summary>
/// Marker interface for commands that require authorization.
/// Only commands implementing this interface will have permission checks applied
/// by the AuthorizationBehavior.
/// </summary>
public interface IRequireAuthorization
{
  /// <summary>The permission required to execute this command.</summary>
  string RequiredPermission { get; }
}

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
// AUTHORIZATION BEHAVIOR
// =============================================================================

/// <summary>
/// Authorization behavior that checks permissions using a marker interface pattern.
/// This behavior only applies permission checks to commands that implement
/// IRequireAuthorization, demonstrating selective behavior application.
/// </summary>
/// <remarks>
/// For demonstration purposes, authorization is controlled via the CLI_AUTHORIZED
/// environment variable. In a real application, this would integrate with your
/// authentication/authorization system.
/// </remarks>
public sealed class AuthorizationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  private readonly ILogger<AuthorizationBehavior<TMessage, TResponse>> Logger;

  public AuthorizationBehavior(ILogger<AuthorizationBehavior<TMessage, TResponse>> logger)
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
    // Only check authorization for commands that require it
    if (message is IRequireAuthorization authRequest)
    {
      string permission = authRequest.RequiredPermission;
      Logger.LogInformation("[AUTH] Checking permission: {Permission}", permission);

      // Simple demo: check environment variable for authorization
      // In production, this would integrate with your auth system
      string? authorized = Environment.GetEnvironmentVariable("CLI_AUTHORIZED");
      if (string.IsNullOrEmpty(authorized) || authorized != "1")
      {
        Logger.LogWarning("[AUTH] Access denied - permission required: {Permission}", permission);
        throw new UnauthorizedAccessException
        (
          $"Access denied. Permission required: {permission}. Set CLI_AUTHORIZED=1 to authorize."
        );
      }

      Logger.LogInformation("[AUTH] Access granted for permission: {Permission}", permission);
    }

    return await next(message, cancellationToken);
  }
}

// =============================================================================
// RETRY BEHAVIOR
// =============================================================================

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
