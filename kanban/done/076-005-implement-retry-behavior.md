# Implement Retry Behavior

## Description

Create a RetryBehavior for resilience, demonstrating exponential backoff retry logic for commands marked with IRetryable.

## Parent

076_Add-Pipeline-Middleware-Sample

## Dependencies

- 076_001_Create-Pipeline-Middleware-Sample-Structure (must be completed first)

## Checklist

- [x] Create IRetryable marker interface
- [x] Create RetryBehavior<TRequest, TResponse> class
- [x] Implement exponential backoff (2^attempt seconds)
- [x] Configure max retry attempts (e.g., 3)
- [x] Only retry specific exception types (e.g., HttpRequestException, TimeoutException)
- [x] Log retry attempts
- [x] Create sample command that simulates transient failures

## Results

Implementation added to `Samples/PipelineMiddleware/pipeline-middleware.cs`:

1. **IRetryable marker interface** - Commands implement to opt-in to retry behavior
   - `MaxRetries` property with default value of 3

2. **RetryBehavior<TMessage, TResponse>** - Pipeline behavior with:
   - Exponential backoff: 2^attempt seconds between retries
   - Transient exception detection: `HttpRequestException`, `TimeoutException`, `IOException`
   - Detailed logging of retry attempts with delay information

3. **FlakyCommand** - Demo command that:
   - Simulates transient failures based on `failCount` parameter
   - Implements `IRetryable` to opt-in to retry behavior
   - Throws `HttpRequestException` to trigger retries

Usage: `./pipeline-middleware.cs flaky 2` (fails twice then succeeds)

## Notes

```csharp
public interface IRetryable
{
    int MaxRetries => 3;
}

public sealed class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest
{
    private readonly ILogger<RetryBehavior<TRequest, TResponse>> Logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not IRetryable retryable)
            return await next();

        int maxRetries = retryable.MaxRetries;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try { return await next(); }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                Logger.LogWarning("Attempt {Attempt}/{Max} failed, retrying in {Delay}s...",
                    attempt, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }
        throw new InvalidOperationException("Unreachable");
    }

    private static bool IsTransient(Exception ex) =>
        ex is HttpRequestException or TimeoutException or IOException;
}
```

Demonstrates resilience patterns for CLI apps that interact with external services.
