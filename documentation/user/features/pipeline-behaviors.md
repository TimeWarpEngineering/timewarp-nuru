# Pipeline Behaviors

Pipeline behaviors are middleware that wrap command/query execution. They execute in registration order (first registered = outermost layer).

## Overview

Behaviors intercept every command before and after execution, enabling cross-cutting concerns like logging, telemetry, authorization, and error handling. They work like layers of an onion:

```
Request → LoggingBehavior → PerformanceBehavior → Handler → PerformanceBehavior → LoggingBehavior → Response
```

First registered = outermost (called first, returns last).

## Basic Pattern

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(PerformanceBehavior))
  .Map("echo {message}")
    .WithHandler((string message) => Console.WriteLine(message))
    .AsCommand()
    .Done()
  .Build();
```

## INuruBehavior Interface

All behaviors implement this interface:

```csharp
public interface INuruBehavior
{
  ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed);
}
```

Call `proceed()` to continue to the next behavior (or the handler if this is the innermost behavior). You have full control: catch exceptions, retry, skip execution, or transform behavior.

## BehaviorContext

Every behavior receives a `BehaviorContext` with these properties:

| Property | Type | Description |
|----------|------|-------------|
| `CommandName` | `string` | Name of the route being executed |
| `CorrelationId` | `string` | Unique ID for the execution (for tracing) |
| `CancellationToken` | `CancellationToken` | Token for cancellation |
| `Command` | `object?` | The command object (for endpoints) |

## Example Behaviors

### Logging Behavior

Logs command entry and exit:

```csharp
public sealed class LoggingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    Console.WriteLine($"[START] {context.CommandName}");
    try
    {
      await proceed();
      Console.WriteLine($"[END] {context.CommandName}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[ERROR] {context.CommandName}: {ex.Message}");
      throw;
    }
  }
}
```

### Performance Behavior

Measures execution time and warns on slow commands:

```csharp
public sealed class PerformanceBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    var sw = Stopwatch.StartNew();
    try
    {
      await proceed();
    }
    finally
    {
      sw.Stop();
      if (sw.ElapsedMilliseconds > 500)
        Console.WriteLine($"[SLOW] {context.CommandName}: {sw.ElapsedMilliseconds}ms");
    }
  }
}
```

### Telemetry Behavior

Using `System.Diagnostics.Activity` for OpenTelemetry integration:

```csharp
public sealed class TelemetryBehavior : INuruBehavior
{
  private static readonly ActivitySource Source = new("MyApp.Commands");

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    using var activity = Source.StartActivity(context.CommandName);
    activity?.SetTag("correlation.id", context.CorrelationId);
    
    try
    {
      await proceed();
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

### Exception Handling Behavior

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

      Console.Error.WriteLine($"[EXCEPTION] {message}");
      throw;  // Re-throw or swallow based on your needs
    }
  }
}
```

## Execution Order

Registration order determines execution order:

- **First registered** = outermost (called first, returns last)
- **Last registered** = innermost (called last, returns first)

```csharp
NuruApp.CreateBuilder(args)
  .AddBehavior(typeof(TelemetryBehavior))         // 1st - outermost
  .AddBehavior(typeof(LoggingBehavior))           // 2nd
  .AddBehavior(typeof(ExceptionHandlingBehavior)) // 3rd
  .AddBehavior(typeof(PerformanceBehavior))       // 4th - innermost
  .Map("echo {message}")
    .WithHandler((string message) => Console.WriteLine(message))
    .Done()
  .Build();
```

**Execution flow:**

```
Telemetry.HandleAsync → Logging.HandleAsync → Exception.HandleAsync → Performance.HandleAsync → Handler
                                                                                              ↓
Telemetry completes ← Logging completes ← Exception completes ← Performance completes ← Handler returns
```

## Dependency Injection

Behaviors are singletons. Inject services via constructor:

```csharp
public sealed class AuthorizationBehavior(IAuthService auth) : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    if (!await auth.IsAuthorizedAsync(context.CommandName))
      throw new UnauthorizedAccessException();
    await proceed();
  }
}
```

## Filtered Behaviors

Use `INuruBehavior<T>` to apply behaviors only to routes that implement a specific interface:

```csharp
public interface IRequireAuthorization
{
  string RequiredPermission { get; set; }
}

public sealed class AuthorizationBehavior : INuruBehavior<IRequireAuthorization>
{
  public async ValueTask HandleAsync(BehaviorContext<IRequireAuthorization> context, Func<ValueTask> proceed)
  {
    // context.Command is already IRequireAuthorization - no casting needed
    string permission = context.Command.RequiredPermission;
    
    if (!HasPermission(permission))
      throw new UnauthorizedAccessException($"Required: {permission}");
    
    await proceed();
  }
}
```

Apply to routes with `.Implements<T>()`:

```csharp
NuruApp.CreateBuilder(args)
  .AddBehavior(typeof(AuthorizationBehavior))
  
  // No interface - AuthorizationBehavior does not run
  .Map("echo {message}")
    .WithHandler((string message) => Console.WriteLine(message))
    .Done()
  
  // Has IRequireAuthorization - AuthorizationBehavior runs
  .Map("admin {action}")
    .Implements<IRequireAuthorization>(x => x.RequiredPermission = "admin:execute")
    .WithHandler((string action) => Console.WriteLine($"Admin: {action}"))
    .Done()
  
  .Build();
```

## Key Benefits

| Benefit | Description |
|---------|-------------|
| Full Control | Retry, skip, catch, transform - anything is possible |
| Source-Generated | No runtime reflection or DI overhead |
| Simple | Single `HandleAsync` method |
| Natural Patterns | `using`, `try/catch`, `finally` work as expected |
| Fast | Minimal startup overhead |

## See Also

- [samples/07-pipeline-middleware/](../../../samples/07-pipeline-middleware/) - Complete examples
- [Logging](logging.md) - Logging system integration
- [Routing](routing.md) - Route pattern syntax
