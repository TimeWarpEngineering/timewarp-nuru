# Pipeline Middleware Sample

This sample demonstrates how to implement cross-cutting concerns using Mediator pipeline behaviors (middleware) in TimeWarp.Nuru CLI applications.

## What is Pipeline Middleware?

Pipeline behaviors are interceptors that wrap command execution, allowing you to add functionality before and after a command runs. They work like layers of an onion:

```
Request Flow:
┌───────────────────────────────────────────────────────────────────────┐
│ TelemetryBehavior (outermost - registered first)                      │
│   ┌───────────────────────────────────────────────────────────────┐   │
│   │ LoggingBehavior                                               │   │
│   │   ┌───────────────────────────────────────────────────────┐   │   │
│   │   │ PerformanceBehavior                                   │   │   │
│   │   │   ┌───────────────────────────────────────────────┐   │   │   │
│   │   │   │ AuthorizationBehavior                         │   │   │   │
│   │   │   │   ┌───────────────────────────────────────┐   │   │   │   │
│   │   │   │   │ RetryBehavior                         │   │   │   │   │
│   │   │   │   │   ┌───────────────────────────────┐   │   │   │   │   │
│   │   │   │   │   │ ExceptionHandlingBehavior     │   │   │   │   │   │
│   │   │   │   │   │   ┌───────────────────────┐   │   │   │   │   │   │
│   │   │   │   │   │   │ Command Handler       │   │   │   │   │   │   │
│   │   │   │   │   │   │ (innermost)           │   │   │   │   │   │   │
│   │   │   │   │   │   └───────────────────────┘   │   │   │   │   │   │
│   │   │   │   │   └───────────────────────────────┘   │   │   │   │   │
│   │   │   │   └───────────────────────────────────────┘   │   │   │   │
│   │   │   └───────────────────────────────────────────────┘   │   │   │
│   │   └───────────────────────────────────────────────────────┘   │   │
│   └───────────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────────┘
```

## Samples

| Sample | Description |
|--------|-------------|
| [pipeline-middleware.cs](pipeline-middleware.cs) | Complete example with all behaviors |

## Pipeline Behaviors Demonstrated

### TelemetryBehavior

Creates OpenTelemetry-compatible Activity spans for distributed tracing:

