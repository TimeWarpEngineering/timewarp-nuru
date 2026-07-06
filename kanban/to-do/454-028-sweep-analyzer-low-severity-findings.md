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

- [ ] Shared, complete EscapeString helper; all emitters use it
- [ ] IsStatic check in method-group validation path
- [ ] Non-generic ValueTask recognized
- [ ] Service-validator diagnostics carry locations
- [ ] Multi-word group alias math fixed (+ test)
- [ ] Dead debug diagnostics and IsBuilderType removed
- [ ] `ganda runfile cache --clear` + run CI tests

## Additional item (discovered during 454-004)

7. `validation/handler-validator.cs` — `DetectClosures` walks `lambda.Body.DescendantNodes()`,
   which excludes the body node itself, so `() => capturedLocal` (body IS the identifier)
   is a false NEGATIVE for NURU_H002. Before "fixing" with DescendantNodesAndSelf, analyze
   whether single-identifier constant captures are deliberately tolerated (the DSL
   interpreter inlines resolvable constants); decide + add tests either way.
