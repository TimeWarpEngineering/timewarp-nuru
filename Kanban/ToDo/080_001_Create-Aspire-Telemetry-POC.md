# Create Aspire Telemetry POC

## Description

Create a proof-of-concept demonstrating a Nuru CLI app sending telemetry (traces, metrics, logs) to the standalone Aspire Dashboard. This validates the integration approach before implementing the full `TimeWarp.Nuru.Telemetry` package.

## Parent

080_Investigate-OpenTelemetry-Aspire-Integration

## Requirements

- Create a standalone sample in `Samples/AspireTelemetryPOC/`
- Demonstrate traces for command execution
- Demonstrate custom metrics (command count, duration)
- Demonstrate logs flowing to Aspire Dashboard
- Use environment variables for OTLP configuration
- Verify zero overhead when `OTEL_EXPORTER_OTLP_ENDPOINT` not set

## Checklist

### Setup
- [ ] Create `Samples/AspireTelemetryPOC/` directory
- [ ] Create runfile `aspire-telemetry-poc.cs`
- [ ] Add required OpenTelemetry package references

### Implementation
- [ ] Configure `ActivitySource` for command tracing
- [ ] Configure `Meter` for command metrics
- [ ] Configure `ILoggerFactory` with OpenTelemetry provider
- [ ] Add OTLP exporter with environment variable detection
- [ ] Wrap command execution in Activity spans
- [ ] Record command duration histogram

### Testing
- [ ] Start Aspire Dashboard container
- [ ] Run POC app and execute commands
- [ ] Verify traces appear in dashboard
- [ ] Verify metrics appear in dashboard
- [ ] Verify logs appear in dashboard
- [ ] Verify no overhead when OTEL endpoint not configured

### Documentation
- [ ] Create `Overview.md` with setup instructions
- [ ] Document how to run Aspire Dashboard
- [ ] Document expected telemetry output

## Notes

### Running Aspire Dashboard

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 --name aspire-dashboard \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Dashboard URL: http://localhost:18888
Copy login token from container output.

### Environment Variables

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
export OTEL_SERVICE_NAME=nuru-telemetry-poc
```

### POC Structure

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru@3.0.0-beta.4
#:package OpenTelemetry.Exporter.OpenTelemetryProtocol
#:package OpenTelemetry.Extensions.Hosting

using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

// Define telemetry sources
var activitySource = new ActivitySource("TimeWarp.Nuru.POC");
var meter = new Meter("TimeWarp.Nuru.POC");
var commandCounter = meter.CreateCounter<int>("nuru.commands.invoked");
var commandDuration = meter.CreateHistogram<double>("nuru.commands.duration", "ms");

// Configure OpenTelemetry (only when OTEL endpoint configured)
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
// ... setup code ...

var app = NuruApp.CreateBuilder()
    .Map("greet {name}", (string name) => {
        using var activity = activitySource.StartActivity("greet");
        activity?.SetTag("name", name);
        var sw = Stopwatch.StartNew();
        Console.WriteLine($"Hello, {name}!");
        commandCounter.Add(1, new KeyValuePair<string, object?>("command", "greet"));
        commandDuration.Record(sw.ElapsedMilliseconds);
    })
    .Build();
```
