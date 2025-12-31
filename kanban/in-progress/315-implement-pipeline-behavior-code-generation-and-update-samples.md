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

**User writes:**
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
  // LoggingBehavior - before
  logger.LogInformation("[PIPELINE] Handling ping");
  var stopwatch = Stopwatch.StartNew();
  
  try 
  {
    // Handler
    string result = "pong";
    app.Terminal.WriteLine(result);
    
    // PerformanceBehavior - after
    stopwatch.Stop();
    if (stopwatch.ElapsedMilliseconds > 500)
      logger.LogWarning("[PERFORMANCE] ping took {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
    
    // LoggingBehavior - after
    logger.LogInformation("[PIPELINE] Completed ping");
    return 0;
  }
  catch (Exception ex)
  {
    logger.LogError(ex, "[PIPELINE] Error handling ping");
    throw;
  }
}
```

## Design Decisions Needed

### 1. How to specify behavior code?

**Option A: Convention-based methods**
```csharp
public class LoggingBehavior
{
  public static void Before(string commandName, ILogger logger) 
    => logger.LogInformation("[PIPELINE] Handling {Command}", commandName);
  
  public static void After(string commandName, ILogger logger)
    => logger.LogInformation("[PIPELINE] Completed {Command}", commandName);
  
  public static void OnError(string commandName, Exception ex, ILogger logger)
    => logger.LogError(ex, "[PIPELINE] Error handling {Command}", commandName);
}
```

**Option B: Interface with source-gen extraction**
```csharp
public interface INuruBehavior
{
  void Before(BehaviorContext context);
  void After(BehaviorContext context);
  void OnError(BehaviorContext context, Exception ex);
}
```

**Option C: Attribute-based code blocks**
```csharp
[NuruBehavior]
public static class LoggingBehavior
{
  [Before]
  public static string BeforeCode => """
    logger.LogInformation("[PIPELINE] Handling {command}");
    """;
}
```

### 2. How to handle behavior dependencies?

Behaviors may need services (ILogger, IConfiguration, etc.). Options:
- Extract from behavior class constructor
- Use same service resolution as handlers (static instantiation per #292)

### 3. Execution order

Behaviors execute in registration order:
- First registered = outermost (before first, after last)
- Last registered = innermost (before last, after first)

## Checklist

### Phase 1: Design and Interface
- [ ] Decide on behavior specification approach (Option A/B/C above)
- [ ] Create `INuruBehavior` or equivalent in `timewarp-nuru-core/abstractions/`
- [ ] Create `BehaviorContext` if needed
- [ ] Document behavior authoring pattern

### Phase 2: Generator Implementation
- [ ] Create `behavior-emitter.cs` in `generators/emitters/`
- [ ] Extract behavior method bodies from syntax tree
- [ ] Generate before/after/error wrapping code
- [ ] Integrate with `handler-invoker-emitter.cs`
- [ ] Handle behavior dependencies (service injection)

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
| `source/timewarp-nuru-core/abstractions/behavior-interfaces.cs` | Create - behavior authoring interface |
| `source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs` | Create - code generation |
| `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` | Modify - integrate behaviors |
| `samples/_pipeline-middleware/*.cs` | Modify - migrate from Mediator |
| `samples/_pipeline-middleware/overview.md` | Modify - update documentation |

## Dependencies

- Depends on V2 generator being functional (#265 phases 0-5 complete)
- Should be done after #289 (Zero-Cost Runtime) or in parallel

## Related Tasks

- #265 Epic: V2 Source Generator Implementation
- #289 V2 Generator Phase 7: Zero-Cost Runtime
- #312 Update samples to use NuruApp.CreateBuilder

## Notes

### Why source-gen behaviors?

Per benchmarks in #152:
- Mediator path: 131ms startup
- Direct path: 4ms startup

Source-generating behavior code eliminates:
- Runtime reflection for behavior discovery
- DI container overhead for behavior instantiation
- Generic type instantiation at runtime

### Marker Interface Pattern

Behaviors like `AuthorizationBehavior` and `RetryBehavior` use marker interfaces (`IRequireAuthorization`, `IRetryable`) to selectively apply. The generator needs to:
1. Detect if command implements marker interface
2. Only emit behavior code if marker is present

This may require semantic analysis of the command type at generation time.

### Reference

- Current Mediator-based samples: `samples/_pipeline-middleware/`
- Behavior models: `source/timewarp-nuru-analyzers/generators/models/behavior-definition.cs`
- Pipeline models: `source/timewarp-nuru-analyzers/generators/models/pipeline-definition.cs`
