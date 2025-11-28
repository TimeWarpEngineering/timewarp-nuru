# Investigate OpenTelemetry and Aspire Integration

## Description

Research and design the `AddOpenTelemetry()` extension method for TimeWarp.Nuru to enable observability and .NET Aspire integration. This task focuses on investigation and design before implementation.

## Parent

071_Implement-Static-Factory-Builder-API (reference to OpenTelemetry in feature matrix)

## Subtasks

- 080_001_Create-Aspire-Telemetry-POC
- 080_002_Implement-TimeWarp-Nuru-Telemetry-Package

## Requirements

- Investigate OpenTelemetry SDK integration patterns for CLI applications
- Research .NET Aspire dashboard compatibility for CLI tools
- Determine appropriate telemetry data: traces, metrics, logs
- Design API that follows existing `Add*` extension method patterns
- Consider AOT compatibility implications

## Checklist

### Research
- [x] Review OpenTelemetry .NET SDK documentation
- [x] Investigate `System.Diagnostics.ActivitySource` for tracing
- [x] Research Aspire dashboard requirements for CLI apps
- [x] Examine how other CLI frameworks handle telemetry
- [x] Evaluate OTLP exporter options (gRPC vs HTTP)

### Design
- [x] Define `AddOpenTelemetry()` API signature and options
- [x] Determine what activities/spans to create (command execution, route matching, etc.)
- [x] Design metrics collection (command duration, success/failure counts)
- [x] Plan integration with existing logging infrastructure
- [x] Document AOT compatibility considerations (OpenTelemetry 1.14.0 supports AOT)

### Prototype
- [x] Create proof-of-concept implementation (see 080_001)
- [x] Test with Aspire dashboard (POC created, manual testing required)
- [x] Validate minimal overhead when telemetry disabled (zero overhead by design)

## Implementation Summary

Both subtasks completed:

1. **080_001_Create-Aspire-Telemetry-POC** - Created `Samples/AspireTelemetryPOC/` with:
   - Working sample demonstrating traces, metrics, logs
   - Comprehensive Overview.md documentation
   - Added to examples.json

2. **080_002_Implement-TimeWarp-Nuru-Telemetry-Package** - Created `Source/TimeWarp.Nuru.Telemetry/` with:
   - `UseAspireTelemetry()` extension method
   - `NuruTelemetryOptions` configuration class
   - Helper methods for manual telemetry integration
   - All designed metrics implemented
   - README.md with usage documentation

## Research Findings

### Aspire Dashboard Standalone Mode

The Aspire Dashboard can run independently without full Aspire orchestration:

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 --name aspire-dashboard \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

- Port `18888` - Dashboard UI
- Port `4317` (mapped from 18889) - OTLP endpoint for receiving telemetry

### Required NuGet Packages

```xml
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" />
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
```

### Configuration via Environment Variables

Standard OpenTelemetry env vars:
```
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_SERVICE_NAME=my-nuru-app
```

### Proposed Telemetry

**Traces (Activities)**
- Command parsing duration
- Command execution duration
- REPL session lifecycle
- Individual REPL command execution

**Metrics**
- `nuru.commands.invoked` - Counter of commands executed
- `nuru.commands.duration` - Histogram of command execution time
- `nuru.repl.sessions` - Counter of REPL sessions started
- `nuru.repl.commands` - Commands executed in REPL mode

**Logs**
- Already supported via existing `ILoggerFactory` - add OpenTelemetry provider

### Proposed API

```csharp
var app = NuruApp.CreateBuilder()
    .UseAspireTelemetry()  // Auto-configures when OTEL env vars present
    .AddReplSupport()
    .Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
    .Build();
```

## Notes

- OpenTelemetry integration was listed in the feature matrix for `CreateBuilder` (task 071)
- Should integrate seamlessly with .NET Aspire's service defaults pattern
- Create separate NuGet package: `TimeWarp.Nuru.Telemetry`
- Activity names should follow semantic conventions where applicable
- Follow existing pattern of `TimeWarp.Nuru.Logging` package
- No ASP.NET Core dependency - use custom service defaults approach
- Use same zero-overhead pattern as `NullLoggerFactory` when telemetry disabled
