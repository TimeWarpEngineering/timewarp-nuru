# Aspire + OpenTelemetry

Demonstrates OpenTelemetry integration with .NET Aspire for traces, metrics, and logs.

## Run It

```bash
# Run via AppHost (telemetry auto-configured; REPL under WithTerminal)
cd samples/aspire-otel
aspire run
# or: ./apphost.cs
# Dashboard Terminal view works with no extra config.
# CLI attach only:
aspire config set features.terminalCommandsEnabled true
aspire terminal attach nuruclient

# Run standalone with OTLP endpoint
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 \
  ./nuru-client.cs greet Alice
```

## What's Demonstrated

- **apphost.cs**: .NET Aspire AppHost configuration
- **nuru-client.cs**: CLI with OpenTelemetry auto-wiring
- `.UseTelemetry()` for OTLP export (traces, metrics, logs)
- `TelemetryBehavior` in the pipeline
- Dual output: `Console.WriteLine` for terminal, `ILogger` for telemetry
- Interactive REPL mode with telemetry

## Related Documentation

- [Telemetry](../../documentation/user/features/telemetry.md)
