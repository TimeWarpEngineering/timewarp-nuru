# TimeWarp.Nuru.Telemetry

OpenTelemetry integration for TimeWarp.Nuru CLI applications. Works with any OTLP-compatible backend (Aspire Dashboard, Jaeger, Zipkin, Grafana, etc.).

## Installation

```bash
dotnet add package TimeWarp.Nuru.Telemetry
```

## Quick Start

```csharp
using TimeWarp.Nuru;

var app = NuruApp.CreateBuilder(args)
    .UseTelemetry()  // Auto-configures when OTEL env vars present
    .Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
    .Build();

await app.RunAsync(args);
```

## Configuration

### Environment Variables

The standard OpenTelemetry environment variables are supported:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
export OTEL_SERVICE_NAME=my-nuru-app
```

### Programmatic Configuration

```csharp
builder.UseTelemetry(options =>
{
    options.ServiceName = "my-app";
    options.ServiceVersion = "1.0.0";
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.OtlpEndpoint = "http://localhost:4317";
});
```

## Telemetry Data

### Traces (Activities)

- **ActivitySource**: `TimeWarp.Nuru`
- Command execution spans with status and error information

### Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `nuru.commands.invoked` | Counter | Commands executed |
| `nuru.commands.errors` | Counter | Failed commands |
| `nuru.commands.duration` | Histogram | Execution time (ms) |
| `nuru.repl.sessions` | Counter | REPL sessions started |
| `nuru.repl.commands` | Counter | REPL commands executed |

## Running with Aspire Dashboard

```bash
# Start Aspire Dashboard
docker run --rm -it -p 18888:18888 -p 4317:18889 --name aspire-dashboard \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest

# Set environment variables
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
export OTEL_SERVICE_NAME=my-nuru-app

# Run your app
./my-nuru-app greet "World"

# View telemetry at http://localhost:18888
```

## Zero Overhead

When `OTEL_EXPORTER_OTLP_ENDPOINT` is not set, telemetry export is disabled with zero overhead. The `ActivitySource` and `Meter` exist but have no listeners.

## See Also

- [Aspire Telemetry Sample](../../Samples/AspireTelemetry/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
