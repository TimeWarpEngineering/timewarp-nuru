# Redact telemetry error.message on OTLP export

Parent: 470 (2026-09-04 full-repo review). Severity: suggestion (M33).

## Description

On failure, `TelemetryBehavior` (`telemetry-behavior.cs:58-60`) and the generated twin (`telemetry-emitter.cs:120`) set Activity status/detail and tag `error.message` to `ex.Message`. If OTLP export is enabled (`OTEL_EXPORTER_OTLP_ENDPOINT` / options), exception text that embeds user argv or secrets can leave the process.

Export is opt-in; tags do not include raw argv today (`command.name` / `command.type` only). `UseTelemetry(Action<NuruTelemetryOptions>)` not applying options is **470-001**.

## Requirements

- Prefer `error.type` only by default, or redact/truncate `error.message` when exporting.
- Document that OTLP sinks must be trusted.
- Keep generated and runtime behavior in parity.

## Checklist

- [ ] Redact or drop error.message by default
- [ ] Document OTLP sink trust
- [ ] Generator/runtime parity

## Notes

Evidence: parent 470 `review/round-1/merged.md` M33.
