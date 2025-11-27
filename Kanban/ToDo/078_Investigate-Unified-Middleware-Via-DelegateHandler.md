# Investigate Unified Middleware via DelegateHandler

## Description

Investigate wrapping delegate-based routes in a generic `DelegateHandler` that runs through the Mediator pipeline when DI is configured. This would unify middleware behavior across both delegate and Mediator-based routes.

Additionally, evaluate replacing TimeWarp.Mediator (MediatR fork) with martinothamar/Mediator (source-generator based) for improved AOT support and performance.

## Problem Statement

When mixing delegate and Mediator approaches (see `Samples/Calculator/calc-mixed.cs`), middleware like logging, metrics, or validation only applies to Mediator commands. Users reasonably expect cross-cutting concerns to apply uniformly to all routes.

## Proposed Solution

When `AddDependencyInjection()` is called:
1. Delegate routes get wrapped in a generated `IRequest` type
2. A generic `DelegateHandler` invokes the original delegate
3. All routes flow through the Mediator pipeline, receiving middleware uniformly

## Mediator Library Evaluation

### TimeWarp.Mediator vs martinothamar/Mediator

TimeWarp.Mediator is a fork of MediatR (reflection-based). martinothamar/Mediator uses source generators.

| Aspect | TimeWarp.Mediator | martinothamar/Mediator |
|--------|-------------------|------------------------|
| Implementation | Reflection-based | Source generator |
| AOT Support | Partial (`TrimMode=partial`) | Full Native AOT |
| Performance | Good | Excellent |
| API Return Type | `Task<TResponse>` | `ValueTask<TResponse>` |
| Pipeline Delegate | `RequestHandlerDelegate<TResponse>()` | `MessageHandlerDelegate<TMessage, TResponse>(message, ct)` |
| Service Lifetime | Typically Transient/Scoped | Singleton by default |
| Handler Registration | Runtime reflection | Compile-time generated |
| Diagnostics | Runtime errors | Build-time warnings/errors |

### Benefits of martinothamar/Mediator for Nuru

1. **Full AOT compatibility** - No more `TrimMode=partial` workaround
2. **Better cold start** - No reflection on startup
3. **Compile-time safety** - Missing handlers caught at build time
4. **Performance parity** - Eliminates "Mediator approach is slower" story
5. **ValueTask** - Better for CLI where many operations complete synchronously
6. **Source-generated DelegateHandler** - Could generate strongly-typed invocation code, eliminating `DynamicInvoke`

### API Differences

```csharp
// TimeWarp.Mediator (MediatR style)
Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
await next()

// martinothamar/Mediator
ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken ct)
await next(message, ct)  // Avoids closure allocations
```

### Fork Available

TimeWarpEngineering fork: https://github.com/TimeWarpEngineering/martinothamar-mediator

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

### DelegateHandler Sketch (with source generator)

With martinothamar/Mediator, the source generator could emit strongly-typed request types and handlers for each delegate route, eliminating reflection entirely:

```csharp
// Generated for: .Map("add {x:double} {y:double}", (double x, double y) => ...)
public sealed record AddRoute_Request(double X, double Y) : IRequest;

public sealed class AddRoute_Handler : IRequestHandler<AddRoute_Request>
{
    private readonly Action<double, double> _handler;

    public AddRoute_Handler(/* original delegate captured at registration */)
    {
        _handler = ...;
    }

    public ValueTask Handle(AddRoute_Request request, CancellationToken ct)
    {
        _handler(request.X, request.Y);
        return default;
    }
}
```

## Research Areas

- [ ] Evaluate martinothamar/Mediator API compatibility with existing Nuru code
- [ ] Assess migration effort from TimeWarp.Mediator
- [ ] Prototype source-generated DelegateHandler approach
- [ ] Benchmark performance difference between libraries
- [ ] Verify full AOT compatibility with martinothamar/Mediator
- [ ] Review pipeline behavior registration differences
- [ ] Consider how return values flow through the pipeline
- [ ] Test scoped `RouteExecutionContext` pattern with both libraries

## Trade-offs

| Aspect | Before | After (with martinothamar/Mediator) |
|--------|--------|-------------------------------------|
| Performance (delegate) | ~4KB, minimal overhead | Source-generated, near-zero overhead |
| Middleware consistency | Split behavior | Unified |
| Mental model | Two distinct paths | One pipeline, two authoring styles |
| AOT compatibility | Partial for Mediator | Full for both approaches |
| Build-time safety | Runtime handler errors | Compile-time diagnostics |

## References

- `Samples/Calculator/calc-mixed.cs` - Mixed delegate/Mediator example
- `Samples/PipelineMiddleware/pipeline-middleware.cs` - Pipeline behavior example
- https://www.jimmybogard.com/sharing-context-in-mediatr-pipelines/
- https://github.com/martinothamar/Mediator
- https://github.com/TimeWarpEngineering/martinothamar-mediator
