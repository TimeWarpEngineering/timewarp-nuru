# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** local uncommitted diff for 470-015 (telemetry-behavior, telemetry-emitter EmitTelemetryCatch, NuruTelemetryOptions, docs, samples, telemetry-02 test)

## Summary

The change drops `error.message` and Activity status description derived from `Exception.Message` on failure, recording `error.type` (exception type name) instead in the library behavior and generated catch twin. OTLP sink trust is documented on `NuruTelemetryOptions.OtlpEndpoint` and in user telemetry docs. Runtime test proves tags/status stay free of a secret-bearing message; a second test asserts emitter source parity. Risk is low: failure telemetry is slightly less diagnostic by design, which matches the security brief. Requirements (redact/drop by default, document trust, generator/runtime parity) are met. Validation tests passed (telemetry-02: 2/2; telemetry-01: 1/1).

## Issues

<!-- None. Zero issues is a valid outcome. -->
