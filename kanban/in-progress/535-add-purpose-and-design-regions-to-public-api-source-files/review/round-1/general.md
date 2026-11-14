# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** `81633070` comment-only Design and Open Questions regions on public-API files under `source/`, checked against the groomed task and `## Goal (implementer restatement)`.

## Summary

The change adds `#region Design` after Purpose and before the namespace on the public builder and extension files, and on the rest of the public surface where a decision exists. Open Questions use the skill's `Q# (date, author)` form, the diff adds no C# deletions, and the recorded build, test, and audit results cover the change. The restatement matches the groomed task (Design only; Purpose already landed in 580). Two new Design regions state a fallback or a default that the code does not follow, so a later reader would treat real behavior as a defect.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru/telemetry/nuru-telemetry-options.cs:9
- Description: The new Design says a value TelemetryOptionsExtractor cannot read reports NURU_T001 and the environment variable applies instead, and it names only OtlpEndpoint plus ActivitySources and Meters adds as the compile-time contract. The extractor also reads ServiceName, ServiceVersion, and EnableTracing, EnableMetrics, and EnableLogging. Fallbacks are not one environment variable: OtlpEndpoint uses OTEL_EXPORTER_OTLP_ENDPOINT, ServiceName and ServiceVersion use the OTEL_* variable and then the assembly, the Enable* flags use the default true, and a non-constant ActivitySources.Add or Meters.Add is dropped (`The name is not added` in `telemetry-options-extractor.cs`). The same region ends with "OTLP sinks are a trust boundary" and does not say what that requires.
- Suggestion: State the properties the extractor reads and the fallback for each (environment variable, assembly default, default true, or the name is not added). Say what the trust-boundary constraint requires, or drop that sentence.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-parsing/message-type.cs:7
- Description: The new Design says Unspecified is treated as Command everywhere, so an unclassified route is confirmed and never auto-retried. `NuruMcpToolCatalog.CreateAnnotations` does not do that: Query, IdempotentCommand, and Command set read-only and idempotent hints, and Unspecified takes the default branch that leaves both hints unset. The catalog remarks say "Unspecified: no hints." A reader who trusts this region would treat that path as a defect.
- Suggestion: Record the split. CompiledRoute defaults to Command. The capabilities catalog keeps Unspecified as its own kind, and MCP tool annotations emit no hints for it. If the XML "treat as Command" rule is the intended agent policy, put the catalog mismatch in Open Questions instead of stating "everywhere."
- Status: open
