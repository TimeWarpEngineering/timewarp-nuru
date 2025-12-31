# Pipeline Middleware Sample

This sample demonstrates how to implement cross-cutting concerns using TimeWarp.Nuru's `INuruBehavior` pipeline behaviors in CLI applications.

## What is Pipeline Middleware?

Pipeline behaviors are interceptors that wrap command execution, allowing you to add functionality before and after a command runs. They work like layers of an onion:

```
Request Flow:
┌───────────────────────────────────────────────────────────────────────┐
│ TelemetryBehavior (outermost - registered first)                      │
│   ┌───────────────────────────────────────────────────────────────┐   │
│   │ LoggingBehavior                                               │   │
│   │   ┌───────────────────────────────────────────────────────┐   │   │
│   │   │ ExceptionHandlingBehavior                             │   │   │
│   │   │   ┌───────────────────────────────────────────────┐   │   │   │
│   │   │   │ PerformanceBehavior                           │   │   │   │
│   │   │   │   ┌───────────────────────────────────────┐   │   │   │   │
│   │   │   │   │ Command Handler (innermost)           │   │   │   │   │
│   │   │   │   └───────────────────────────────────────┘   │   │   │   │
│   │   │   └───────────────────────────────────────────────┘   │   │   │
│   │   └───────────────────────────────────────────────────────┘   │   │
│   └───────────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────────┘
```

## Samples

| Sample | Description | Status |
|--------|-------------|--------|
| [01-pipeline-middleware-basic.cs](01-pipeline-middleware-basic.cs) | Logging and performance monitoring | **Converted to INuruBehavior** |
| [02-pipeline-middleware-exception.cs](02-pipeline-middleware-exception.cs) | Consistent error handling | **Converted to INuruBehavior** |
| [03-pipeline-middleware-telemetry.cs](03-pipeline-middleware-telemetry.cs) | OpenTelemetry distributed tracing | **Converted to INuruBehavior** |
| [pipeline-middleware-authorization.cs](pipeline-middleware-authorization.cs) | Permission checks with marker interfaces | Uses Mediator (deferred) |
| [pipeline-middleware-retry.cs](pipeline-middleware-retry.cs) | Resilience with exponential backoff | Uses Mediator (deferred) |
| [pipeline-middleware.cs](pipeline-middleware.cs) | Combined example with all behaviors | Uses Mediator (deferred) |

## Quick Start

Run the converted samples directly:

```bash
# Basic logging and performance
./01-pipeline-middleware-basic.cs echo "Hello, World!"
./01-pipeline-middleware-basic.cs slow 600

# Exception handling with categorization
./02-pipeline-middleware-exception.cs error validation
./02-pipeline-middleware-exception.cs error auth
./02-pipeline-middleware-exception.cs error argument
./02-pipeline-middleware-exception.cs error unknown

# Distributed tracing with Activity spans
./03-pipeline-middleware-telemetry.cs trace database-query
./03-pipeline-middleware-telemetry.cs trace api-call
```

## INuruBehavior Pattern

TimeWarp.Nuru uses source generation for pipeline behaviors, eliminating runtime overhead. Behaviors implement `INuruBehavior`:

```csharp
public interface INuruBehavior
{
  ValueTask OnBeforeAsync(BehaviorContext context) => ValueTask.CompletedTask;
  ValueTask OnAfterAsync(BehaviorContext context) => ValueTask.CompletedTask;
  ValueTask OnErrorAsync(BehaviorContext context, Exception exception) => ValueTask.CompletedTask;
}
```

### BehaviorContext

Every behavior receives a `BehaviorContext` with:

```csharp
public class BehaviorContext
{
  public required string CommandName { get; init; }      // Route pattern: "echo {message}"
  public required string CommandTypeName { get; init; }  // Type name or generated name
  public required CancellationToken CancellationToken { get; init; }
  public string CorrelationId { get; }                   // GUID for request correlation
  public Stopwatch Stopwatch { get; }                    // Auto-started for timing
}
```

### Custom State Pattern

Behaviors needing per-request state define a nested `State` class:

```csharp
public sealed class TelemetryBehavior : INuruBehavior
{
  // Custom State holds Activity across method calls
  public sealed class State : BehaviorContext
  {
    public Activity? Activity { get; set; }
  }

  public ValueTask OnBeforeAsync(BehaviorContext context)
  {
    if (context is State state)
    {
      state.Activity = ActivitySource.StartActivity(state.CommandName);
    }
    return ValueTask.CompletedTask;
  }

  public ValueTask OnAfterAsync(BehaviorContext context)
  {
    if (context is State state)
    {
      state.Activity?.SetStatus(ActivityStatusCode.Ok);
      state.Activity?.Dispose();
    }
    return ValueTask.CompletedTask;
  }
}
```

## Pipeline Behaviors Demonstrated

### LoggingBehavior

Logs command entry and exit for observability:

```csharp
public sealed class LoggingBehavior : INuruBehavior
{
  public ValueTask OnBeforeAsync(BehaviorContext context)
  {
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Handling {context.CommandName}");
    return ValueTask.CompletedTask;
  }

  public ValueTask OnAfterAsync(BehaviorContext context)
  {
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Completed {context.CommandName}");
    return ValueTask.CompletedTask;
  }

  public ValueTask OnErrorAsync(BehaviorContext context, Exception exception)
  {
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Error: {exception.GetType().Name}");
    return ValueTask.CompletedTask;
  }
}
```

### PerformanceBehavior

Measures execution time and warns on slow commands (uses inherited Stopwatch):

