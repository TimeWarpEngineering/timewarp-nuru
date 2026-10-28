# Round 1 — general
**Date:** 2026-10-28
**Scope reviewed:** branch vs `master` (merge-base `8cd9d502`). `NuruOtlpEndpoint`, telemetry emitter, options and user docs, glossary, generator-43, telemetry-04, standalone CI wiring. Checked against the task and `## Goal (implementer restatement)`.

## Summary

`UseTelemetry()` no longer assigns the collector base URL to `o.Endpoint`. Generated setup calls `NuruOtlpEndpoint.Configure` per signal. For a code-set `OtlpEndpoint`, HTTP gets `/v1/{signal}` unless that path is already present, and gRPC is unchanged. With only `OTEL_EXPORTER_OTLP_ENDPOINT`, `Endpoint` stays unset so the SDK keeps its append. Per-signal `OTEL_EXPORTER_OTLP_{SIGNAL}_ENDPOINT` and `_PROTOCOL` are applied by Nuru because OpenTelemetry 1.19.1's per-signal `AddOtlpExporter` reads the base env vars only (`OtlpExporterOptionsConfigurationType.Default`); `UseOtlpExporter` is what reads the signal-specific vars. Setting `Endpoint` clears `AppendSignalPathToEndpoint`, which is why the old `o.Endpoint = base` posted to `/`. gRPC still appends its service path in the export client. `FlushTelemetryAsync` still returns immediately when no provider exists. The restatement matches the task; the SDK premise in the task (that `AddOtlpExporter` honors signal endpoint overrides) is false, and the implementation compensates. That is recorded in Results, not a misread of the goal.

`ForSignal` matches `OtlpExporterOptionsExtensions.AppendPathIfNotPresent` for ordinary base URLs (trailing slash, path prefix, path already ending in the signal). Generator tests lock the emitted `Configure` calls for the code and env paths. Runtime tests cover HTTP and gRPC resolution, code-over-env, and an HTTP capture of `/v1/traces`, `/v1/metrics`, `/v1/logs` plus a traces override. telemetry-04 is excluded from the multi-mode assembly and listed in the standalone phase. Sandbox denied `dotnet run` (runfile cache and NuGet mutex outside the workspace); the SDK claims were checked by decompiling `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.19.1.

## Issues

No issues.
