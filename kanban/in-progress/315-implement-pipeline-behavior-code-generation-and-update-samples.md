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
| **Emitter for behaviors** | **NEEDS UPDATE** |

### Current Samples Problem

`_pipeline-middleware/` samples use Mediator's runtime `IPipelineBehavior<TMessage, TResponse>`:
- Requires `#:package Mediator.Abstractions` and `#:package Mediator.SourceGenerator`
- Uses `services.AddMediator(options => { options.PipelineBehaviors = [...] })`
- Runtime overhead: 131ms startup vs 4ms for source-gen path (per benchmarks)

### Target Architecture

Source generator should emit inline behavior code. No runtime `IPipelineBehavior` interface needed.

## Design Decisions

### ~~OLD: `OnBeforeAsync`/`OnAfterAsync`/`OnErrorAsync` Pattern~~ (REJECTED)

The original design used separate hooks:
```csharp
// REJECTED - lacks control flow (can't retry, can't skip handler)
public interface INuruBehavior
{
  ValueTask OnBeforeAsync(BehaviorContext context);
  ValueTask OnAfterAsync(BehaviorContext context);
  ValueTask OnErrorAsync(BehaviorContext context, Exception exception);
}
```

**Problems:**
- `OnErrorAsync` can only observe - cannot retry or handle exceptions
- Cannot skip handler execution (e.g., authorization failure)
- Cannot wrap execution with `using` statements naturally
- Required awkward `State` class pattern for per-request state

### NEW: `HandleAsync(context, next)` Pattern (CHOSEN)

Switch to the familiar Mediator/MediatR pattern with full control:

```csharp
public interface INuruBehavior
{
  ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> next);
}
```

**Benefits:**
- Full control over execution flow
- Can retry, skip, wrap, transform
- Familiar to Mediator/MediatR users
- Single method to implement
- Simpler generator code
- Enables retry behavior (was impossible before)

### Examples with New Pattern

**Logging (simple before/after):**
```csharp
public sealed class LoggingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> next)
  {
    Console.WriteLine($"[{context.CorrelationId[..8]}] Before: {context.CommandName}");
    await next();
    Console.WriteLine($"[{context.CorrelationId[..8]}] After: {context.CommandName}");
  }
}
```

**Performance timing:**
```csharp
public sealed class PerformanceBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> next)
  {
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();
    
    if (sw.ElapsedMilliseconds > 500)
      Console.WriteLine($"[SLOW] {context.CommandName} took {sw.ElapsedMilliseconds}ms");
  }
}
```

**Exception handling:**
```csharp
public sealed class ExceptionHandlingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> next)
  {
    try
    {
      await next();
    }
    catch (ValidationException ex)
    {
      Console.Error.WriteLine($"Validation error: {ex.Message}");
      throw;  // or swallow, or wrap - YOUR CHOICE
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine("An unexpected error occurred.");
      throw;
    }
  }
}
```

**Telemetry with `using`:**
```csharp
public sealed class TelemetryBehavior : INuruBehavior
{
  private static readonly ActivitySource Source = new("TimeWarp.Nuru.Commands");

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> next)
  {
    using var activity = Source.StartActivity(context.CommandName, ActivityKind.Internal);
    activity?.SetTag("correlation.id", context.CorrelationId);
    
    try
    {
      await next();
      activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      throw;
    }
  }
}
```

**Retry (NOW POSSIBLE!):**
```csharp
public sealed class RetryBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> next)
  {
    if (context.Command is not IRetryable retryable)
    {
      await next();
      return;
    }

    for (int attempt = 1; attempt <= retryable.MaxRetries + 1; attempt++)
    {
      try
      {
        await next();
        return;
      }
      catch (Exception ex) when (IsTransient(ex) && attempt <= retryable.MaxRetries)
      {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
        Console.WriteLine($"[RETRY] Attempt {attempt} failed, retrying in {delay.TotalSeconds}s...");
        await Task.Delay(delay, context.CancellationToken);
      }
    }
  }

  private static bool IsTransient(Exception ex) =>
    ex is HttpRequestException or TimeoutException or IOException;
}
```

**Authorization (can skip handler):**
```csharp
public sealed class AuthorizationBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> next)
  {
    if (context.Command is IRequireAuthorization auth)
    {
      if (!HasPermission(auth.RequiredPermission))
        throw new UnauthorizedAccessException($"Required: {auth.RequiredPermission}");
    }
    
    await next();
  }
}
```

### BehaviorContext

Simplified - no more `State` class pattern needed:

```csharp
public class BehaviorContext
{
  public required string CommandName { get; init; }
  public required string CommandTypeName { get; init; }
  public required CancellationToken CancellationToken { get; init; }
  public string CorrelationId { get; } = Guid.NewGuid().ToString();
  
  /// <summary>
  /// The command instance. Can be cast to check interface implementations.
  /// Available for both attributed routes and delegate routes with .Implements&lt;T&gt;().
  /// </summary>
  public object? Command { get; init; }
}
```

Note: `Stopwatch` removed from context - behaviors that need timing can create their own (simpler, more explicit).

### Generator Output

