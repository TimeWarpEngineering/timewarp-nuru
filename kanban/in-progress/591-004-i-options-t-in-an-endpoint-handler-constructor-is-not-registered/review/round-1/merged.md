# Round 1 — merged findings
**Date:** 2026-11-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 2 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs:324
- Description: `EmitOptionsBinding`'s new `configurationExpression` parameter is never passed a non-default value; the mediator registration names its local `configuration` instead. The Design region implies the parameter carries the provider-resolved expression.
- Suggestion: Drop the parameter, or use it from `EmitEndpointOptionsRegistrations`.
- Source: general
- Disposition notes: wontfix (review oracle, 2026-11-19). Internal emitter helper with a harmless default; generated code is correct. Not worth a fix pass on its own; clean up on the next touch of this emitter.

### M2 — Severity: nit — Status: wontfix
- File: kanban/in-progress/591-004-i-options-t-in-an-endpoint-handler-constructor-is-not-registered/task.md:56
- Description: Results call generator-77 a standalone runfile; CI also compiles it into the JARIBU_MULTI assembly, and that run is deferred to CI.
- Suggestion: Reword the Results line.
- Source: general
- Disposition notes: wontfix (review oracle, 2026-11-19). Record wording only; CI runs the multi assembly on the PR and the review disposition in Results notes the correction.

## Duplicates / conflicts

- None (single reviewer).
