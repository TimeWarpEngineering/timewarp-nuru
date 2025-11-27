# Implement Telemetry Behavior

## Description

Create a TelemetryBehavior using System.Diagnostics.Activity for OpenTelemetry-compatible distributed tracing of CLI commands.

## Parent

076_Add-Pipeline-Middleware-Sample

## Checklist

- [ ] Create TelemetryBehavior<TRequest, TResponse> class
- [ ] Create ActivitySource for command tracing
- [ ] Start Activity for each command execution
- [ ] Set tags for command type and parameters
- [ ] Set status (Ok/Error) based on execution result
- [ ] Demonstrate Activity output in sample

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
