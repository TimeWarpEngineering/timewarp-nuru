# Implement Performance Behavior

## Description

Create a PerformanceBehavior that measures command execution time using Stopwatch and logs warnings for slow-running commands.

## Parent

073_Add-Pipeline-Middleware-Sample

## Checklist

- [ ] Create PerformanceBehavior<TRequest, TResponse> class
- [ ] Use Stopwatch for accurate timing
- [ ] Configure warning threshold (e.g., 500ms)
- [ ] Log elapsed time for all commands
- [ ] Log warning for commands exceeding threshold
- [ ] Add sample command that demonstrates slow execution

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
