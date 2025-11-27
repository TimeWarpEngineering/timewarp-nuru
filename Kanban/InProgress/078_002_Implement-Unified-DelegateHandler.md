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
- [x] Design DelegateRequest wrapper type
- [x] Design RouteExecutionContext for sharing route metadata
- [x] Determine handler registration strategy

### Implementation
- [x] Create RouteExecutionContext scoped service
- [x] Create DelegateRequest : IRequest<DelegateResponse>
- [x] Create DelegateRequestHandler : IRequestHandler<DelegateRequest, DelegateResponse>
- [x] Modify execution path to use mediator.Send() when DI enabled and behaviors registered
- [x] Populate RouteExecutionContext before Send()

### Verification
- [x] Create sample demonstrating unified middleware
- [x] Verify delegate routes receive pipeline behaviors
- [x] Verify explicit IRequest routes still work
- [x] Test mixed delegate/Mediator scenarios (44/44 tests pass)
- [ ] Benchmark performance impact

## Implementation Notes

### How It Works

1. `DelegateRequest` implements `IRequest<DelegateResponse>`
2. `DelegateRequestHandler` implements `IRequestHandler<DelegateRequest, DelegateResponse>`
3. martinothamar/Mediator's source generator scans referenced assemblies by default
4. When consuming apps call `services.AddMediator()`, the handler is discovered
5. Pipeline behaviors registered for `DelegateRequest` execute automatically via `mediator.Send()`

### Files Created

- `Source/TimeWarp.Nuru/Execution/RouteExecutionContext.cs` - Scoped service for sharing route metadata
- `Source/TimeWarp.Nuru/Execution/DelegateRequest.cs` - Request, response, and handler types
- `Samples/UnifiedMiddleware/unified-middleware.cs` - Demo sample

### Files Modified

- `Source/TimeWarp.Nuru/ServiceCollectionExtensions.cs` - Register RouteExecutionContext
- `Source/TimeWarp.Nuru/NuruApp.cs` - Route through mediator.Send() when behaviors registered

### Execution Flow

When DI is enabled AND pipeline behaviors are registered:
1. Create DI scope for request
2. Populate RouteExecutionContext with route metadata
3. Bind delegate parameters
4. Create DelegateRequest with invoker function
5. Call `mediator.Send(request)` - pipeline behaviors execute automatically
6. Display response

When DI is NOT enabled OR no behaviors registered:
- Direct delegate invocation (no change from current behavior)

### Usage Example

```csharp
services.AddMediator(); // Discovers DelegateRequestHandler automatically
services.AddSingleton<IPipelineBehavior<DelegateRequest, DelegateResponse>, LoggingBehavior>();

builder.Map("add {x:int} {y:int}", (int x, int y) => x + y);
// Now receives LoggingBehavior via mediator.Send()!
```

## References

- https://www.jimmybogard.com/sharing-context-in-mediatr-pipelines/
- Depends on: 078_001_Migrate-To-Martinothamar-Mediator
