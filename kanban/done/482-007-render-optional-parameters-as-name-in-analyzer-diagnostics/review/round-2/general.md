# Round 2 — general
**Date:** 2026-10-05
**Scope reviewed:** M1 fix in `route-location-lookup.cs` and `generator-49-nuru-r004-repl-default-route.cs`. Re-checked optional `{name?}` rendering against the round-1 scope.

## Summary

`HandlerKind.Command` routes now resolve only `EffectivePattern`, which is the key `ExtractEndpointWithLocation` stores. Fluent routes still try `OriginalPattern`, then `FullPattern`, then `EffectivePattern`. The new NURU_R004 test failed on the previous lookup (one diagnostic, span `""`, both routes generated) and passes after the change (attribute span and `Map("")` span). Overlap tests from 482-007 still pass. No new issues on the fix delta.

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/validation/route-location-lookup.cs:22
- Description: Endpoint diagnostics no longer take a fluent `Map` location just because `OriginalPattern` matches that literal.
- Suggestion: Applied as suggested in round 1.
- Status: fixed