**Developer writes:**
```csharp
NuruApp.CreateBuilder(args)
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(PerformanceBehavior))
  .Map("ping").WithHandler(() => "pong").Done()
  .Build();
```

**Generator emits:**
```csharp
if (args is ["ping"])
{
  var __context = new BehaviorContext
  {
    CommandName = "ping",
    CommandTypeName = "Route_0",
    CancellationToken = CancellationToken.None,
    Command = null  // or generated command instance
  };

  await __behavior_Logging.HandleAsync(__context, async () =>
  {
    await __behavior_Performance.HandleAsync(__context, async () =>
    {
      // Handler
      string result = "pong";
      app.Terminal.WriteLine(result);
    });
  });
  
  return 0;
}
```

### Execution Order

Behaviors execute in registration order (first = outermost):
```csharp
.AddBehavior(typeof(TelemetryBehavior))    // 1st - outermost
.AddBehavior(typeof(LoggingBehavior))      // 2nd
.AddBehavior(typeof(RetryBehavior))        // 3rd
.AddBehavior(typeof(PerformanceBehavior))  // 4th - innermost
```

Call flow: `Telemetry → Logging → Retry → Performance → Handler`

## Checklist

### Phase 1: Update Interface ✅
- [x] Update `INuruBehavior` to use `HandleAsync(context, proceed)` pattern
- [x] Remove `OnBeforeAsync`/`OnAfterAsync`/`OnErrorAsync` methods
- [x] Simplify `BehaviorContext` (remove Stopwatch, add Command property)
- [x] Remove nested `State` class detection from generator

### Phase 2: Update Generator ✅
- [x] Update `behavior-emitter.cs` for new pattern
- [x] Emit nested lambda chain instead of separate OnBefore/OnAfter/OnError calls
- [x] Simplify generated State class handling (no longer needed)
- [x] Pass command instance to BehaviorContext

### Phase 3: Re-convert Samples ✅ (Core samples done, deferred samples blocked on #316)

All samples need re-conversion with new pattern:

#### 3.1: 01-pipeline-middleware-basic.cs ✅
- [x] Update `LoggingBehavior` to `HandleAsync` pattern
- [x] Update `PerformanceBehavior` to `HandleAsync` pattern (use local Stopwatch)
- [x] Test: `./01-pipeline-middleware-basic.cs echo "Hello"`
- [x] Test: `./01-pipeline-middleware-basic.cs slow 600`

#### 3.2: 02-pipeline-middleware-exception.cs ✅
- [x] Update `ExceptionHandlingBehavior` to `HandleAsync` pattern
- [x] Now CAN catch and handle exceptions properly
- [x] Test exception categorization

#### 3.3: 03-pipeline-middleware-telemetry.cs ✅
- [x] Update `TelemetryBehavior` to `HandleAsync` pattern
- [x] Use `using` for Activity lifecycle (much cleaner!)
- [x] Remove awkward `State` class
- [x] Test distributed tracing

#### 3.4: pipeline-middleware-authorization.cs (Blocked on #316)
- [ ] Convert with `HandleAsync` pattern
- [ ] Requires #316 for `context.Command` and `.Implements<T>()`
- [ ] Test authorization flow

#### 3.5: pipeline-middleware-retry.cs (Blocked on #316)
- [ ] Convert with `HandleAsync` pattern (NOW POSSIBLE!)
- [ ] Requires #316 for `context.Command` and `.Implements<T>()`
- [ ] Test retry with exponential backoff

#### 3.6: pipeline-middleware.cs (combined) (Blocked on #316)
- [ ] Update after all individual samples work

### Phase 4: Update Documentation ✅
- [x] Update `overview.md` for new pattern
- [x] Update examples in documentation

## Files to Modify

| File | Action |
|------|--------|
| `source/timewarp-nuru-core/abstractions/behavior-interfaces.cs` | Update interface |
| `source/timewarp-nuru-core/abstractions/behavior-context.cs` | Simplify, add Command |
| `source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs` | Update emission |
| `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` | Remove State detection |
| `samples/_pipeline-middleware/*.cs` | Re-convert all |
| `samples/_pipeline-middleware/overview.md` | Update docs |

## Dependencies

- #316 for `.Implements<T>()` and `context.Command` (authorization/retry samples)

## Related Tasks

- #265 Epic: V2 Source Generator Implementation
- #289 V2 Generator Phase 7: Zero-Cost Runtime
- #316 Behavior Filtering via `.Implements<T>()`

## Notes

### Why the design change?

The `OnBefore/OnAfter/OnError` pattern seemed simpler but was actually more limiting:
1. Could not retry (OnErrorAsync only observes)
2. Could not skip handler (authorization)
3. Required awkward State class for per-request state
4. More methods to implement

The `HandleAsync(context, next)` pattern:
1. Enables ALL patterns (retry, skip, wrap, transform)
2. Single method to implement
3. Familiar to Mediator/MediatR users
4. Simpler generated code
5. Local variables for per-request state (no State class needed)

### Why source-gen behaviors?

Per benchmarks in #152:
- Mediator path: 131ms startup
- Direct path: 4ms startup

Source-generating behavior code eliminates:
- Runtime reflection for behavior discovery
- DI container overhead for behavior instantiation
- Generic type instantiation at runtime
