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
- [x] Review martinothamar/Mediator API differences
- [x] Identify breaking changes from TimeWarp.Mediator
- [x] Document migration steps

### Implementation
- [x] Update package references in Directory.Packages.props
- [x] Update NuruAppBuilder.Configuration.cs for new registration API
- [x] Update MediatorExecutor for new API (ValueTask, MessageHandlerDelegate)
- [x] Update pipeline behavior samples for new signature
- [x] Update calc-mediator.cs sample
- [x] Update calc-mixed.cs sample
- [x] Update pipeline-middleware.cs sample
- [x] Add [DynamicallyAccessedMembers] to Map<TCommand> for AOT property preservation

### Verification
- [x] All samples compile and run
- [x] Test AOT compilation without TrimMode=partial
- [x] Run benchmark comparison
- [x] Verify pipeline behaviors execute correctly
- [x] All 44 test-both-versions.sh tests pass (Delegate JIT, Mediator JIT, Delegate AOT, Mediator AOT)

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
5. **AddMediator() location**: Must be called by the consuming application, not the library

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

## Implementation Notes

### Breaking Change: AddMediator() Registration

The library (TimeWarp.Nuru) no longer calls `AddMediator()` automatically. Applications using Mediator-based commands must call it explicitly:

```csharp
var app = new NuruAppBuilder()
    .AddDependencyInjection()
    .ConfigureServices(services => services.AddMediator()) // Required!
    .Map<MyCommand>("my-command")
    .Build();
```

This is because martinothamar/Mediator's source generator discovers handlers only in the assembly where `AddMediator()` is called. Since handlers are defined in the application, not the library, the application must make this call.

### Pipeline Behaviors in Runfiles

For AOT/runfile scenarios, use explicit generic registrations rather than open generic registration to avoid trimmer issues:

```csharp
// Instead of: services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
// Use explicit registrations:
services.AddSingleton<IPipelineBehavior<MyCommand, Unit>, LoggingBehavior<MyCommand, Unit>>();
```

### AOT Property Preservation

Added `[DynamicallyAccessedMembers]` attribute to `Map<TCommand>` generic type parameters to ensure command properties are preserved during AOT trimming. This was required to fix Mediator AOT tests where property binding was failing.

## Benchmark Results

| Framework | Time | Memory |
|-----------|------|--------|
| ConsoleAppFramework v5 | 1.18 ms | 0 B |
| System.CommandLine | 21.72 ms | 10 KB |
| CliFx | 25.12 ms | 66 KB |
| **TimeWarp.Nuru.Direct** | **28.07 ms** | **8 KB** |
| TimeWarp.Nuru (Mediator) | 55.79 ms | 63 KB |
| Cocona | 143.44 ms | 651 KB |

## Test Results

All 44 tests pass for all 4 configurations:

| Implementation | Tests | Execution Time |
|----------------|-------|----------------|
| Delegate (JIT) | 44/44 | 3.31s |
| Mediator (JIT) | 44/44 | 8.09s |
| Delegate (AOT) | 44/44 | 0.43s |
| Mediator (AOT) | 44/44 | 0.62s |

## Notes

TimeWarpEngineering fork available: https://github.com/TimeWarpEngineering/martinothamar-mediator
