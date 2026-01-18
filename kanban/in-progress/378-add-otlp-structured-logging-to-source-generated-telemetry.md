# Add OTLP structured logging to source-generated telemetry

## Description

Extend the source-generated telemetry to include OpenTelemetry logging export. When `UseTelemetry()` is called, logs via `ILogger<T>` will be exported to OTLP alongside traces and metrics. No console output - keeps CLI UX clean.

## Current State

- Traces: Working via OTLP ✓
- Metrics: Working via OTLP ✓
- Logs: Console only, NOT exported to OTLP

## Checklist

- [ ] Modify `telemetry-emitter.cs` to emit OpenTelemetry logging setup
- [ ] Change `NuruCoreApp.LoggerFactory` from `init` to `set`
- [ ] Verify package references include OpenTelemetry logging exporter
- [ ] Build and test with Aspire sample
- [ ] Verify logs appear in Aspire Dashboard Structured Logs tab

## Notes

### Files to Modify
- `source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs`
- `source/timewarp-nuru/nuru-core-app.cs`

### Rationale
This is a CLI framework. Console output is for command results, not log noise. OTLP captures logs for debugging/monitoring without polluting the user's terminal. Console logger currently writes to stdout, mixing with command output.
