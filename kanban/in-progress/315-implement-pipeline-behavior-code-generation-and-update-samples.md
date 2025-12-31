# Implement Pipeline Behavior Code Generation and Update Samples

## Summary

Implement source generator code emission for pipeline behaviors (`AddBehavior<T>()`) and migrate `_pipeline-middleware/` samples from Mediator's runtime `IPipelineBehavior` to TimeWarp.Nuru's source-generated approach.

## Background

### Current State

The V2 source generator has infrastructure for behaviors but no code generation:

| Component | Status |
|-----------|--------|
| `BehaviorDefinition` model | Exists |
| `PipelineDefinition` model | Exists |
| `MiddlewareDefinition` model | Exists |
| `AddBehaviorLocator` | Exists |
| DSL interpreter `AddBehavior()` | Exists |
| `AppModel.Behaviors` | Exists |
| IR builder `AddBehavior()` | Exists |
| **Emitter for behaviors** | **NOT IMPLEMENTED** |

### Current Samples Problem

`_pipeline-middleware/` samples use Mediator's runtime `IPipelineBehavior<TMessage, TResponse>`:
- Requires `#:package Mediator.Abstractions` and `#:package Mediator.SourceGenerator`
- Uses `services.AddMediator(options => { options.PipelineBehaviors = [...] })`
- Runtime overhead: 131ms startup vs 4ms for source-gen path (per benchmarks)

### Target Architecture

Source generator should emit inline behavior code. No runtime `IPipelineBehavior` interface needed.

## Design Decisions (Finalized)

### 1. Interface Pattern: `INuruBehavior` with `OnBeforeAsync`/`OnAfterAsync`/`OnErrorAsync`

**Chosen approach:** Interface-based with async-first methods and default no-op implementations.

```csharp
public interface INuruBehavior
{
  ValueTask OnBeforeAsync(BehaviorContext context) => ValueTask.CompletedTask;
  ValueTask OnAfterAsync(BehaviorContext context) => ValueTask.CompletedTask;
  ValueTask OnErrorAsync(BehaviorContext context, Exception exception) => ValueTask.CompletedTask;
}
```

**Why this approach:**
- Familiar pattern (similar to Mediator/MediatR)
- Async-first for flexibility
- Default implementations mean behaviors only override what they need
- No `next` callback - generator handles chaining

### 2. Service Injection: Constructor Injection (Singleton behaviors)

Behaviors are instantiated once (Singleton) with services injected via constructor:

```csharp
public class LoggingBehavior(ILogger<LoggingBehavior> logger) : INuruBehavior
{
  public ValueTask OnBeforeAsync(BehaviorContext context)
  {
    logger.LogInformation("[{CorrelationId}] Handling {Command}", 
      context.CorrelationId, context.CommandName);
    return ValueTask.CompletedTask;
  }
}
```

### 3. Per-Request State: Nested `State` class inheriting from `BehaviorContext`

Behaviors needing per-request state define a nested `State` class:

```csharp
public class PerformanceBehavior(ILogger<PerformanceBehavior> logger) : INuruBehavior
{
  // Nested State class for per-request state
  public class State : BehaviorContext
  {
    public Stopwatch Stopwatch { get; } = new();
  }
  
  public ValueTask OnBeforeAsync(State state)
  {
    state.Stopwatch.Start();
    return ValueTask.CompletedTask;
  }
  
  public ValueTask OnAfterAsync(State state)
  {
    state.Stopwatch.Stop();
    if (state.Stopwatch.ElapsedMilliseconds > 500)
      logger.LogWarning("[{CorrelationId}] {Command} took {Ms}ms", 
        state.CorrelationId, state.CommandName, state.Stopwatch.ElapsedMilliseconds);
    return ValueTask.CompletedTask;
  }
}
```

**Key insight:** Developer experience looks like MediatR transient (instance state), but reality is Singleton with per-request context.

### 4. Base Context: `BehaviorContext` with built-in properties

```csharp
public class BehaviorContext
{
  public required string CommandName { get; init; }
  public required string CommandTypeName { get; init; }
  public required CancellationToken CancellationToken { get; init; }
  public string CorrelationId { get; } = Guid.NewGuid().ToString();  // Built-in for troubleshooting
  public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();        // Built-in for timing
}
```

### 5. Execution Order

Behaviors execute in registration order:
- First registered = outermost (OnBefore first, OnAfter last)
- Last registered = innermost (OnBefore last, OnAfter first)

### 6. Sharing State Between Behaviors

**Deferred to backlog.** If needed later, can add `Items` dictionary to `BehaviorContext`.

## Example: What Developer Writes vs What Generator Emits

**Developer writes:**
```csharp
NuruApp.CreateBuilder(args)
  .AddBehavior<LoggingBehavior>()
  .AddBehavior<PerformanceBehavior>()
  .Map("ping").WithHandler(() => "pong").Done()
  .Build();
```

