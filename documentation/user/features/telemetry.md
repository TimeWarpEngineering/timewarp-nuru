# OpenTelemetry Integration

TimeWarp.Nuru integrates with OpenTelemetry for distributed tracing, metrics, and structured logging.

## Enabling Telemetry

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .UseTelemetry()
  .Build();
```

This configures:
- OTLP exporter for traces, metrics, and logs
- Automatic service name from assembly
- Environment-based endpoint detection

## Environment Variables

| Variable | Description |
|----------|-------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP collector endpoint (e.g., `http://localhost:4317`) |
| `OTEL_SERVICE_NAME` | Service name for telemetry (defaults to app name) |

## Aspire Dashboard Integration

When running with .NET Aspire, telemetry automatically flows to the Aspire Dashboard.

**AppHost setup:**

```csharp
// apphost.cs
var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.MyCliApp>("cli");
builder.Build().Run();
```

The Aspire Dashboard provides:
- Distributed traces across commands
- Structured log viewing
- Real-time metrics

## TelemetryBehavior

Combine with pipeline behaviors for per-command tracing:

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

Register with:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .UseTelemetry()
  .AddBehavior(typeof(TelemetryBehavior))
  .Build();
```

## Dual Output Pattern

For CLI apps, use two output channels:
- `Console.WriteLine()` - User feedback (visible in terminal)
- `ILogger<T>` - Telemetry (flows to OTLP collector)

```csharp
[NuruRoute("greet {name}")]
public sealed class GreetCommand : ICommand<Unit>
{
  [Parameter] public string Name { get; set; } = string.Empty;

  public sealed class Handler(ILogger<GreetCommand> logger) 
    : ICommandHandler<GreetCommand, Unit>
  {
    public ValueTask<Unit> Handle(GreetCommand cmd, CancellationToken ct)
    {
      // User sees this in terminal
      Console.WriteLine($"Hello, {cmd.Name}!");
      
      // This flows to Aspire Dashboard / OTLP collector
      logger.LogInformation("Greeted {Name}", cmd.Name);
      
      return default;
    }
  }
}
```

## Running with Aspire

1. Start the Aspire Dashboard: `dotnet run --project AppHost`
2. Run commands - telemetry appears in dashboard
3. View traces, logs, and metrics in real-time

## See Also

- [samples/aspire-otel/](../../../samples/aspire-otel/) - Complete Aspire + OTEL example
- [Pipeline Behaviors](pipeline-behaviors.md) - TelemetryBehavior pattern
