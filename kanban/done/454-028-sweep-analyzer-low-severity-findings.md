# Sweep Analyzer Low Severity Findings

Parent: 454 (2026-07-06 full code review). Severity: LOW (batch).

## Description

Low-severity analyzer/generator findings (property-default ToString emission is handled
in 454-012, not here). All paths under `source/timewarp-nuru-analyzers/`:

1. Six duplicated `EscapeString` helpers across emitters (behavior/capabilities/help/
   route-help/completion/version) with INCONSISTENT coverage:
   `generators/emitters/telemetry-emitter.cs:88,101,113` escapes only `"` (not `\`,
   `\n`); `behavior-emitter.cs:443` omits newlines. Consolidate into one correct shared
   helper. (Coordinate with 454-002, which fixes brace escaping in help-emitter.)
2. `validation/handler-validator.cs:164-189` — `ValidateMethodGroupHandler` never checks
   `IsStatic`; an instance method-group misses NURU_H001 (the member-access path at ~:211
   does check it).
3. `extractors/handler-extractor.cs:489-508` — non-generic `ValueTask` not recognized as
   awaitable; a ValueTask-returning handler is treated as a plain value.
4. `validation/service-validator.cs:221,341,365` — UnregisteredService /
   CircularDependency / LifetimeMismatch diagnostics always report at `Location.None`
   though route locations are available.
5. `extractors/endpoint-extractor.cs:137-154` — alias index math assumes one whitespace
   part per group; a multi-word `[NuruRouteGroup("git remote")]` prefix replaces the
   wrong alias segment.
6. Dead/debug code: `extractors/app-extractor.cs` ~:242-352 retains many NURU_DEBUG*
   hidden diagnostics; `interpreter/dsl-interpreter.cs:1715` `IsBuilderType` never called.

## Checklist

- [x] Shared, complete EscapeString helper; all emitters use it (#1, commit 7f752e15)
- [x] IsStatic check in method-group validation path (#2, commit bae8b3ab)
- [x] Non-generic ValueTask recognized (#3, commit bae8b3ab)
- [x] Service-validator diagnostics carry locations (#4, commit 6c7d41c3)
- [x] Multi-word group alias math fixed (+ test routing-29) (#5, commit bae8b3ab)
- [x] Dead debug diagnostics and IsBuilderType removed (#6, commit f59826bd)
- [x] `ganda runfile cache --clear` + run CI tests (1386/1379/0 after each commit)

## Resolution (2026-07-14)

All eight findings fixed across six commits, full CI green (1386/1379/0) after each:
- **#1** `7f752e15` — one shared `EmitterStringUtils.EscapeForStringLiteral`; all 8 emitters
  + telemetry's 3 inline escapes route through it (fixes behavior/telemetry under-escaping).
- **#2** `bae8b3ab` — `ValidateMethodGroupHandler` now checks `IsStatic` (NURU_H001).
- **#3** `bae8b3ab` — non-generic `ValueTask` recognized as awaitable.
- **#4** `6c7d41c3` — NURU050 anchors at the route; NURU055/056 anchor at the service
  registration site (via the LocationInfo added in 454-010 3c).
- **#5** `bae8b3ab` — multi-word group alias replaces the whole group prefix
  (`GroupInfo.GroupPrefixes`); regression test `routing-29-multiword-group-alias`.
- **#6** `f59826bd` — removed all NURU_DEBUG* hidden diagnostics, `DebugRouteFound`,
  `NURU_DEBUG_CONV1`, and the never-called `IsBuilderType`.
- **#7** `f93c01b5` — `DetectClosures` walks `DescendantNodesAndSelf` so a sole-identifier
  handler body (`() => capturedLocal`) is flagged NURU_H002. Analysis showed const-local
  captures are already treated as closures in non-sole positions, so there is no tolerated
  behavior to preserve. Regression test `generator-38-h002-sole-identifier-body`.
- **#8** `f59826bd` — removed the always-true `>= ComputedSpecificity` guard in
  overlap-validator (sorted descending; kept as a documented scoping block).

## Additional item (discovered during 454-004)

7. `validation/handler-validator.cs` — `DetectClosures` walks `lambda.Body.DescendantNodes()`,
   which excludes the body node itself, so `() => capturedLocal` (body IS the identifier)
   is a false NEGATIVE for NURU_H002. Before "fixing" with DescendantNodesAndSelf, analyze
   whether single-identifier constant captures are deliberately tolerated (the DSL
   interpreter inlines resolvable constants); decide + add tests either way.

## Additional item (noted during 454-013 review)

8. `validation/overlap-validator.cs` (~:407) — the `higherRoute.ComputedSpecificity >=
   lowerRoute.ComputedSpecificity` guard is always true (the group is pre-sorted
   descending by ComputedSpecificity). Harmless now that required signatures include
   effectively-required flags (454-013), but it is dead weight — remove or replace with
   a comment stating the sort invariant.
