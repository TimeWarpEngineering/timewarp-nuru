#!/usr/bin/dotnet --
// pipeline-middleware - Demonstrates Mediator pipeline behaviors for cross-cutting concerns
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

using System.ComponentModel.DataAnnotations;
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
//
// Telemetry:
// The TelemetryBehavior uses System.Diagnostics.Activity for OpenTelemetry-compatible
// distributed tracing. Activities are created for each command execution.
//
// Exception Handling:
// The ExceptionHandlingBehavior provides consistent error handling with user-friendly
// messages. It should be registered LAST (innermost) to catch all exceptions.

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

      // Exception handling behavior - demonstrates consistent error handling
      // ExceptionHandlingBehavior is registered LAST (innermost) to catch all exceptions
      services.AddSingleton<IPipelineBehavior<ErrorCommand, Unit>, LoggingBehavior<ErrorCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<ErrorCommand, Unit>, PerformanceBehavior<ErrorCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<ErrorCommand, Unit>, ExceptionHandlingBehavior<ErrorCommand, Unit>>();

      // Telemetry behavior - demonstrates OpenTelemetry-compatible distributed tracing
      // TelemetryBehavior should be registered early (outermost) to capture full execution
      services.AddSingleton<IPipelineBehavior<TraceCommand, Unit>, TelemetryBehavior<TraceCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<TraceCommand, Unit>, LoggingBehavior<TraceCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<TraceCommand, Unit>, PerformanceBehavior<TraceCommand, Unit>>();
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
  // Error command to demonstrate exception handling behavior
  .Map<ErrorCommand>
  (
    pattern: "error {errorType}",
    description: "Throw different exception types (validation, auth, argument, unknown)"
  )
  // Trace command to demonstrate telemetry/distributed tracing
  .Map<TraceCommand>
  (
    pattern: "trace {operation}",
    description: "Demonstrate OpenTelemetry-compatible distributed tracing with Activity"
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

/// <summary>
/// Error command that throws different exception types to demonstrate
/// the ExceptionHandlingBehavior's differentiated error handling.
/// </summary>
public sealed class ErrorCommand : IRequest
{
  /// <summary>Type of error to throw: validation, auth, argument, or unknown.</summary>
  public string ErrorType { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<ErrorCommand>
  {
    public ValueTask<Unit> Handle(ErrorCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Attempting operation that will throw: {request.ErrorType}");

      throw request.ErrorType.ToLowerInvariant() switch
      {
        "validation" => new ValidationException("Email address is not in a valid format"),
        "auth" => new UnauthorizedAccessException("You do not have permission to perform this action"),
        "argument" => new ArgumentException("The provided value is out of range", "errorType"),
        "unknown" or _ => new InvalidOperationException("An unexpected internal error occurred")
      };
    }
  }
}

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

// =============================================================================
// EXCEPTION HANDLING BEHAVIOR
// =============================================================================

/// <summary>
/// Exception handling behavior that provides consistent error handling, logging,
/// and user-friendly error messages across all commands.
/// </summary>
/// <remarks>
/// This behavior should be registered LAST (innermost) in the pipeline to catch
/// all exceptions from the command handler and other behaviors.
///
/// Exception handling categories:
/// - ValidationException: User input validation errors (warning level)
/// - UnauthorizedAccessException: Permission/auth errors (warning level)
/// - ArgumentException: Invalid arguments (warning level)
/// - All others: Unexpected errors (error level, details hidden from user)
/// </remarks>
public sealed class ExceptionHandlingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  private readonly ILogger<ExceptionHandlingBehavior<TMessage, TResponse>> Logger;

  public ExceptionHandlingBehavior(ILogger<ExceptionHandlingBehavior<TMessage, TResponse>> logger)
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

    try
    {
      return await next(message, cancellationToken);
    }
    catch (ValidationException ex)
    {
      // Validation errors - user input issues, show the message
      Logger.LogWarning(ex, "[EXCEPTION] Validation failed for {RequestName}", requestName);
      Error.WriteLine($"Validation error: {ex.Message}");
      throw new CommandExecutionException(requestName, "Validation failed", ex);
    }
    catch (UnauthorizedAccessException ex)
    {
      // Auth errors - permission issues, show the message
      Logger.LogWarning(ex, "[EXCEPTION] Authorization failed for {RequestName}", requestName);
      Error.WriteLine($"Access denied: {ex.Message}");
      throw new CommandExecutionException(requestName, "Authorization failed", ex);
    }
    catch (ArgumentException ex)
    {
      // Argument errors - invalid parameters, show the message
      Logger.LogWarning(ex, "[EXCEPTION] Invalid argument for {RequestName}", requestName);
      Error.WriteLine($"Invalid argument: {ex.Message}");
      throw new CommandExecutionException(requestName, "Invalid argument", ex);
    }
    catch (Exception ex)
    {
      // Unknown errors - hide details from user, log full exception
      Logger.LogError(ex, "[EXCEPTION] Unhandled exception in {RequestName}", requestName);
      Error.WriteLine("Error: An unexpected error occurred. See logs for details.");
      throw new CommandExecutionException(requestName, "Unexpected error", ex);
    }
  }
}

// =============================================================================
// CUSTOM EXCEPTIONS
// =============================================================================

/// <summary>
/// Wrapper exception that provides command context for exceptions thrown during execution.
/// This allows upstream handlers to identify which command failed and why.
/// </summary>
public sealed class CommandExecutionException : Exception
{
  /// <summary>Name of the command that failed.</summary>
  public string CommandName { get; }

  /// <summary>Category of the failure (e.g., "Validation failed", "Authorization failed").</summary>
  public string FailureCategory { get; }

  public CommandExecutionException(string commandName, string failureCategory, Exception innerException)
    : base($"{failureCategory} in {commandName}: {innerException.Message}", innerException)
  {
    CommandName = commandName;
    FailureCategory = failureCategory;
  }
}
