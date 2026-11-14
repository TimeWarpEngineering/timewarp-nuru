# Round 2 — merged findings
**Date:** 2026-11-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/telemetry/nuru-telemetry-options.cs:6
- Description: Round 1: the Design named only OtlpEndpoint plus ActivitySources and Meters, described one environment-variable fallback, and ended with an unexplained trust-boundary sentence.
- Suggestion: State the properties the extractor reads and the fallback for each, and drop or explain the trust-boundary sentence.
- Source: general
- Disposition notes: fixed in `0d3c82e3`. Re-checked against `AssignmentFallback` and `ApplyAdd` in `telemetry-options-extractor.cs`. The region lists ServiceName, ServiceVersion, EnableTracing, EnableMetrics, EnableLogging, OtlpEndpoint, and ActivitySources.Add / Meters.Add. Fallbacks match: OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_SERVICE_NAME / OTEL_SERVICE_VERSION then the entry assembly, Enable* default true, and a non-constant ActivitySources / Meters name is not added. The trust-boundary sentence is gone.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-parsing/message-type.cs:7
- Description: Round 1: the Design said Unspecified is treated as Command everywhere. `CreateAnnotations` leaves read-only and idempotent hints unset for Unspecified.
- Suggestion: Record the split. CompiledRoute defaults to Command. The capabilities catalog keeps Unspecified as its own kind, and MCP tool annotations emit no hints for it.
- Source: general
- Disposition notes: fixed in `0d3c82e3` for this claim. `CompiledRoute.MessageType` defaults to Command. `MapMessageTypeToKind` keeps Unspecified. `CreateAnnotations` returns null hints for Unspecified, and the region says MCP clients read the omitted readOnly and idempotent hints as the spec defaults (false), the same effective policy as Command. The help clause added in the same sentence is M3.

### M3 — Severity: bug — Status: open
- File: source/timewarp-nuru-parsing/message-type.cs:8
- Description: The revised Design says help shows Unspecified as `( )`. `HelpEmitter.EmitCommands` writes a Command/Description table and does not read `MessageType`. `RouteHelpEmitter.EmitRouteHelpContent` writes the pattern, description, parameters, options, and examples, and does not read `MessageType` either. The `( )` sentence exists only in the Unspecified XML remarks. A reader who trusts this region would treat the missing marker as a defect.
- Suggestion: Drop the help clause. State that help lists the command and its description and does not render a message-type marker. Leave the Unspecified XML remarks out of this region.
- Source: general
- Disposition notes: fix pass 3 (implement-fix-3) rewrote the help clause: help lists the command and its description and renders no message-type marker. Verified neither `HelpEmitter` nor `RouteHelpEmitter` reads `MessageType`. Awaiting round-3 confirmation.

## Duplicates / conflicts

- None. One reviewer. M1 and M2 carried forward as fixed. M3 is new on the fix delta.