```csharp
public sealed class PerformanceBehavior : INuruBehavior
{
  private const int SlowThresholdMs = 500;

  public ValueTask OnBeforeAsync(BehaviorContext context) => ValueTask.CompletedTask;

  public ValueTask OnAfterAsync(BehaviorContext context)
  {
    context.Stopwatch.Stop();
    long elapsed = context.Stopwatch.ElapsedMilliseconds;

    if (elapsed > SlowThresholdMs)
      WriteLine($"[PERFORMANCE] {context.CommandName} took {elapsed}ms - SLOW!");
    else
      WriteLine($"[PERFORMANCE] {context.CommandName} completed in {elapsed}ms");

    return ValueTask.CompletedTask;
  }
}
```

### ExceptionHandlingBehavior

Provides user-friendly error messages based on exception type:

```csharp
public sealed class ExceptionHandlingBehavior : INuruBehavior
{
  public ValueTask OnBeforeAsync(BehaviorContext context) => ValueTask.CompletedTask;
  public ValueTask OnAfterAsync(BehaviorContext context) => ValueTask.CompletedTask;

  public ValueTask OnErrorAsync(BehaviorContext context, Exception exception)
  {
    string message = exception switch
    {
      ValidationException ex => $"Validation error: {ex.Message}",
      UnauthorizedAccessException ex => $"Access denied: {ex.Message}",
      ArgumentException ex => $"Invalid argument: {ex.Message}",
      _ => "Error: An unexpected error occurred."
    };

    Error.WriteLine($"[EXCEPTION] {message}");
    return ValueTask.CompletedTask;
    // Note: Exception still propagates after OnErrorAsync
  }
}
```

### TelemetryBehavior

Creates OpenTelemetry-compatible Activity spans (uses custom State):

```csharp
public sealed class TelemetryBehavior : INuruBehavior
{
  private static readonly ActivitySource CommandActivitySource = new("TimeWarp.Nuru.Commands", "1.0.0");

  public sealed class State : BehaviorContext
  {
    public Activity? Activity { get; set; }
  }

  public ValueTask OnBeforeAsync(BehaviorContext context)
  {
    if (context is State state)
    {
      state.Activity = CommandActivitySource.StartActivity(state.CommandName, ActivityKind.Internal);
      state.Activity?.SetTag("command.name", state.CommandName);
      state.Activity?.SetTag("correlation.id", state.CorrelationId);
    }
    return ValueTask.CompletedTask;
  }

  public ValueTask OnAfterAsync(BehaviorContext context)
  {
    if (context is State state)
    {
      state.Activity?.SetStatus(ActivityStatusCode.Ok);
      state.Activity?.Dispose();
    }
    return ValueTask.CompletedTask;
  }

  public ValueTask OnErrorAsync(BehaviorContext context, Exception exception)
  {
    if (context is State state)
    {
      state.Activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
      state.Activity?.SetTag("error.type", exception.GetType().Name);
      state.Activity?.Dispose();
    }
    return ValueTask.CompletedTask;
  }
}
```

## Registration and Execution Order

**Critical**: Registration order determines execution order.

- **First registered** = outermost (OnBefore first, OnAfter/OnError last)
- **Last registered** = innermost (OnBefore last, OnAfter/OnError first)

```csharp
NuruApp.CreateBuilder(args)
  .AddBehavior(typeof(TelemetryBehavior))       // 1st - outermost
  .AddBehavior(typeof(LoggingBehavior))         // 2nd
  .AddBehavior(typeof(ExceptionHandlingBehavior)) // 3rd
  .AddBehavior(typeof(PerformanceBehavior))     // 4th - innermost
  .Map("echo {message}")
    .WithHandler((string message) => WriteLine(message))
    .Done()
  .Build();
```

**Execution flow:**
```
OnBefore: Telemetry → Logging → Exception → Performance → Handler
OnAfter:  Performance → Exception → Logging → Telemetry
OnError:  Performance → Exception → Logging → Telemetry
```

## Key Differences from Mediator

| Aspect | Mediator IPipelineBehavior | TimeWarp.Nuru INuruBehavior |
|--------|---------------------------|----------------------------|
| Pattern | `next()` callback | Separate OnBefore/OnAfter/OnError |
| Lifetime | Per-request (transient) | Singleton (per-request State) |
| Exception handling | Can catch and transform | Observe only (propagates) |
| Code generation | Runtime reflection | Source-generated |
| Startup overhead | ~131ms | ~4ms |

## Deferred Samples

The following samples require **behavior filtering** (applying behaviors selectively based on route metadata or marker interfaces). This feature is planned for a future release:

- `pipeline-middleware-authorization.cs` - Needs `IRequireAuthorization` marker interface check
- `pipeline-middleware-retry.cs` - Needs `IRetryable` marker interface check
- `pipeline-middleware.cs` - Combined example with all behaviors

## Key Benefits

1. **Source-Generated**: No runtime reflection or DI overhead
2. **Separation of Concerns**: Each behavior handles one responsibility
3. **Composability**: Mix and match behaviors
4. **Testability**: Test behaviors in isolation
5. **Consistency**: Same patterns across all commands
6. **Performance**: Fast startup (~4ms vs ~131ms with Mediator)

## See Also

- [Aspire Telemetry Sample](../_aspire-telemetry/aspire-telemetry.cs) - Full OpenTelemetry integration
- [INuruBehavior Interface](../../source/timewarp-nuru-core/abstractions/behavior-interfaces.cs) - Interface definition
- [BehaviorContext](../../source/timewarp-nuru-core/abstractions/behavior-context.cs) - Context class
