# Pipeline Middleware Sample

This sample demonstrates how to implement cross-cutting concerns using TimeWarp.Nuru's `INuruBehavior` pipeline behaviors in CLI applications.

## What is Pipeline Middleware?

Pipeline behaviors are interceptors that wrap command execution, allowing you to add functionality before, after, or around a command. They work like layers of an onion:

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

| Sample | Description |
|--------|-------------|
| [01-pipeline-middleware-basic.cs](01-pipeline-middleware-basic.cs) | Logging and performance monitoring |
| [02-pipeline-middleware-exception.cs](02-pipeline-middleware-exception.cs) | Consistent error handling |
| [03-pipeline-middleware-telemetry.cs](03-pipeline-middleware-telemetry.cs) | OpenTelemetry distributed tracing |
| [04-pipeline-middleware-filtered-auth.cs](04-pipeline-middleware-filtered-auth.cs) | Filtered authorization with `INuruBehavior<T>` |
| [05-pipeline-middleware-retry.cs](05-pipeline-middleware-retry.cs) | Resilience with exponential backoff |
| [06-pipeline-middleware-combined.cs](06-pipeline-middleware-combined.cs) | Combined example with all behaviors |

## Quick Start

Run the samples directly:

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

# Filtered authorization (INuruBehavior<IRequireAuthorization>)
./04-pipeline-middleware-filtered-auth.cs echo "Hello"       # No auth needed
./04-pipeline-middleware-filtered-auth.cs admin delete-all   # Access denied
CLI_AUTHORIZED=1 ./04-pipeline-middleware-filtered-auth.cs admin delete-all  # Access granted

# Retry with exponential backoff (INuruBehavior<IRetryable>)
./05-pipeline-middleware-retry.cs flaky 0    # Succeeds immediately
./05-pipeline-middleware-retry.cs flaky 2    # Fails twice, then succeeds
./05-pipeline-middleware-retry.cs echo Hello # No retry (doesn't implement IRetryable)

# Combined example with all behaviors
./06-pipeline-middleware-combined.cs echo "Hello"
./06-pipeline-middleware-combined.cs slow 600
./06-pipeline-middleware-combined.cs admin delete-all
CLI_AUTHORIZED=1 ./06-pipeline-middleware-combined.cs admin delete-all
./06-pipeline-middleware-combined.cs flaky 2
./06-pipeline-middleware-combined.cs error validation
./06-pipeline-middleware-combined.cs trace api-call
```

## INuruBehavior Pattern

TimeWarp.Nuru uses source generation for pipeline behaviors, eliminating runtime overhead. Behaviors implement `INuruBehavior`:

```csharp
public interface INuruBehavior
{
  ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed);
}
```

The `HandleAsync` method wraps the next behavior or handler. Call `proceed()` to continue the pipeline.

### BehaviorContext

Every behavior receives a `BehaviorContext` with:

```csharp
public class BehaviorContext
{
  public required string CommandName { get; init; }      // Route pattern: "echo {message}"
  public required string CommandTypeName { get; init; }  // Type name or generated name
  public required CancellationToken CancellationToken { get; init; }
  public string CorrelationId { get; }                   // GUID for request correlation
  public object? Command { get; init; }                  // Command instance for interface checks
}
```

## Pipeline Behaviors Demonstrated

### LoggingBehavior

Logs command entry and exit for observability:

```csharp
public sealed class LoggingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Handling {context.CommandName}");

    try
    {
      await proceed();
      WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Completed {context.CommandName}");
    }
    catch (Exception ex)
    {
      WriteLine($"[PIPELINE] [{context.CorrelationId[..8]}] Error: {ex.GetType().Name}");
      throw;
    }
  }
}
```

### PerformanceBehavior

Measures execution time and warns on slow commands:

```csharp
public sealed class PerformanceBehavior : INuruBehavior
{
  private const int SlowThresholdMs = 500;

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    var stopwatch = Stopwatch.StartNew();

    try
    {
      await proceed();
    }
    finally
    {
      stopwatch.Stop();
      long elapsed = stopwatch.ElapsedMilliseconds;

      if (elapsed > SlowThresholdMs)
        WriteLine($"[PERFORMANCE] {context.CommandName} took {elapsed}ms - SLOW!");
      else
        WriteLine($"[PERFORMANCE] {context.CommandName} completed in {elapsed}ms");
    }
  }
}
```

### ExceptionHandlingBehavior

Catches exceptions and provides user-friendly error messages:

```csharp
public sealed class ExceptionHandlingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    try
    {
      await proceed();
    }
    catch (Exception exception)
    {
      string message = exception switch
      {
        ValidationException ex => $"Validation error: {ex.Message}",
        UnauthorizedAccessException ex => $"Access denied: {ex.Message}",
        ArgumentException ex => $"Invalid argument: {ex.Message}",
        _ => "Error: An unexpected error occurred."
      };

      Error.WriteLine($"[EXCEPTION] {message}");
      throw;  // Re-throw or swallow - YOUR CHOICE
    }
  }
}
```

### TelemetryBehavior

Creates OpenTelemetry-compatible Activity spans using `using` statement:

```csharp
public sealed class TelemetryBehavior : INuruBehavior
{
  private static readonly ActivitySource Source = new("TimeWarp.Nuru.Commands", "1.0.0");

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    // 'using' ensures Activity is disposed even on exception
    using Activity? activity = Source.StartActivity(context.CommandName, ActivityKind.Internal);
    activity?.SetTag("command.name", context.CommandName);
    activity?.SetTag("correlation.id", context.CorrelationId);

