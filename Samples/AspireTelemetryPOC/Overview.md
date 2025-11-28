# Aspire Telemetry POC

This proof-of-concept demonstrates how to send OpenTelemetry data (traces, metrics) from a Nuru CLI application to the standalone .NET Aspire Dashboard using the **Pipeline Middleware pattern**.

## Key Pattern: TelemetryBehavior

This sample uses the recommended **Pipeline Middleware** approach for telemetry:

```csharp
// Register TelemetryBehavior for all commands - automatic instrumentation!
services.AddSingleton<IPipelineBehavior<GreetCommand, Unit>, TelemetryBehavior<GreetCommand, Unit>>();
services.AddSingleton<IPipelineBehavior<WorkCommand, Unit>, TelemetryBehavior<WorkCommand, Unit>>();
```

Benefits:
- **Automatic instrumentation** - No manual `ExecuteWithTelemetry()` calls needed
- **Consistent** - Every command gets the same telemetry treatment
- **Composable** - Combine with other behaviors (logging, performance, auth)
- **Testable** - Behavior can be unit tested in isolation

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

## Pipeline Architecture

```
Request Flow:
┌─────────────────────────────────────────────────────────┐
│ TelemetryBehavior (outermost)                           │
│   - Starts Activity span                                │
│   - Records metrics                                     │
│   - Captures errors                                     │
│   ┌─────────────────────────────────────────────────┐   │
│   │ Command Handler                                 │   │
│   │   - Business logic only                         │   │
│   │   - No telemetry code needed!                   │   │
│   └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

## Telemetry Data Collected

### Traces (Activities)

Each command execution creates a span with:
- **Name**: Command class name (e.g., "GreetCommand", "WorkCommand")
- **Tags**:
  - `command.type` - Full type name
  - `command.name` - Simple type name
  - `error.type` - Exception type (on failure)
  - `error.message` - Exception message (on failure)
- **Status**: `Ok` or `Error`

### Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `nuru.commands.invoked` | Counter | Number of commands executed |
| `nuru.commands.errors` | Counter | Number of failed commands |
| `nuru.commands.duration` | Histogram | Command execution time in ms |

All metrics include tags: `command`, `status`, `error.type` (when applicable).

## Zero Overhead When Disabled

When `OTEL_EXPORTER_OTLP_ENDPOINT` is not set:
- No OTLP exporters are configured
- `ActivitySource` and `Meter` exist but have no listeners
- Activities return `null` (no overhead)
- Metric recordings are no-ops

This follows the OpenTelemetry design principle of zero overhead when telemetry is disabled.

## CLI App Telemetry Flush

**Critical for CLI apps**: Telemetry must be flushed before the process exits:

```csharp
// Flush telemetry before exit - critical for CLI apps!
if (telemetryEnabled)
{
  tracerProvider?.ForceFlush();
  meterProvider?.ForceFlush();
  await Task.Delay(1000); // Allow export to complete
}
```

Without this, telemetry data may be lost because CLI apps exit quickly.

## See Also

- [Pipeline Middleware Sample](../PipelineMiddleware/) - Full middleware patterns
- [TimeWarp.Nuru.Telemetry Package](../../Source/TimeWarp.Nuru.Telemetry/) - Production telemetry package
- [Aspire Dashboard Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [OpenTelemetry .NET SDK](https://opentelemetry.io/docs/languages/net/)
