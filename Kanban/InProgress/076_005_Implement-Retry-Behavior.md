# Implement Retry Behavior

## Description

Create a RetryBehavior for resilience, demonstrating exponential backoff retry logic for commands marked with IRetryable.

## Parent

076_Add-Pipeline-Middleware-Sample

## Dependencies

- 076_001_Create-Pipeline-Middleware-Sample-Structure (must be completed first)

## Checklist

- [ ] Create IRetryable marker interface
- [ ] Create RetryBehavior<TRequest, TResponse> class
- [ ] Implement exponential backoff (2^attempt seconds)
- [ ] Configure max retry attempts (e.g., 3)
- [ ] Only retry specific exception types (e.g., HttpRequestException, TimeoutException)
- [ ] Log retry attempts
- [ ] Create sample command that simulates transient failures

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
