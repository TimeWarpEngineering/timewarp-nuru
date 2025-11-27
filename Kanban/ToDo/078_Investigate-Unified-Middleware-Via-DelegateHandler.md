# Investigate Unified Middleware via DelegateHandler

## Description

Investigate wrapping delegate-based routes in a generic `DelegateHandler` that runs through the Mediator pipeline when DI is configured. This would unify middleware behavior across both delegate and Mediator-based routes.

## Problem Statement

When mixing delegate and Mediator approaches (see `Samples/Calculator/calc-mixed.cs`), middleware like logging, metrics, or validation only applies to Mediator commands. Users reasonably expect cross-cutting concerns to apply uniformly to all routes.

## Proposed Solution

When `AddDependencyInjection()` is called:
1. Delegate routes get wrapped in a generated `IRequest` type
2. A generic `DelegateHandler` invokes the original delegate
3. All routes flow through the Mediator pipeline, receiving middleware uniformly

## Key Design Considerations

### Route Execution Context (Scoped DI)

Per Jimmy Bogard's recommendation (https://www.jimmybogard.com/sharing-context-in-mediatr-pipelines/), use a scoped service for sharing context across pipeline behaviors:

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

This allows any `IPipelineBehavior` to inject route metadata regardless of route type.

### Behavior When DI Not Configured

If `AddDependencyInjection()` is not called, delegates execute directly (no Mediator, no middleware). This preserves the lightweight scenario for simple tools.

### DelegateHandler Sketch

```csharp
public class DelegateRequest : IRequest
{
    public string RoutePattern { get; init; }
    public object?[] BoundArguments { get; init; }
    public Delegate Handler { get; init; }
}

public class DelegateRequestHandler : IRequestHandler<DelegateRequest>
{
    public async Task Handle(DelegateRequest request, CancellationToken ct)
    {
        var result = request.Handler.DynamicInvoke(request.BoundArguments);
        if (result is Task task)
            await task;
    }
}
```

## Research Areas

- [ ] Review TimeWarp.Mediator pipeline behavior registration
- [ ] Determine if `DynamicInvoke` performance is acceptable or if compiled expressions are needed
- [ ] Evaluate AOT compatibility implications
- [ ] Consider how return values (objects, JSON serialization) flow through the pipeline
- [ ] Investigate whether source generators could eliminate reflection

## Trade-offs

| Aspect | Before | After |
|--------|--------|-------|
| Performance (delegate) | ~4KB, minimal overhead | Mediator pipeline overhead |
| Middleware consistency | Split behavior | Unified |
| Mental model | Two distinct paths | One pipeline, two authoring styles |
| AOT compatibility | Full for delegates | Needs investigation |

## References

- `Samples/Calculator/calc-mixed.cs` - Mixed delegate/Mediator example
- `Samples/PipelineMiddleware/pipeline-middleware.cs` - Pipeline behavior example
- https://www.jimmybogard.com/sharing-context-in-mediatr-pipelines/
