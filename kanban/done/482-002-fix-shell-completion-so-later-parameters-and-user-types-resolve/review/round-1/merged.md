# Round 1 — merged findings
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
- Description: `ClrTypeName` is taken only from `BindingSource.Parameter`. After `RouteDefinitionBuilder.RebindHandlerParameters`, `PatternStringExtractor.BuildBindings` stores a catch-all as `BindingSource.CatchAll`. Endpoint `[Parameter(IsCatchAll = true)]` bindings are `FromCatchAll` as well. `TryGetParameterInfo` still returns the catch-all name and matches `paramPos >= position`, but `parameterType` is null, so `RegisterForType` never runs.
- Suggestion: Treat `BindingSource.CatchAll` like `BindingSource.Parameter` when resolving `ClrTypeName` (same `SourceName` comparison). Add a completion-28 case for `pack {*files}` with `RegisterForType(typeof(string[]))` at the catch-all slot and one word past it.
- Source: general
- Disposition notes: Fixed on this task. `ExtractParameters` now matches `BindingSource.CatchAll` as well as `BindingSource.Parameter`. completion-28 covers `pack {*files}` with `RegisterForType(typeof(string[]))` at the catch-all slot and one word past it. After rebuilding the analyzer and clearing the runfile cache, all 7 tests passed. Generated `TryGetParameterInfo` emits `parameterType = typeof(string[])`.

## Duplicates / conflicts

- Single reviewer. No overlap.