    try
    {
      await proceed();
      activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      activity?.SetTag("error.type", ex.GetType().Name);
      throw;
    }
  }
}
```

## Filtered Behaviors with `INuruBehavior<T>`

Filtered behaviors only apply to routes that implement a specific interface via `.Implements<T>()`. This enables selective behavior application without runtime type checks.

### AuthorizationBehavior (Filtered)

Only runs for routes with `.Implements<IRequireAuthorization>()`:

```csharp
public interface IRequireAuthorization
{
  string RequiredPermission { get; set; }
}

public sealed class AuthorizationBehavior : INuruBehavior<IRequireAuthorization>
{
  public async ValueTask HandleAsync(BehaviorContext<IRequireAuthorization> context, Func<ValueTask> proceed)
  {
    // context.Command is already IRequireAuthorization - no casting needed!
    string permission = context.Command.RequiredPermission;
    
    if (!HasPermission(permission))
      throw new UnauthorizedAccessException($"Required: {permission}");
    
    await proceed();
  }
}
```

### RetryBehavior (Filtered)

Only runs for routes with `.Implements<IRetryable>()`:

```csharp
public interface IRetryable
{
  int MaxRetries { get; set; }
}

public sealed class RetryBehavior : INuruBehavior<IRetryable>
{
  public async ValueTask HandleAsync(BehaviorContext<IRetryable> context, Func<ValueTask> proceed)
  {
    // context.Command is already IRetryable - no casting needed!
    int maxRetries = context.Command.MaxRetries;

    for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
    {
      try
      {
        await proceed();
        return;
      }
      catch (Exception ex) when (IsTransient(ex) && attempt <= maxRetries)
      {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
        await Task.Delay(delay, context.CancellationToken);
      }
    }
  }

  private static bool IsTransient(Exception ex) =>
    ex is HttpRequestException or TimeoutException or IOException;
}
```

### Using `.Implements<T>()` on Routes

```csharp
NuruApp.CreateBuilder(args)
  .AddBehavior(typeof(AuthorizationBehavior))
  .AddBehavior(typeof(RetryBehavior))
  
  // No interfaces - neither behavior runs
  .Map("echo {message}")
    .WithHandler((string message) => WriteLine(message))
    .Done()
  
  // Has IRequireAuthorization - AuthorizationBehavior runs
  .Map("admin {action}")
    .Implements<IRequireAuthorization>(x => x.RequiredPermission = "admin:execute")
    .WithHandler((string action) => WriteLine($"Admin: {action}"))
    .Done()
  
  // Has IRetryable - RetryBehavior runs
  .Map("flaky {operation}")
    .Implements<IRetryable>(x => x.MaxRetries = 3)
    .WithHandler((string operation) => DoFlakyOperation(operation))
    .Done()
  
  .Build();
```

## Registration and Execution Order

**Critical**: Registration order determines execution order.

- **First registered** = outermost (called first, returns last)
- **Last registered** = innermost (called last, returns first)

```csharp
NuruApp.CreateBuilder(args)
  .AddBehavior(typeof(TelemetryBehavior))         // 1st - outermost
  .AddBehavior(typeof(LoggingBehavior))           // 2nd
  .AddBehavior(typeof(ExceptionHandlingBehavior)) // 3rd
  .AddBehavior(typeof(PerformanceBehavior))       // 4th - innermost
  .Map("echo {message}")
    .WithHandler((string message) => WriteLine(message))
    .Done()
  .Build();
```

**Execution flow:**
```
Telemetry.HandleAsync → Logging.HandleAsync → Exception.HandleAsync → Performance.HandleAsync → Handler
                                                                                              ↓
Telemetry completes ← Logging completes ← Exception completes ← Performance completes ← Handler returns
```

## Key Differences from Mediator

| Aspect | Mediator IPipelineBehavior | TimeWarp.Nuru INuruBehavior |
|--------|---------------------------|----------------------------|
| Pattern | `next(message, ct)` | `proceed()` |
| Control | Full (catch, retry, skip) | Full (catch, retry, skip) |
| Lifetime | Per-request (transient) | Singleton |
| State | Instance fields | Local variables |
| Code generation | Runtime reflection | Source-generated |
| Startup overhead | ~131ms | ~4ms |

## Key Benefits

1. **Full Control**: Retry, skip, catch, transform - anything is possible
2. **Source-Generated**: No runtime reflection or DI overhead
3. **Simple**: Single `HandleAsync` method, no State class needed
4. **Natural Patterns**: `using`, `try/catch`, `finally` work as expected
5. **Familiar**: Similar to Mediator/MediatR pattern
6. **Fast**: ~4ms startup vs ~131ms with Mediator

## See Also

- [Aspire Telemetry Sample](../_aspire-telemetry/aspire-telemetry.cs) - Full OpenTelemetry integration
- [INuruBehavior Interface](../../source/timewarp-nuru-core/abstractions/behavior-interfaces.cs) - Interface definition
- [BehaviorContext](../../source/timewarp-nuru-core/abstractions/behavior-context.cs) - Context class
