# Migrate to martinothamar/Mediator

## Description

Replace TimeWarp.Mediator (MediatR fork) with martinothamar/Mediator (source-generator based) for improved AOT support and performance.

## Parent

078_Unified-Middleware-Pipeline

## Requirements

- All existing Mediator-based samples must work with the new library
- Full Native AOT compatibility (no `TrimMode=partial` workaround)
- Pipeline behaviors continue to work
- Build and tests pass

## Checklist

### Research
- [ ] Review martinothamar/Mediator API differences
- [ ] Identify breaking changes from TimeWarp.Mediator
- [ ] Document migration steps

### Implementation
- [ ] Update package references in Directory.Packages.props
- [ ] Update NuruAppBuilder.Configuration.cs for new registration API
- [ ] Update MediatorExecutor for new API (ValueTask, MessageHandlerDelegate)
- [ ] Update pipeline behavior samples for new signature
- [ ] Update calc-mediator.cs sample
- [ ] Update calc-mixed.cs sample
- [ ] Update pipeline-middleware.cs sample

### Verification
- [ ] All samples compile and run
- [ ] Test AOT compilation without TrimMode=partial
- [ ] Run benchmark comparison
- [ ] Verify pipeline behaviors execute correctly

## API Differences

```csharp
// TimeWarp.Mediator (MediatR style)
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}
// Usage: await next()

// martinothamar/Mediator
public interface IPipelineBehavior<TMessage, TResponse>
{
    ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken ct);
}
// Usage: await next(message, ct)  // Avoids closure allocations
```

## Key Changes

1. **Return type**: `Task<T>` -> `ValueTask<T>`
2. **Delegate signature**: `RequestHandlerDelegate<TResponse>()` -> `MessageHandlerDelegate<TMessage, TResponse>(message, ct)`
3. **Registration**: Reflection-based -> Source generator (`AddMediator()` with options)
4. **Handler interface**: Same name but in different namespace

## Benefits

| Aspect | TimeWarp.Mediator | martinothamar/Mediator |
|--------|-------------------|------------------------|
| Implementation | Reflection-based | Source generator |
| AOT Support | Partial (`TrimMode=partial`) | Full Native AOT |
| Performance | Good | Excellent |
| API Return Type | `Task<TResponse>` | `ValueTask<TResponse>` |
| Service Lifetime | Typically Transient/Scoped | Singleton by default |
| Handler Registration | Runtime reflection | Compile-time generated |
| Diagnostics | Runtime errors | Build-time warnings/errors |

## Notes

TimeWarpEngineering fork available: https://github.com/TimeWarpEngineering/martinothamar-mediator
