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
- [x] Create DelegateRequest wrapper (non-generic, uses boxed results)
- [x] Create DelegatePipelineExecutor (custom pipeline, not IMediator.Send)
- [x] Modify execution path to route through pipeline when DI enabled and behaviors registered
- [x] Populate RouteExecutionContext before pipeline execution

### Verification
- [x] Create sample demonstrating unified middleware
- [x] Verify delegate routes receive pipeline behaviors
- [x] Verify explicit IRequest routes still work
- [x] Test mixed delegate/Mediator scenarios (44/44 tests pass)
- [ ] Benchmark performance impact

## Implementation Notes

### Key Design Decision: Custom Pipeline Executor

The original design proposed using `IMediator.Send()` with a `DelegateRequest<TResult>` wrapper.
However, martinothamar/Mediator uses source generation to discover handlers at compile time,
which doesn't support handlers defined in external libraries.

**Solution**: Created a `DelegatePipelineExecutor` that manually invokes registered
`IPipelineBehavior<DelegateRequest, DelegateResponse>` instances, achieving the same
result without requiring Mediator's handler discovery.

### Files Created

- `Source/TimeWarp.Nuru/Execution/RouteExecutionContext.cs` - Scoped service for sharing route metadata
- `Source/TimeWarp.Nuru/Execution/DelegateRequest.cs` - Request wrapper and response types
- `Source/TimeWarp.Nuru/Execution/DelegatePipelineExecutor.cs` - Custom pipeline executor
- `Samples/UnifiedMiddleware/unified-middleware.cs` - Demo sample

### Files Modified

- `Source/TimeWarp.Nuru/ServiceCollectionExtensions.cs` - Register new services
- `Source/TimeWarp.Nuru/NuruApp.cs` - Route through pipeline when behaviors registered

### Execution Flow

When DI is enabled AND pipeline behaviors are registered:
1. Create DI scope for request
2. Bind delegate parameters
3. Create DelegateRequest with invoker function
4. Populate RouteExecutionContext with route metadata
5. Execute through DelegatePipelineExecutor (chains behaviors)
6. Display response

When DI is NOT enabled OR no behaviors registered:
- Direct delegate invocation (no change from current behavior)

### Usage Example

```csharp
services.AddSingleton<IPipelineBehavior<DelegateRequest, DelegateResponse>, LoggingBehavior>();

builder.Map("add {x:int} {y:int}", (int x, int y) => x + y);
// Now receives LoggingBehavior!
```

## References

- https://www.jimmybogard.com/sharing-context-in-mediatr-pipelines/
- Depends on: 078_001_Migrate-To-Martinothamar-Mediator
