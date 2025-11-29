# Aspire Host OpenTelemetry Sample

## Description

Create a sample demonstrating Aspire Host orchestration with OpenTelemetry and a Nuru console REPL app. This showcases how to run Nuru REPL applications with full telemetry (traces, metrics, structured logs) flowing to the Aspire Dashboard.

Reference: David Fowler tweet on Aspire orchestration patterns.

## Requirements

- Aspire Host project orchestrating the solution
- OpenTelemetry Collector via `CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector`
- Nuru console app with REPL that sends telemetry to OTLP collector
- Aspire Dashboard displays traces, metrics, and structured logs

## Checklist

- [ ] Create Aspire Host project (AppHost)
- [ ] Add OpenTelemetry Collector resource
- [ ] Create Nuru console app with REPL support
- [ ] Configure OTLP export to collector
- [ ] Demonstrate structured logging (not just Console.WriteLine)
- [ ] Verify telemetry appears in Aspire Dashboard
- [ ] Create Overview.md explaining the architecture
- [ ] Document how to run the sample

## Notes

### Key Packages
- `CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector` version 13.0.0+
- `TimeWarp.Nuru` with `UseTelemetry()`
- `TimeWarp.Nuru.Repl` for REPL support

### Architecture
```
┌─────────────────────────────────────────────────────────────┐
│ Aspire AppHost                                              │
│  ├─ OpenTelemetry Collector (OTLP receiver)                 │
│  ├─ Aspire Dashboard (traces, metrics, logs)                │
│  └─ Nuru Console App (REPL with telemetry)                  │
│       └─ Uses structured ILogger, not Console.WriteLine     │
└─────────────────────────────────────────────────────────────┘
```

### Structured Logging Example
Commands should use `ILogger<T>` with semantic properties:
```csharp
_logger.LogInformation("Greeting {Name} at {Timestamp}", request.Name, DateTime.UtcNow);
```
NOT:
```csharp
Console.WriteLine($"Hello, {request.Name}!");
```

This ensures logs flow through OTEL pipeline to Aspire Dashboard.
