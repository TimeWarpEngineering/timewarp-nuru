#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-logging/timewarp-nuru-logging.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// EXCEPTION HANDLING PIPELINE MIDDLEWARE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates consistent exception handling across all commands
// using TimeWarp.Nuru's INuruBehavior with HandleAsync(context, proceed) pattern.
//
// The ExceptionHandlingBehavior catches exceptions, categorizes them, and provides
// user-friendly error messages while the LoggingBehavior logs details.
//
// EXCEPTION CATEGORIES:
//   - ValidationException: User input validation errors (show message)
//   - UnauthorizedAccessException: Permission errors (show message)
//   - ArgumentException: Invalid arguments (show message)
//   - All others: Unexpected errors (hide details from user)
//
// BEHAVIOR EXECUTION ORDER:
//   LoggingBehavior wraps ExceptionHandlingBehavior wraps Handler
//   Exceptions bubble up: Handler → ExceptionHandling (catches) → Logging (catches)
//
// RUN THIS SAMPLE:
//   ./02-pipeline-middleware-exception.cs error validation
//   ./02-pipeline-middleware-exception.cs error auth
//   ./02-pipeline-middleware-exception.cs error argument
//   ./02-pipeline-middleware-exception.cs error unknown
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
using TimeWarp.Nuru;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  // Register behaviors - execute in order (first = outermost)
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
/// Registered first (outermost) - catches exceptions after ExceptionHandlingBehavior.
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
      // Log the error details (runs after ExceptionHandlingBehavior displays user message)
      Error.WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Error in {context.CommandName}: {ex.GetType().Name}");
      throw;
    }
  }
}

/// <summary>
/// Exception handling behavior that provides user-friendly error messages.
/// Registered last (innermost) - catches exceptions first and displays friendly messages.
/// </summary>
/// <remarks>
/// Exception handling categories:
/// - ValidationException: User input validation errors (show message)
/// - UnauthorizedAccessException: Permission/auth errors (show message)
/// - ArgumentException: Invalid arguments (show message)
/// - All others: Unexpected errors (hide details from user)
///
/// With HandleAsync pattern, we have full control - can catch, log, transform, or swallow.
/// In this example, we display a user-friendly message then re-throw.
/// </remarks>
public sealed class ExceptionHandlingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    try
    {
      await proceed();
    }
    catch (Exception exception)
    {
      // Categorize and display user-friendly messages
      string message = exception switch
      {
        ValidationException ex => $"Validation error: {ex.Message}",
        UnauthorizedAccessException ex => $"Access denied: {ex.Message}",
        ArgumentException ex => $"Invalid argument: {ex.Message}",
        _ => "Error: An unexpected error occurred. See logs for details."
      };

      Error.WriteLine($"[EXCEPTION] {message}");
      throw;  // Re-throw to let outer behaviors and framework handle it
    }
  }
}
