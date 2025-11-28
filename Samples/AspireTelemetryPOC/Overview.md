# Aspire Telemetry POC

This proof-of-concept demonstrates how to send OpenTelemetry data (traces, metrics, logs) from a Nuru CLI application to the standalone .NET Aspire Dashboard.

## Prerequisites

### 1. Start Aspire Dashboard

Run the Aspire Dashboard in a Docker container:

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 --name aspire-dashboard \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

**Ports:**
- `18888` - Dashboard web UI
- `4317` (mapped from 18889) - OTLP endpoint for receiving telemetry

**Note:** Copy the login token from the container output when it starts.

### 2. Set Environment Variables

Configure the OTLP exporter endpoint and service name:

**Bash/Zsh:**
```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
export OTEL_SERVICE_NAME=nuru-telemetry-poc
```

**PowerShell:**
```powershell
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
$env:OTEL_SERVICE_NAME = "nuru-telemetry-poc"
```

## Running the Sample

```bash
# Check telemetry status
./Samples/AspireTelemetryPOC/aspire-telemetry-poc.cs status

# Execute commands to generate telemetry
./Samples/AspireTelemetryPOC/aspire-telemetry-poc.cs greet "World"
./Samples/AspireTelemetryPOC/aspire-telemetry-poc.cs work 500
./Samples/AspireTelemetryPOC/aspire-telemetry-poc.cs fail "Test error"

# View help
./Samples/AspireTelemetryPOC/aspire-telemetry-poc.cs --help
```

## Viewing Telemetry

1. Open http://localhost:18888 in your browser
2. Enter the login token from the container output
3. Navigate to:
   - **Traces** - View command execution spans
   - **Metrics** - View command counters and duration histograms
   - **Logs** - View application logs

## Telemetry Data Collected

### Traces (Activities)

Each command execution creates a span with:
- **Name**: Command name (e.g., "greet", "work", "fail")
- **Tags**:
  - `command.name` - The command being executed
  - `error.type` - Exception type (on failure)
  - `error.message` - Exception message (on failure)
- **Status**: `Ok` or `Error`

### Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `nuru.commands.invoked` | Counter | Number of commands executed |
| `nuru.commands.errors` | Counter | Number of failed commands |
| `nuru.commands.duration` | Histogram | Command execution time in ms |

All metrics include a `command` tag for filtering by command name.

### Logs

Application logs are sent to the OTLP endpoint via the OpenTelemetry logging provider. These appear in the Aspire Dashboard's Logs section with structured data.

## Zero Overhead When Disabled

When `OTEL_EXPORTER_OTLP_ENDPOINT` is not set:
- No OTLP exporters are configured
- `ActivitySource` and `Meter` exist but have no listeners
- Activities return `null` (no overhead)
- Metric recordings are no-ops

This follows the OpenTelemetry design principle of zero overhead when telemetry is disabled.

## Architecture

```
┌─────────────────────┐     OTLP/gRPC      ┌──────────────────────┐
│   Nuru CLI App      │ ──────────────────▶│  Aspire Dashboard    │
│                     │                    │                      │
│ ┌─────────────────┐ │                    │ ┌──────────────────┐ │
│ │ ActivitySource  │ │ ─── Traces ──────▶ │ │ Trace Viewer     │ │
│ └─────────────────┘ │                    │ └──────────────────┘ │
│                     │                    │                      │
│ ┌─────────────────┐ │                    │ ┌──────────────────┐ │
│ │     Meter       │ │ ─── Metrics ─────▶ │ │ Metrics Viewer   │ │
│ └─────────────────┘ │                    │ └──────────────────┘ │
│                     │                    │                      │
│ ┌─────────────────┐ │                    │ ┌──────────────────┐ │
│ │ ILoggerFactory  │ │ ─── Logs ────────▶ │ │ Log Viewer       │ │
│ └─────────────────┘ │                    │ └──────────────────┘ │
└─────────────────────┘                    └──────────────────────┘
```

## Next Steps

This POC validates the integration approach. The next step is to implement the `TimeWarp.Nuru.Telemetry` package that provides:

- `UseAspireTelemetry()` extension method
- Built-in instrumentation in `NuruApp.ExecuteAsync()`
- REPL session tracking
- Automatic route matching telemetry

See task `080_002_Implement-TimeWarp-Nuru-Telemetry-Package` for details.

## See Also

- [Aspire Dashboard Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [OpenTelemetry .NET SDK](https://opentelemetry.io/docs/languages/net/)
- [OTLP Exporter](https://opentelemetry.io/docs/specs/otlp/)
