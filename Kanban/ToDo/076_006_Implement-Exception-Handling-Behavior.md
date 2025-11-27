# Implement Exception Handling Behavior

## Description

Create an ExceptionHandlingBehavior that provides consistent error handling, logging, and user-friendly error messages across all commands.

## Parent

076_Add-Pipeline-Middleware-Sample

## Checklist

- [ ] Create ExceptionHandlingBehavior<TRequest, TResponse> class
- [ ] Catch and log all exceptions with command context
- [ ] Differentiate between known and unknown exceptions
- [ ] Provide user-friendly error output
- [ ] Optionally wrap exceptions in CommandExecutionException
- [ ] Create sample command that throws different exception types

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
