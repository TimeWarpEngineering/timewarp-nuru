#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-logging/timewarp-nuru-logging.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

// ═══════════════════════════════════════════════════════════════════════════════
// EXCEPTION HANDLING PIPELINE MIDDLEWARE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates consistent exception handling across all commands.
// The ExceptionHandlingBehavior catches exceptions and provides user-friendly
// error messages while logging full details.
//
// EXCEPTION CATEGORIES:
//   - ValidationException: User input validation errors (show message)
//   - UnauthorizedAccessException: Permission errors (show message)
//   - ArgumentException: Invalid arguments (show message)
//   - All others: Unexpected errors (hide details from user)
//
// RUN THIS SAMPLE:
//   ./pipeline-middleware-exception.cs error validation
//   ./pipeline-middleware-exception.cs error auth
//   ./pipeline-middleware-exception.cs error argument
//   ./pipeline-middleware-exception.cs error unknown
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)
  // Error command to demonstrate exception handling behavior
  .Map<ErrorCommand>("error {errorType}")
    .WithDescription("Throw different exception types (validation, auth, argument, unknown)")
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  // Register Mediator with exception handling behavior.
  // This should typically be the innermost behavior to catch all exceptions.
  services.AddMediator(options =>
  {
    options.PipelineBehaviors =
    [
      typeof(LoggingBehavior<,>),
      typeof(ExceptionHandlingBehavior<,>)  // Innermost: catches all exceptions
    ];
  });
}

// =============================================================================
// COMMANDS
// =============================================================================

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
