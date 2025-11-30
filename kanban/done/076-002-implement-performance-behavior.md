# Implement Performance Behavior

## Description

Create a PerformanceBehavior that measures command execution time using Stopwatch and logs warnings for slow-running commands.

## Parent

076_Add-Pipeline-Middleware-Sample

## Checklist

- [x] Create PerformanceBehavior<TRequest, TResponse> class
- [x] Use Stopwatch for accurate timing
- [x] Configure warning threshold (e.g., 500ms)
- [x] Log elapsed time for all commands
- [x] Log warning for commands exceeding threshold
- [x] Add sample command that demonstrates slow execution

## Results

Implementation exists in two samples demonstrating different use cases:

1. **pipeline-middleware.cs** - Basic Mediator pipeline behavior
2. **unified-middleware.cs** - Both Mediator and Delegate route performance monitoring

Updated both implementations to log elapsed time for ALL commands:
- Fast commands: `LogInformation` with elapsed time
- Slow commands (>500ms): `LogWarning` with elapsed time and threshold

Files modified:
- `samples/pipeline-middleware/pipeline-middleware.cs`
- `samples/unified-middleware/unified-middleware.cs`

## Notes

```csharp
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> Logger;
    private const int WarningThresholdMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try { return await next(); }
        finally
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds > WarningThresholdMs)
                Logger.LogWarning("Long running: {Command} ({ElapsedMs}ms)", typeof(TRequest).Name, sw.ElapsedMilliseconds);
            else
                Logger.LogInformation("{Command} completed in {ElapsedMs}ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);
        }
    }
}
```
