# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commit 8950b15f — optional `{name?}` rendering and route location lookup

## Summary

`ParameterDefinition.PatternSyntax` now renders optional parameters as `{name?}` and `{name:type?}`, and the new overlap tests anchor NURU_R002 / NURU_R001 text on the `Map(...)` string. That part matches A-4. `RouteLocationLookup` tries `OriginalPattern`, then `FullPattern`, then `EffectivePattern`. Endpoint locations are stored under `EffectivePattern`, so the source-form keys can select a different route's `Map` literal.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/validation/route-location-lookup.cs:18
- Description: Fluent `Map` locations are keyed by the string literal (`GetFluentRouteLocation`). Endpoint locations are keyed by `EffectivePattern` (`ExtractEndpointWithLocation`), because `[NuruRoute]` is only a single literal and property segments are appended. `Find` tries `OriginalPattern` first. An endpoint `[NuruRoute("")]` with an optional parameter has `OriginalPattern` `""` and `EffectivePattern` `{name?}`. A sibling `.Map("")` owns the `""` key, so NURU_R004 (and the same lookup in overlap, unreachable, and service checks) squiggles the fluent literal instead of the attribute. The same steal happens for `[NuruRoute("list")]` plus `{filter?}` against `.Map("list")`. Before this change, overlap / REPL / service lookups used `EffectivePattern` only, which is the endpoint key.
- Suggestion: For `HandlerKind.Command` (endpoint routes), resolve only `EffectivePattern`. For fluent routes (`Delegate` and `Method`), keep source-form keys first (`OriginalPattern`, then `FullPattern`, then `EffectivePattern`) so grouped and normalized patterns still hit the `Map` literal. Add a generator test with `.Map("")` and `[NuruRoute("")]` plus an optional parameter under `AutoStartWhenEmpty`, and assert one NURU_R004 span is the attribute.
- Status: open
