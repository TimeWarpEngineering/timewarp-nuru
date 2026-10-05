# Diagnose invalid route patterns instead of emitting a fallback literal

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding A-2 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

Three parser errors never become diagnostics, and the route is still emitted.

`MapParseErrorToDiagnostic` returns null for `InvalidIdentifierError`, `InvalidModifierCombinationError`, and `AdjacentParametersError`. There is no descriptor for them. On failure, `PatternStringExtractor.ExtractSegmentsWithErrors` still returns a single `LiteralDefinition` of the raw pattern, and that literal is registered. Runtime `PatternParser` rejects the same input.

Evidence: `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` (`MapParseErrorToDiagnostic`), `source/timewarp-nuru-analyzers/generators/extractors/pattern-string-extractor.cs`, `source/timewarp-nuru-parsing/parsing/parser/parse-error.cs`. Parent record: `review/analyzers.md` A-2.

## Checklist

- [x] `{a}{b}`, `{*name?}`, and `{my-param}` each report a NURU diagnostic with an id, category, and a user-facing message
- [x] Those patterns do not emit a fallback literal route
- [x] Add an asserting test for each of the three errors
- [x] A pattern that already has a diagnostic (unbalanced braces, bad option) still reports that diagnostic and still does not emit a matching route
- [x] Review disposition recorded (clean; M1 and M2 fixed)

## How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-53-invalid-route-patterns.cs
```

Expect: Each new test expects a diagnostic and no generated route for `{a}{b}`, `{*name?}`, and `{my-param}`.

## Results

- New descriptors `NURU_P008` (invalid identifier), `NURU_P009` (catch-all + optional), `NURU_P010`
  (adjacent parameters), category `RoutePattern.Syntax`, severity Error; mapped in
  `DslInterpreter.MapParseErrorToDiagnostic` and listed in `AnalyzerReleases.Unshipped.md`.
- `PatternStringExtractor.ExtractSegmentsWithErrors` no longer returns a fallback `LiteralDefinition`
  of the raw pattern on failure; it returns no segments.
- `IrAppBuilder.Map` / `IrGroupBuilder.Map` pass `parseResult.Success` to `IrRouteBuilder`; `Done()`
  skips `RegisterRoute` for an invalid pattern, so no route (and no empty default route) is emitted.
  This also applies to patterns that already had a diagnostic (e.g. `greet {name`).
- Side effect: `[NuruRoute("{a}{b}")]` no longer passes `ValidateRoutePattern` as a single literal;
  it now reports `NURU_A001` like any other non-literal endpoint pattern.
- Test: `tests/timewarp-nuru-tests/generator/generator-53-invalid-route-patterns.cs` (5 tests, all
  pass). Excluded from the CI multi-assembly like other Roslyn-hosted generator tests (CS0433).
- `dotnet run tests/ci-tests/run-ci-tests.cs`: 1925 passed, 0 failed (before review). Review then wired `generator-53` into that runner's standalone phase.

### Review

- Rounds: 2. Roster: general. Effort: 2.
- Round 1: 2 bugs (M1, M2), 0 suggestions, 0 nits. Both fixed on this task. Round 2: no new findings.
- Final counts: bug 0 open / 2 fixed / 0 wontfix. Disposition: **clean**. No wontfix. No escalation.
- M1: `generator-53` was listed in `CiTestExcludes` but missing from `tests/ci-tests/run-ci-tests.cs` `standaloneTests`, so CI did not run it. It is now on that list (`generator-28..53`).
- M2: `NURU_P010` rendered doubled braces. Roslyn 5.6 `GetMessage` leaves `{{` unchanged when there are no format arguments. The descriptor now uses `'{0}'` and `'{1}'` with `"{a} {b}"` and `"{a}{b}"`.
- Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-53-invalid-route-patterns.cs
```

Expect: 5 tests pass. `{a}{b}` → `NURU_P010`, `{*name?}` → `NURU_P009`, `{my-param}` → `NURU_P008`,
each Error / `RoutePattern.Syntax`, with no `// Route:` comment emitted for the bad pattern while the
control route `greet {name}` is emitted. The `NURU_P010` message contains `(e.g., '{a} {b}' rather than '{a}{b}')`.
`greet {name` and `deploy --{x}` still report a `NURU_P` diagnostic and emit no route.

## Session

- Created: 532275 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)
- Implemented: claude (ganda task work implement oracle) (2026-10-05)
- Review: grok 01a10ae6-22a2-73a2-8d03-84b40635c7d8 (2026-10-05)
- Review oracle: review by implementer-grok (grok, model grok-4.7), session not reported, max-turns 120 — 2026-10-05T07:25:43Z

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
- Implementation review: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`. Outcome clean (2 rounds, general, effort 2). M1 and M2 fixed.
