# Render optional parameters as {name?} in analyzer diagnostics

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding A-4 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

Optional parameters are rewritten to square brackets, so diagnostic locations miss.

`ParameterDefinition.PatternSyntax` renders an optional parameter as `[{Name}]`, not `{name?}`. `EffectivePattern` is built from that. Overlap, duplicate, unreachable, and REPL-default lookups key on `EffectivePattern` and fall through to `Location.None` when the key is the source literal (`deploy {env} {tag?}`). The diagnostic text then shows a pattern the parser would reject.

Evidence: `source/timewarp-nuru-analyzers/generators/models/segment-definition.cs` (`PatternSyntax`), `validation/overlap-validator.cs`. Parent record: `review/analyzers.md` A-4.

## Checklist

- [x] `PatternSyntax` for an optional parameter is `{name?}` (and `{name:type?}` when there is a constraint), not `[name]`
- [x] Duplicate and overlap diagnostics on an optional route squiggle the `Map(...)` string
- [x] The message text is a pattern `PatternParser` accepts
- [x] Add a test with two routes that overlap on `{tag?}`

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~Overlap
```

Expect: The overlap test's diagnostic location is the source route string, and the message contains `{name?}` rather than `[name]`.

## Results

- `ParameterDefinition.PatternSyntax` now renders optional parameters as `{name?}` and
  `{name:type?}` (a trailing `?` already present on `TypeConstraint` is not doubled), so
  `EffectivePattern` and the diagnostic text use syntax `PatternParser` accepts.
- New `validation/route-location-lookup.cs` (`RouteLocationLookup`) anchors overlap (R001/R002/R003),
  REPL-default (R004), reserved json-args (R005), and service diagnostics. Fluent routes resolve
  `OriginalPattern`, then `FullPattern`, then `EffectivePattern`. Endpoint routes resolve
  `EffectivePattern` only, so a fluent `Map` of the attribute literal does not take the span.
- Tests in `tests/timewarp-nuru-tests/generator/generator-30-nuru-r003-overlap.cs`: duplicate
  `deploy {env} {tag?}` (R002 anchors at the source string, message has `{tag?}`, no `[tag]`),
  overlap on `{tag?}` vs `{tag:int?}` (anchored in source, parser-form text), and the
  `PatternSyntax` unit cases.
- Full CI run (`dotnet run tests/ci-tests/run-ci-tests.cs`) passed with no failures.
- Review fix: `Should_anchor_endpoint_nuru_r004_beside_fluent_empty_route` covers `Map("")` beside
  `[NuruRoute("")]` with an optional parameter. The endpoint diagnostic span contains `NuruRoute`.

## Review

- Rounds: 2. Roster: general. Effort: 2.
- Counts (round 2): bug 0 open / 1 fixed / 0 wontfix. Suggestion 0. Nit 0.
- Disposition: clean. No wontfix and no escalation.
- Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

### How to validate

The repo has no `timewarp-nuru-tests.csproj`; tests are runfiles.

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-30-nuru-r003-overlap.cs
```

Expect: All 5 tests pass, including `Should_anchor_nuru_r002_at_optional_route_string_with_parseable_pattern`
and `Should_anchor_nuru_r001_overlap_on_optional_tag`. The diagnostics are anchored at the source route
string and the message contains `{tag?}` / `{tag:int?}`, never `[tag]`.

```bash
dotnet run tests/timewarp-nuru-tests/generator/generator-49-nuru-r004-repl-default-route.cs
```

Expect: All 10 tests pass, including `Should_anchor_endpoint_nuru_r004_beside_fluent_empty_route`.
One NURU_R004 span is the `Map("")` literal and another contains `NuruRoute`.

## Session

- Created: 533403 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)
- Implemented: claude (ganda task work implement oracle) (2026-10-05)
- Review: grok 01a10b26-755d-73c1-b43b-483e7d0b89da (2026-10-05)
- Review oracle: review by implementer-grok (grok, model grok-4.7), session not reported, max-turns 120 — 2026-10-05T08:34:04Z

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
- Implementation review: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`. Outcome clean (M1 fixed).
