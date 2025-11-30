# Aspire Host OpenTelemetry Sample

## Description

Create a sample demonstrating Aspire Host orchestration with OpenTelemetry and a Nuru console REPL app. This showcases how to run Nuru REPL applications with full telemetry (traces, metrics, structured logs) flowing to the Aspire Dashboard.

Reference: David Fowler tweet on Aspire orchestration patterns.

## Requirements

- Aspire Host project orchestrating the solution
- NuruClient registered as Aspire-managed project
- Nuru console app with REPL that sends telemetry to OTLP collector
- Aspire Dashboard displays traces, metrics, and structured logs
- Both Aspire-launched and interactive REPL sessions send telemetry

## Checklist

- [x] Create Aspire Host project (AppHost)
- [x] Register NuruClient as Aspire-managed project with `builder.AddProject<>()`
- [x] Create Nuru console app with REPL support
- [x] Implement `IHostApplicationBuilder` on `NuruAppBuilder` for Aspire integration
- [x] Configure OTLP export via Aspire-style extension methods
- [x] Demonstrate structured logging (not just Console.WriteLine)
- [x] Verify telemetry appears in Aspire Dashboard
- [x] Create Overview.md explaining the architecture
- [x] Document how to run the sample

## Results

**Fully working!** Both Aspire-launched commands and interactive REPL sessions send telemetry to the AppHost Dashboard.

### Key Implementation Details

1. **IHostApplicationBuilder**: `NuruAppBuilder` implements `IHostApplicationBuilder`, enabling Aspire-style extensions
2. **Project Registration**: NuruClient registered via `builder.AddProject<>().WithArgs("status")`
3. **Interactive REPL**: Works from separate terminal, telemetry flows to same dashboard
4. **Dual Sources**: Dashboard shows `nuruclient` (Aspire-launched) and `nuru-repl-client` (interactive)

### Verified Telemetry

Confirmed via Aspire Dashboard MCP:
- **Traces**: Command spans with timing and attributes
- **Structured Logs**: Semantic properties like `Name`, `MachineName`, `Duration`
- **Correlation**: trace_id and span_id link logs to traces

## Notes

### Architecture
```
┌─────────────────────────────────────────────────┐
│ Aspire AppHost                                  │
│  └─ Dashboard (http://localhost:15186)          │
│       └─ OTLP receiver (port 19034)             │
└─────────────────────────────────────────────────┘
          ▲                         ▲
          │ OTLP                    │ OTLP
┌─────────────────────┐   ┌─────────────────────┐
│ Aspire-launched     │   │ Interactive REPL    │
│ (runs "status")     │   │ (your terminal)     │
└─────────────────────┘   └─────────────────────┘
```

### Structured Logging Example
Commands use `ILogger<T>` with semantic properties:
```csharp
_logger.LogInformation("Greeting {Name} at {Timestamp}", request.Name, DateTime.UtcNow);
```
