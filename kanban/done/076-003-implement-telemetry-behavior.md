# Implement Telemetry Behavior

## Description

Create a TelemetryBehavior using System.Diagnostics.Activity for OpenTelemetry-compatible distributed tracing of CLI commands.

## Parent

076_Add-Pipeline-Middleware-Sample

## Checklist

- [x] Create TelemetryBehavior<TRequest, TResponse> class
- [x] Create ActivitySource for command tracing
- [x] Start Activity for each command execution
- [x] Set tags for command type and parameters
- [x] Set status (Ok/Error) based on execution result
- [x] Demonstrate Activity output in sample

## Results

Implementation added to `Samples/PipelineMiddleware/pipeline-middleware.cs`:

1. **TelemetryBehavior<TMessage, TResponse>** - Pipeline behavior with:
   - `ActivitySource` named "TimeWarp.Nuru.Commands" (version 1.0.0)
   - Creates `Activity` span for each command execution
   - Sets tags: `command.type`, `command.name`
   - On error: adds `error.type`, `error.message` tags
   - Sets `ActivityStatusCode.Ok` or `ActivityStatusCode.Error`

2. **TraceCommand** - Demo command that:
   - Shows Activity information (ID, TraceId, SpanId)
   - Demonstrates what telemetry data is captured
   - Explains OpenTelemetry integration

3. **Overview.md updated** with:
   - TelemetryBehavior documentation
   - Updated pipeline diagram showing telemetry as outermost
   - Usage example for trace command

Usage: `./pipeline-middleware.cs trace "database-query"`

## Notes

```csharp
public sealed class TelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest
{
    private static readonly ActivitySource ActivitySource = new("TimeWarp.Nuru.Commands");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity(typeof(TRequest).Name, ActivityKind.Internal);
        activity?.SetTag("command.type", typeof(TRequest).FullName);
        try
        {
            var response = await next();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

This integrates with OpenTelemetry exporters (Jaeger, Zipkin, OTLP) for distributed tracing visualization.
