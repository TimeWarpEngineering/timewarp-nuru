# Round 2 — merged findings
**Date:** 2026-10-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/completion-data-extractor.cs:104
- Description: `ClrTypeName` was taken only from `BindingSource.Parameter`. After rebind, a catch-all handler parameter is `BindingSource.CatchAll`, so `parameterType` was null and `RegisterForType` did not run.
- Suggestion: Match `BindingSource.CatchAll` as well as `BindingSource.Parameter`, and cover `pack {*files}` with `RegisterForType(typeof(string[]))`.
- Source: general
- Disposition notes: Fixed on this task. `ExtractParameters` matches both binding sources. completion-28 passed 7/7 after an analyzer rebuild and runfile cache clear. Generated `TryGetParameterInfo` emits `parameterType = typeof(string[])` for the catch-all slot and for a later word.

## Resolved prior

- M1 carried from round 1. Re-review confirmed the fix. No new findings.

## Duplicates / conflicts

- Single reviewer. No overlap.
