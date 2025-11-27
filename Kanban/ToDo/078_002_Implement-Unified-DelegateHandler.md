# Implement Unified DelegateHandler

## Description

Implement a DelegateHandler that wraps delegate-based routes and executes them through the Mediator pipeline when DI is configured. This ensures middleware applies uniformly to all routes.

## Parent

078_Unified-Middleware-Pipeline

## Requirements

- Delegate routes get pipeline behaviors when DI is enabled
- No change to behavior when DI is not configured (direct execution)
- RouteExecutionContext available to all pipeline behaviors
- Performance overhead is minimal

## Checklist

### Design
- [ ] Design DelegateRequest wrapper type
- [ ] Design RouteExecutionContext for sharing route metadata
- [ ] Determine handler registration strategy

### Implementation
- [ ] Create RouteExecutionContext scoped service
- [ ] Create DelegateRequest<TResult> wrapper
- [ ] Create DelegateRequestHandler
- [ ] Modify execution path to route through Mediator when DI enabled
- [ ] Populate RouteExecutionContext before Send()

### Verification
- [ ] Create sample demonstrating unified middleware
- [ ] Verify delegate routes receive pipeline behaviors
- [ ] Verify explicit IRequest routes still work
- [ ] Test mixed delegate/Mediator scenarios
- [ ] Benchmark performance impact

## Design

### RouteExecutionContext (Scoped DI)

Per Jimmy Bogard's recommendation, use a scoped service for sharing context:

```csharp
public class RouteExecutionContext
{
    public string RoutePattern { get; set; } = "";
    public string RouteName { get; set; } = "";
    public IReadOnlyDictionary<string, object?> Parameters { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
}
```

### DelegateRequest Wrapper

```csharp
public sealed record DelegateRequest<TResult>(
    string RoutePattern,
    object?[] BoundArguments,
    Func<object?[], TResult> Invoker
) : IRequest<TResult>;

public sealed class DelegateRequestHandler<TResult> : IRequestHandler<DelegateRequest<TResult>, TResult>
{
    public ValueTask<TResult> Handle(DelegateRequest<TResult> request, CancellationToken ct)
    {
        var result = request.Invoker(request.BoundArguments);
        return new ValueTask<TResult>(result);
    }
}
```

### Execution Flow

When DI is enabled:
1. Create RouteExecutionContext, populate with route metadata
2. Wrap delegate in DelegateRequest
3. Call `mediator.Send(delegateRequest)`
4. Pipeline behaviors execute (logging, metrics, validation, etc.)
5. DelegateRequestHandler invokes original delegate
6. Result flows back through pipeline

When DI is not enabled:
- Direct delegate invocation (no change from current behavior)

## References

- https://www.jimmybogard.com/sharing-context-in-mediatr-pipelines/
- Depends on: 078_001_Migrate-To-Martinothamar-Mediator
