# Round 1 — merged findings
**Date:** 2026-11-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/telemetry/nuru-telemetry-options.cs:9
- Description: The new Design says a value TelemetryOptionsExtractor cannot read reports NURU_T001 and the environment variable applies instead, and it names only OtlpEndpoint plus ActivitySources and Meters adds as the compile-time contract. The extractor also reads ServiceName, ServiceVersion, and EnableTracing, EnableMetrics, and EnableLogging. Fallbacks are not one environment variable: OtlpEndpoint uses OTEL_EXPORTER_OTLP_ENDPOINT, ServiceName and ServiceVersion use the OTEL_* variable and then the assembly, the Enable* flags use the default true, and a non-constant ActivitySources.Add or Meters.Add is dropped (`The name is not added` in `telemetry-options-extractor.cs`). The same region ends with "OTLP sinks are a trust boundary" and does not say what that requires.
- Suggestion: State the properties the extractor reads and the fallback for each (environment variable, assembly default, default true, or the name is not added). Say what the trust-boundary constraint requires, or drop that sentence.
- Source: general
- Disposition notes: fixed in fix pass 2 (2026-11-14). Design now lists every member the extractor reads and the NURU_T001 fallback for each (from `AssignmentFallback` and the Add path); the unexplained trust-boundary sentence is dropped.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-parsing/message-type.cs:7
- Description: The new Design says Unspecified is treated as Command everywhere, so an unclassified route is confirmed and never auto-retried. `NuruMcpToolCatalog.CreateAnnotations` does not do that: Query, IdempotentCommand, and Command set read-only and idempotent hints, and Unspecified takes the default branch that leaves both hints unset. The catalog remarks say "Unspecified: no hints." A reader who trusts this region would treat that path as a defect.
- Suggestion: Record the split. CompiledRoute defaults to Command. The capabilities catalog keeps Unspecified as its own kind, and MCP tool annotations emit no hints for it. If the XML "treat as Command" rule is the intended agent policy, put the catalog mismatch in Open Questions instead of stating "everywhere."
- Source: general
- Disposition notes: fixed in fix pass 2 (2026-11-14). Design records the split: CompiledRoute defaults to Command, help shows ( ), the capabilities catalog keeps Unspecified as its own EndpointKind, and MCP annotations set no readOnly/idempotent hints, which MCP clients read as spec defaults (false), the same as Command. No Open Question needed: the effective agent behavior matches.

## Duplicates / conflicts

- None. One reviewer.
