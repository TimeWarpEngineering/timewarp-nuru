# Implement Exception Handling Behavior

## Description

Create an ExceptionHandlingBehavior that provides consistent error handling, logging, and user-friendly error messages across all commands.

## Parent

076_Add-Pipeline-Middleware-Sample

## Checklist

- [x] Create ExceptionHandlingBehavior<TRequest, TResponse> class
- [x] Catch and log all exceptions with command context
- [x] Differentiate between known and unknown exceptions
- [x] Provide user-friendly error output
- [x] Optionally wrap exceptions in CommandExecutionException
- [x] Create sample command that throws different exception types

## Results

Implementation added to `samples/pipeline-middleware/pipeline-middleware.cs`:

1. **ExceptionHandlingBehavior<TMessage, TResponse>** - Pipeline behavior with:
   - Differentiated handling for known exception types:
     - `ValidationException`: Warning level, shows message to user
     - `UnauthorizedAccessException`: Warning level, shows message to user
     - `ArgumentException`: Warning level, shows message to user
     - All others: Error level, hides details from user (security best practice)
   - User-friendly error output to stderr
   - All exceptions wrapped in `CommandExecutionException` with command context

2. **CommandExecutionException** - Custom exception that:
   - Wraps the original exception
   - Provides command name and failure category
   - Enables upstream handlers to identify which command failed

3. **ErrorCommand** - Demo command that throws:
   - `validation` → ValidationException
   - `auth` → UnauthorizedAccessException
   - `argument` → ArgumentException
   - `unknown` → InvalidOperationException

Usage examples:
- `./pipeline-middleware.cs error validation`
- `./pipeline-middleware.cs error auth`
- `./pipeline-middleware.cs error unknown`

## Notes

```csharp
public sealed class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest
{
    private readonly ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> Logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (ValidationException ex)
        {
            Logger.LogWarning(ex, "Validation failed for {Command}", typeof(TRequest).Name);
            Console.Error.WriteLine($"Validation error: {ex.Message}");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Authorization failed for {Command}", typeof(TRequest).Name);
            Console.Error.WriteLine($"Access denied: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unhandled exception in {Command}", typeof(TRequest).Name);
            Console.Error.WriteLine($"Error: An unexpected error occurred. See logs for details.");
            throw;
        }
    }
}
```

This should be the innermost behavior (last registered) to catch all exceptions from the pipeline.
