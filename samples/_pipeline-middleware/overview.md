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
│   │   │ AuthorizationBehavior                                 │   │   │
│   │   │   ┌───────────────────────────────────────────────┐   │   │   │
│   │   │   │ RetryBehavior                                 │   │   │   │
│   │   │   │   ┌───────────────────────────────────────┐   │   │   │   │
│   │   │   │   │ PerformanceBehavior                   │   │   │   │   │
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

| Sample | Description | Concepts |
|--------|-------------|----------|
| [pipeline-middleware-basic.cs](pipeline-middleware-basic.cs) | Logging and performance monitoring | Before/after pattern, timing |
| [pipeline-middleware-authorization.cs](pipeline-middleware-authorization.cs) | Permission checks with marker interfaces | Selective behavior, IRequireAuthorization |
| [pipeline-middleware-retry.cs](pipeline-middleware-retry.cs) | Resilience with exponential backoff | Transient failures, IRetryable |
| [pipeline-middleware-exception.cs](pipeline-middleware-exception.cs) | Consistent error handling | Exception categories, user-friendly messages |
| [pipeline-middleware-telemetry.cs](pipeline-middleware-telemetry.cs) | OpenTelemetry distributed tracing | Activity spans, observability |
| [pipeline-middleware.cs](pipeline-middleware.cs) | **Complete example** with all behaviors | Full reference implementation |

## Quick Start

Run any sample directly:

```bash
# Basic logging and performance
./pipeline-middleware-basic.cs echo "Hello, World!"
./pipeline-middleware-basic.cs slow 600

# Authorization with marker interface
./pipeline-middleware-authorization.cs admin delete-all       # Access denied
CLI_AUTHORIZED=1 ./pipeline-middleware-authorization.cs admin delete-all  # Success

# Retry with exponential backoff
./pipeline-middleware-retry.cs flaky 2    # Fails twice, then succeeds

# Exception handling
./pipeline-middleware-exception.cs error validation
./pipeline-middleware-exception.cs error unknown

# Distributed tracing
./pipeline-middleware-telemetry.cs trace database-query
```

## Pipeline Behaviors Demonstrated

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
services.AddMediator(options =>
{
    options.PipelineBehaviors =
    [
        typeof(TelemetryBehavior<,>),         // 1st - outermost
        typeof(LoggingBehavior<,>),           // 2nd
        typeof(AuthorizationBehavior<,>),     // 3rd
        typeof(RetryBehavior<,>),             // 4th
        typeof(PerformanceBehavior<,>),       // 5th
        typeof(ExceptionHandlingBehavior<,>)  // 6th - innermost
    ];
});
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

## Key Benefits

1. **Separation of Concerns**: Each behavior handles one responsibility
2. **Composability**: Mix and match behaviors per command
3. **Testability**: Test behaviors in isolation
4. **Consistency**: Same patterns across all commands
5. **Flexibility**: Add/remove behaviors without changing command code

## See Also

- [Unified Middleware Sample](../unified-middleware/unified-middleware.cs) - Pipeline behaviors for delegate routes
- [Aspire Telemetry Sample](../aspire-telemetry/aspire-telemetry.cs) - Full OpenTelemetry integration
- [Mediator Documentation](https://github.com/martinothamar/Mediator)