**Generator emits:**
```csharp
if (args is ["ping"])
{
  // Create contexts
  var __context = new BehaviorContext
  {
    CommandName = "ping",
    CommandTypeName = "PingCommand",
    CancellationToken = cancellationToken
  };
  var __state_Performance = new PerformanceBehavior.State
  {
    CommandName = "ping",
    CommandTypeName = "PingCommand",
    CancellationToken = cancellationToken
  };
  
  // OnBefore (outermost first)
  await __behavior_Logging.OnBeforeAsync(__context);
  await __behavior_Performance.OnBeforeAsync(__state_Performance);
  
  try
  {
    // Handler
    string result = "pong";
    app.Terminal.WriteLine(result);
    
    // OnAfter (innermost first - reverse order)
    await __behavior_Performance.OnAfterAsync(__state_Performance);
    await __behavior_Logging.OnAfterAsync(__context);
    
    return 0;
  }
  catch (Exception __ex)
  {
    // OnError (innermost first - reverse order)
    await __behavior_Performance.OnErrorAsync(__state_Performance, __ex);
    await __behavior_Logging.OnErrorAsync(__context, __ex);
    throw;
  }
}
```

## Checklist

### Phase 1: Design and Interface ✅
- [x] Decide on behavior specification approach → `INuruBehavior` interface
- [x] Design `BehaviorContext` base class
- [x] Design nested `State` pattern for per-request state
- [x] Document behavior authoring pattern

### Phase 1.1: Create Interface File ✅
- [x] Create `source/timewarp-nuru-core/abstractions/behavior-interfaces.cs`
- [x] Commit: `feat(core): add INuruBehavior interface and BehaviorContext`

### Phase 2: Generator Implementation
- [ ] Create `behavior-extractor.cs` - find behaviors, extract State classes
- [ ] Create `behavior-emitter.cs` - emit context creation and method calls
- [ ] Integrate with `interceptor-emitter.cs` - wrap handler with behavior calls
- [ ] Handle behavior service dependencies (constructor injection)
- [ ] Handle behaviors with/without custom State class

### Phase 3: Update Samples (one by one)

#### 3.1: pipeline-middleware-basic.cs
- [ ] Remove `#:package Mediator.*` directives
- [ ] Remove `using Mediator;`
- [ ] Remove `services.AddMediator()` call
- [ ] Convert `LoggingBehavior` to TimeWarp.Nuru pattern
- [ ] Convert `PerformanceBehavior` to TimeWarp.Nuru pattern
- [ ] Use `.AddBehavior<T>()` DSL
- [ ] Test: `./pipeline-middleware-basic.cs echo "Hello"`
- [ ] Test: `./pipeline-middleware-basic.cs slow 600`

#### 3.2: pipeline-middleware-authorization.cs
- [ ] Convert `AuthorizationBehavior` 
- [ ] Convert `IRequireAuthorization` marker interface pattern
- [ ] Test authorization flow

#### 3.3: pipeline-middleware-retry.cs
- [ ] Convert `RetryBehavior`
- [ ] Convert `IRetryable` marker interface pattern
- [ ] Test retry with exponential backoff

#### 3.4: pipeline-middleware-exception.cs
- [ ] Convert `ExceptionHandlingBehavior`
- [ ] Convert `CommandExecutionException`
- [ ] Test exception categorization

#### 3.5: pipeline-middleware-telemetry.cs
- [ ] Convert `TelemetryBehavior`
- [ ] Preserve OpenTelemetry Activity integration
- [ ] Test distributed tracing

#### 3.6: pipeline-middleware.cs (combined)
- [ ] Update combined example with all behaviors
- [ ] Verify full pipeline works end-to-end

### Phase 4: Testing and Documentation
- [ ] Add generator tests for behavior emission
- [ ] Update `_pipeline-middleware/overview.md`
- [ ] Rename folder to numbered sample (e.g., `07-pipeline-middleware/`)

## Files to Create/Modify

| File | Action |
|------|--------|
| `source/timewarp-nuru-core/abstractions/behavior-interfaces.cs` | Create - `INuruBehavior`, `BehaviorContext` |
| `source/timewarp-nuru-analyzers/generators/extractors/behavior-extractor.cs` | Create - extract behaviors and State classes |
| `source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs` | Create - code generation |
| `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` | Modify - integrate behavior wrapping |
| `samples/_pipeline-middleware/*.cs` | Modify - migrate from Mediator |
| `samples/_pipeline-middleware/overview.md` | Modify - update documentation |

## Dependencies

- Depends on V2 generator being functional (#265 phases 0-5 complete)
- Should be done after #289 (Zero-Cost Runtime) or in parallel

## Related Tasks

- #265 Epic: V2 Source Generator Implementation
- #289 V2 Generator Phase 7: Zero-Cost Runtime
- #312 Update samples to use NuruApp.CreateBuilder

## Backlog Items (Deferred)

- [ ] Sharing state between behaviors (Items dictionary on BehaviorContext)
- [ ] Marker interface pattern for selective behavior application
- [ ] Configurable CorrelationId format

## Notes

### Why source-gen behaviors?

Per benchmarks in #152:
- Mediator path: 131ms startup
- Direct path: 4ms startup

Source-generating behavior code eliminates:
- Runtime reflection for behavior discovery
- DI container overhead for behavior instantiation
- Generic type instantiation at runtime

### Reference

- Current Mediator-based samples: `samples/_pipeline-middleware/`
- Behavior models: `source/timewarp-nuru-analyzers/generators/models/behavior-definition.cs`
- Pipeline models: `source/timewarp-nuru-analyzers/generators/models/pipeline-definition.cs`
