# OpenTelemetry Integration

TimeWarp.Nuru integrates with OpenTelemetry for distributed tracing, metrics, and structured logging.

## Enabling Telemetry

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .UseTelemetry()
  .Build();
```

This configures:
- A span and metrics for every command (see [What UseTelemetry() emits](#what-usetelemetry-emits))
- OTLP exporter for traces, metrics, and logs
- Automatic service name from assembly
- Environment-based endpoint detection

## What UseTelemetry() emits

`UseTelemetry()` registers the built-in `TimeWarp.Nuru.TelemetryBehavior` as the outermost
pipeline behavior, so every command a route runs is instrumented. You do not need
`.AddBehavior(typeof(TelemetryBehavior))`; an app that adds it anyway still gets one span per
command.

**Traces** (ActivitySource `TimeWarp.Nuru.Behavior`): one `Internal` span per command.

| Field | Value |
|-------|-------|
| Span name | The route pattern, e.g. `deploy {env}` |
| `command.name` | The route pattern |
| `command.type` | The command class (endpoint) or the generated command class (fluent route) |
| Status | `Ok` on success; `Error` when the handler throws |
| `error.type` | Exception type name, on failure only |

The span is a child of `Activity.Current`, so a host that starts its own activity before
`RunAsync` sees the command span nested under it.

**Metrics** (Meter `TimeWarp.Nuru.Behavior`): `nuru.behavior.commands.invoked`,
`nuru.behavior.commands.errors` and the `nuru.behavior.commands.duration` histogram (ms), each
tagged with `command`.

**Logs**: the app's `LoggerFactory` exports `ILogger` records through OTLP.

Setting `EnableTracing` and `EnableMetrics` both to `false` leaves the behavior out.

## Environment Variables

| Variable | Description |
|----------|-------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP collector base URL (e.g., `http://localhost:4317`). Export is on only when this or `OtlpEndpoint` is set |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` (SDK default) or `http/protobuf` |
| `OTEL_EXPORTER_OTLP_{TRACES,METRICS,LOGS}_ENDPOINT` | Full URL for one signal, used as-is; overrides the base URL for that signal |
| `OTEL_EXPORTER_OTLP_{TRACES,METRICS,LOGS}_PROTOCOL` | Protocol for one signal; overrides `OTEL_EXPORTER_OTLP_PROTOCOL` |
| `OTEL_SERVICE_NAME` | Service name for telemetry (defaults to app name) |

### Endpoint paths

The endpoint, from `OTEL_EXPORTER_OTLP_ENDPOINT` or the `OtlpEndpoint` option, is a **base URL**.
With `http/protobuf`, each signal posts to its own path under it:

| Signal | URL for base `https://collector:4318/` |
|--------|----------------------------------------|
| Traces | `https://collector:4318/v1/traces` |
| Metrics | `https://collector:4318/v1/metrics` |
| Logs | `https://collector:4318/v1/logs` |

The signal path is not added twice when the base already ends with it. With gRPC, the URL is
used unchanged. A per-signal `OTEL_EXPORTER_OTLP_{SIGNAL}_ENDPOINT` is a full URL and is used
as-is. An `OtlpEndpoint` set in code takes precedence over every endpoint variable.

## OTLP sink trust

OTLP export is opt-in (endpoint via options or `OTEL_EXPORTER_OTLP_ENDPOINT`). Any collector that receives export is a **trust boundary**: it can see traces, metrics, and logs from the process. Point export only at collectors you control or otherwise trust.

On failure, Nuru records `error.type` (exception type name) by default and does **not** set `error.message` or Activity status detail from `Exception.Message`, so argv- or secret-bearing exception text is not exported.

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

## Custom telemetry behavior

`UseTelemetry()` already traces every command. To add your own tags (here a correlation id) on
your own ActivitySource, write a behavior and register it. Add your source to the host's tracer
provider if you export it yourself.

```csharp
public sealed class CorrelationTelemetryBehavior : INuruBehavior
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
      // Prefer error.type only — do not export Exception.Message to OTLP by default
      activity?.SetStatus(ActivityStatusCode.Error);
      activity?.SetTag("error.type", ex.GetType().Name);
      throw;
    }
  }
}
```

Register with:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .UseTelemetry()
  .AddBehavior(typeof(CorrelationTelemetryBehavior))
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
    public Task<Unit> Handle(GreetCommand cmd, CancellationToken ct)
    {
      // User sees this in terminal
      Console.WriteLine($"Hello, {cmd.Name}!");

      // This flows to Aspire Dashboard / OTLP collector
      logger.LogInformation("Greeted {Name}", cmd.Name);

      return Unit.Task;
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
- [Pipeline Behaviors](pipeline-behaviors.md) - Behavior pattern