```csharp
public sealed class TelemetryBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
{
    private static readonly ActivitySource CommandActivitySource = new("TimeWarp.Nuru.Commands", "1.0.0");

    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken ct)
    {
        using Activity? activity = CommandActivitySource.StartActivity(typeof(TMessage).Name, ActivityKind.Internal);
        activity?.SetTag("command.type", typeof(TMessage).FullName);
        activity?.SetTag("command.name", typeof(TMessage).Name);

        try
        {
            TResponse response = await next(message, ct);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
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

### LoggingBehavior

Logs command entry and exit for observability:

```csharp
public sealed class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken ct)
    {
        Logger.LogInformation("[PIPELINE] Handling {RequestName}", typeof(TMessage).Name);
        try
        {
            TResponse response = await next(message, ct);
            Logger.LogInformation("[PIPELINE] Completed {RequestName}", typeof(TMessage).Name);
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[PIPELINE] Error handling {RequestName}", typeof(TMessage).Name);
            throw;
        }
    }
}
```

### PerformanceBehavior

Measures execution time and warns on slow commands:

```csharp
public sealed class PerformanceBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
{
    private const int SlowThresholdMs = 500;

    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken ct)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        TResponse response = await next(message, ct);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowThresholdMs)
            Logger.LogWarning("[PERFORMANCE] {RequestName} took {ElapsedMs}ms", typeof(TMessage).Name, stopwatch.ElapsedMilliseconds);
        else
            Logger.LogInformation("[PERFORMANCE] {RequestName} completed in {ElapsedMs}ms", typeof(TMessage).Name, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

### AuthorizationBehavior

Checks permissions using marker interface pattern:

```csharp
public interface IRequireAuthorization
{
    string RequiredPermission { get; }
}

public sealed class AuthorizationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken ct)
    {
        if (message is IRequireAuthorization authRequest)
        {
            // Check authorization (demo uses environment variable)
            if (Environment.GetEnvironmentVariable("CLI_AUTHORIZED") != "1")
                throw new UnauthorizedAccessException($"Permission required: {authRequest.RequiredPermission}");
        }
        return await next(message, ct);
    }
}
```

### RetryBehavior

Implements exponential backoff for transient failures:

```csharp
public interface IRetryable
{
    int MaxRetries => 3;
}

public sealed class RetryBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken ct)
    {
        if (message is not IRetryable retryable)
            return await next(message, ct);

        for (int attempt = 1; attempt <= retryable.MaxRetries + 1; attempt++)
        {
            try { return await next(message, ct); }
            catch (Exception ex) when (IsTransient(ex) && attempt <= retryable.MaxRetries)
            {
                TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, ct);
            }
        }
        throw new InvalidOperationException("Unreachable");
    }

    private static bool IsTransient(Exception ex) =>
        ex is HttpRequestException or TimeoutException or IOException;
}
```

### ExceptionHandlingBehavior

Provides consistent error handling with user-friendly messages:

```csharp
public sealed class ExceptionHandlingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken ct)
    {
        try { return await next(message, ct); }
        catch (ValidationException ex)
        {
            Console.Error.WriteLine($"Validation error: {ex.Message}");
            throw new CommandExecutionException(typeof(TMessage).Name, "Validation failed", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Access denied: {ex.Message}");
            throw new CommandExecutionException(typeof(TMessage).Name, "Authorization failed", ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: An unexpected error occurred. See logs for details.");
            throw new CommandExecutionException(typeof(TMessage).Name, "Unexpected error", ex);
        }
    }
}
```

## Registration Order

**Critical**: Registration order determines execution order.

- **First registered** = outermost (executes first on request, last on response)
- **Last registered** = innermost (executes last on request, first on response)

```csharp
services.AddSingleton<IPipelineBehavior<MyCommand, Unit>, TelemetryBehavior<MyCommand, Unit>>();        // 1st - outermost
services.AddSingleton<IPipelineBehavior<MyCommand, Unit>, LoggingBehavior<MyCommand, Unit>>();          // 2nd
services.AddSingleton<IPipelineBehavior<MyCommand, Unit>, PerformanceBehavior<MyCommand, Unit>>();      // 3rd
services.AddSingleton<IPipelineBehavior<MyCommand, Unit>, AuthorizationBehavior<MyCommand, Unit>>();    // 4th
services.AddSingleton<IPipelineBehavior<MyCommand, Unit>, RetryBehavior<MyCommand, Unit>>();            // 5th
services.AddSingleton<IPipelineBehavior<MyCommand, Unit>, ExceptionHandlingBehavior<MyCommand, Unit>>(); // 6th - innermost
```

## Marker Interface Pattern

Marker interfaces enable selective behavior application. Only commands implementing the interface receive the behavior:

```csharp
// Command with authorization requirement
public sealed class AdminCommand : IRequest, IRequireAuthorization
{
    public string RequiredPermission => "admin:execute";
}

// Command with retry support
public sealed class FlakyCommand : IRequest, IRetryable
{
    public int MaxRetries => 3;
}

// Standard command - no special behaviors
public sealed class EchoCommand : IRequest { }
```

## Comparison with Cocona

| Aspect | Cocona | Nuru + Mediator |
|--------|--------|-----------------|
| **Approach** | Attribute-based filters | Pipeline behaviors |
| **Registration** | `[CommandFilter]` attribute | DI container registration |
| **Ordering** | Filter attribute order | Registration order |
| **Selectivity** | Per-command attributes | Marker interfaces + DI |
| **Testability** | Requires mocking attributes | Standard DI mocking |
| **Composition** | Limited | Full composition support |

### Cocona Filter Example

```csharp
[CommandFilter(typeof(LoggingFilter))]
public class MyCommand
{
    public void Execute() { }
}
```

### Nuru + Mediator Example

```csharp
// Registration
services.AddSingleton<IPipelineBehavior<MyCommand, Unit>, LoggingBehavior<MyCommand, Unit>>();

// Command
public sealed class MyCommand : IRequest
{
    public sealed class Handler : IRequestHandler<MyCommand>
    {
        public ValueTask<Unit> Handle(MyCommand request, CancellationToken ct) { }
    }
}
```

## Running the Sample

```bash
# Basic echo command
./samples/pipeline-middleware/pipeline-middleware.cs echo "Hello, World!"

# Slow command (triggers performance warning at >500ms)
./samples/pipeline-middleware/pipeline-middleware.cs slow 600

# Admin command (requires CLI_AUTHORIZED=1)
./samples/pipeline-middleware/pipeline-middleware.cs admin "delete-all"
CLI_AUTHORIZED=1 ./samples/pipeline-middleware/pipeline-middleware.cs admin "delete-all"

# Flaky command (simulates transient failures with retry)
./samples/pipeline-middleware/pipeline-middleware.cs flaky 2

# Error command (demonstrates exception handling)
./samples/pipeline-middleware/pipeline-middleware.cs error validation
./samples/pipeline-middleware/pipeline-middleware.cs error auth
./samples/pipeline-middleware/pipeline-middleware.cs error unknown

# Trace command (demonstrates distributed tracing)
./samples/pipeline-middleware/pipeline-middleware.cs trace "database-query"

# Help
./samples/pipeline-middleware/pipeline-middleware.cs --help
```

## AOT Considerations

For AOT/runfile scenarios, use **explicit generic registrations** instead of open generic registration:

```csharp
// Good for AOT - explicit registration
services.AddSingleton<IPipelineBehavior<MyCommand, Unit>, LoggingBehavior<MyCommand, Unit>>();

// May cause trimmer issues - open generic registration
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

## Key Benefits

1. **Separation of Concerns**: Each behavior handles one responsibility
2. **Composability**: Mix and match behaviors per command
3. **Testability**: Test behaviors in isolation
4. **Consistency**: Same patterns across all commands
5. **Flexibility**: Add/remove behaviors without changing command code

## See Also

- [Unified Middleware Sample](../UnifiedMiddleware/unified-middleware.cs) - Pipeline behaviors for delegate routes
- [Mediator Documentation](https://github.com/martinothamar/Mediator)
- [Pipeline Behavior Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api#implement-the-command-process-pipeline-with-a-mediator-pattern-mediatr)
