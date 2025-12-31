#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-logging/timewarp-nuru-logging.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// EXCEPTION HANDLING PIPELINE MIDDLEWARE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates consistent exception handling across all commands
// using TimeWarp.Nuru's INuruBehavior pattern.
//
// The ExceptionHandlingBehavior's OnErrorAsync categorizes exceptions and
// provides user-friendly error messages while the LoggingBehavior logs details.
//
// EXCEPTION CATEGORIES:
//   - ValidationException: User input validation errors (show message)
//   - UnauthorizedAccessException: Permission errors (show message)
//   - ArgumentException: Invalid arguments (show message)
//   - All others: Unexpected errors (hide details from user)
//
// BEHAVIOR EXECUTION ORDER:
//   OnBefore: LoggingBehavior → ExceptionHandlingBehavior → Handler
//   OnError:  ExceptionHandlingBehavior → LoggingBehavior (reverse order)
//
// RUN THIS SAMPLE:
//   ./pipeline-middleware-exception.cs error validation
//   ./pipeline-middleware-exception.cs error auth
//   ./pipeline-middleware-exception.cs error argument
//   ./pipeline-middleware-exception.cs error unknown
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
using TimeWarp.Nuru;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  // Register behaviors - execute in order (first = outermost)
  // OnError executes in reverse order: ExceptionHandling first, then Logging
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(ExceptionHandlingBehavior))
  // Error command to demonstrate exception handling behavior
  .Map("error {errorType}")
    .WithDescription("Throw different exception types (validation, auth, argument, unknown)")
    .WithHandler((string errorType) =>
    {
      WriteLine($"Attempting operation that will throw: {errorType}");

      throw errorType.ToLowerInvariant() switch
      {
        "validation" => new ValidationException("Email address is not in a valid format"),
        "auth" => new UnauthorizedAccessException("You do not have permission to perform this action"),
        "argument" => new ArgumentException("The provided value is out of range", "errorType"),
        "unknown" or _ => new InvalidOperationException("An unexpected internal error occurred")
      };
    })
    .Done()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// PIPELINE BEHAVIORS
// =============================================================================

/// <summary>
/// Logging behavior that logs request entry and errors.
/// Registered first (outermost) so OnError runs last - after user-friendly message is displayed.
/// </summary>
public sealed class LoggingBehavior : INuruBehavior
{
  public ValueTask OnBeforeAsync(BehaviorContext context)
  {
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Handling {context.CommandName}");
    return ValueTask.CompletedTask;
  }

  public ValueTask OnAfterAsync(BehaviorContext context)
  {
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Completed {context.CommandName}");
    return ValueTask.CompletedTask;
  }

  public ValueTask OnErrorAsync(BehaviorContext context, Exception exception)
  {
    // Log the error details (runs after ExceptionHandlingBehavior displays user message)
    Error.WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Error in {context.CommandName}: {exception.GetType().Name}");
    return ValueTask.CompletedTask;
  }
}

/// <summary>
/// Exception handling behavior that provides user-friendly error messages.
/// Registered last (innermost) so OnError runs first - displays message before logging.
/// </summary>
/// <remarks>
/// Exception handling categories:
/// - ValidationException: User input validation errors (show message)
/// - UnauthorizedAccessException: Permission/auth errors (show message)
/// - ArgumentException: Invalid arguments (show message)
/// - All others: Unexpected errors (hide details from user)
///
/// Note: Unlike Mediator's IPipelineBehavior which can catch and transform exceptions,
/// INuruBehavior.OnErrorAsync is for observation only - the exception will still propagate
/// after all OnErrorAsync handlers complete.
/// </remarks>
public sealed class ExceptionHandlingBehavior : INuruBehavior
{
  // OnBeforeAsync - nothing to do before handler
  public ValueTask OnBeforeAsync(BehaviorContext context) => ValueTask.CompletedTask;

  // OnAfterAsync - nothing to do on success
  public ValueTask OnAfterAsync(BehaviorContext context) => ValueTask.CompletedTask;

  public ValueTask OnErrorAsync(BehaviorContext context, Exception exception)
  {
    // Categorize and display user-friendly messages
    // This runs first (innermost behavior) before LoggingBehavior logs details
    string message = exception switch
    {
      ValidationException ex => $"Validation error: {ex.Message}",
      UnauthorizedAccessException ex => $"Access denied: {ex.Message}",
      ArgumentException ex => $"Invalid argument: {ex.Message}",
      _ => "Error: An unexpected error occurred. See logs for details."
    };

    Error.WriteLine($"[EXCEPTION] {message}");
    return ValueTask.CompletedTask;
  }
}
