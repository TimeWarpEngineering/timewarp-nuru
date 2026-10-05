# Round 2 — general
**Date:** 2026-10-05
**Scope reviewed:** M1 fix delta (catch-all ClrTypeName) plus completion-28 tests

## Summary

M1 is fixed. `ExtractParameters` now takes `ParameterTypeName` from a handler binding whose source is `BindingSource.Parameter` or `BindingSource.CatchAll` when `SourceName` matches the route parameter. That is the shape `PatternStringExtractor.BuildBindings` writes after `RebindHandlerParameters` (`FromCatchAll` stores the segment name and the handler type) and the shape endpoint `[Parameter(IsCatchAll = true)]` bindings already use. The two new completion-28 cases register `typeof(string[])` for `pack {*files}` at the catch-all slot and one word past it; with the analyzer rebuilt, that file passed 7/7 and the generated `TryGetParameterInfo` emits `parameterType = typeof(string[])`. No new defect in this delta.

## Resolved prior

- M1 — Status: fixed — `ExtractParameters` matches `BindingSource.CatchAll` as well as `BindingSource.Parameter`, and the new `pack {*files}` cases return the `RegisterForType(typeof(string[]))` candidates both on the catch-all slot and one word past it.

## Issues

No new issues.
