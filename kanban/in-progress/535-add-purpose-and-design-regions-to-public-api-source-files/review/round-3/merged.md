# Round 3 — merged findings
**Date:** 2026-11-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/telemetry/nuru-telemetry-options.cs:6
- Description: Round 1: the Design named only OtlpEndpoint plus ActivitySources and Meters, described one environment-variable fallback, and ended with an unexplained trust-boundary sentence.
- Suggestion: State the properties the extractor reads and the fallback for each, and drop or explain the trust-boundary sentence.
- Source: general
- Disposition notes: fixed in `0d3c82e3`. Unchanged in `17d2f8f7`. Round 2 re-checked the region against `AssignmentFallback` and `ApplyAdd`.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-parsing/message-type.cs:7
- Description: Round 1: the Design said Unspecified is treated as Command everywhere. `CreateAnnotations` leaves read-only and idempotent hints unset for Unspecified.
- Suggestion: Record the split. CompiledRoute defaults to Command. The capabilities catalog keeps Unspecified as its own kind, and MCP tool annotations emit no hints for it.
- Source: general
- Disposition notes: fixed in `0d3c82e3` for the CompiledRoute default, the capabilities catalog, and MCP annotations. Those clauses are unchanged in `17d2f8f7`. `CompiledRoute.MessageType` defaults to Command. `MapMessageTypeToKind` keeps Unspecified. `CreateAnnotations` returns null hints for Unspecified and `(false, false)` for Command. The help clause that round 2 split out is M3.

### M3 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-parsing/message-type.cs:8
- Description: The revised Design said help shows Unspecified as `( )`. Help emitters do not read `MessageType`. The `( )` sentence exists only in the Unspecified XML remarks.
- Suggestion: Drop the help clause. State that help lists the command and its description and does not render a message-type marker. Leave the Unspecified XML remarks out of this region.
- Source: general
- Disposition notes: fixed in `17d2f8f7`. The region now says help lists the command and its description and renders no message-type marker for any value. `HelpEmitter.EmitCommands` writes Command and Description columns. `RouteHelpEmitter.EmitRouteHelpContent` writes the pattern, description, parameters, options, and examples. Neither file mentions `MessageType`. The XML remarks are not cited.

## Duplicates / conflicts

- None. One reviewer. M1, M2, and M3 carried forward. No new findings on the fix delta.
