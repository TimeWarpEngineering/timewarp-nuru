# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** f0fb4b38..HEAD completion parameter-info fix (R-3)

## Summary

`TryGetParameterInfo` now reports the positional parameter at the cursor, and the five completion-28 cases match the `__complete` protocol. `EmitCompletionRoutes` passes `routeArgs[1]` as the index and `routeArgs[2..]` as words, so `["__complete", "3", "app", "copy", "from-a"]` is index 3 and `[app, copy, from-a]` (`paramPos` 1, `dest`), `["__complete", "2", "app", "move"]` is the first parameter, and `["__complete", "2", "app", "github"]` does not satisfy the `git` token boundary. Longer literal prefixes are emitted first, which is what keeps `git push {remote}` ahead of a shorter `git` route whose later slot would otherwise share that `paramPos`. `SymbolDisplayFormat.FullyQualifiedFormat` (Roslyn 5.6, `UseSpecialTypes`, no NRT modifier) produces `int?`, `string`, and `global::ShipTarget?`, not `global::System.Int32?` or `global::System.String?`; `TrimEnd('?')` yields a `typeof` that matches `RegisterForType` for those forms, and a nested `Dictionary<string, int?>` is left intact because it ends in `>`.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/completion-data-extractor.cs:104
- Description: `ClrTypeName` is taken only from `BindingSource.Parameter`. After `RouteDefinitionBuilder.RebindHandlerParameters`, `PatternStringExtractor.BuildBindings` turns a catch-all segment into `ParameterBinding.FromCatchAll` (`BindingSource.CatchAll`, `SourceName` = the segment name). Endpoint `[Parameter(IsCatchAll = true)]` bindings are created as `FromCatchAll` directly. The route parameter is still emitted: `IsCatchAll` makes the position test `paramPos >= n`, and `parameterName` is set, so `TryGetParameterInfo` returns true. `parameterType` is null, `EmitTypeOf` emits `null`, and `DynamicCompletionHandler` never calls `GetSourceForType`. `RegisterForParameter` still works; `RegisterForType` does not, including for `string[]` and user array/element types. Name and position matching were updated for catch-all in this change; type lookup was not.
- Suggestion: Treat `BindingSource.CatchAll` like `BindingSource.Parameter` when resolving `ClrTypeName` (same `SourceName` comparison). Add a completion-28 case such as `pack {*files}` with `RegisterForType(typeof(string[]))` (and, if useful, a user element type) so a cursor at or past the catch-all slot returns that source.
- Status: open
