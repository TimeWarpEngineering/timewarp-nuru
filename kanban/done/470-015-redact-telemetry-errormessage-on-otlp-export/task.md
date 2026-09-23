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

- [x] Redact or drop error.message by default
- [x] Document OTLP sink trust
- [x] Generator/runtime parity

## Notes

Evidence: parent 470 `review/round-1/merged.md` M33.

Implementation review (effort 1, general): `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`. Session: review-oracle (ganda task-work tw-implementation-review).

## Results

Dropped `error.message` and Activity status detail from exception text by default. Failure paths now set `error.type` (exception type name) only:

- Runtime: `TelemetryBehavior` (`source/timewarp-nuru/telemetry/telemetry-behavior.cs`)
- Generated twin: `TelemetryEmitter.EmitTelemetryCatch` (`source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs`) — kept in parity even though the catch emitter is not yet wired into the interceptor

Documented OTLP sink trust on `NuruTelemetryOptions.OtlpEndpoint` and in `documentation/user/features/telemetry.md` (new **OTLP sink trust** section). Updated pipeline docs and fluent/endpoint telemetry samples to match. Added `tests/timewarp-nuru-tests/telemetry/telemetry-02-error-message-redaction.cs`.

### Review disposition

- **Outcome:** clean
- **Rounds:** 1 · **Effort:** 1 · **Roster:** general
- **Final counts:** bug/suggestion/nit all 0 open / 0 fixed / 0 wontfix
- **Artifacts:** `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Smoke:** From the claim worktree:

```bash
dotnet -- tests/timewarp-nuru-tests/telemetry/telemetry-02-error-message-redaction.cs
dotnet -- tests/timewarp-nuru-tests/telemetry/telemetry-01-use-telemetry-options.cs
```

**Expect:** telemetry-02 reports 2 passed (error.type only / no status description; emitter catch twin free of `error.message`); telemetry-01 reports 1 passed.
