# Diagnose invalid route patterns instead of emitting a fallback literal

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding A-2 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

Three parser errors never become diagnostics, and the route is still emitted.

`MapParseErrorToDiagnostic` returns null for `InvalidIdentifierError`, `InvalidModifierCombinationError`, and `AdjacentParametersError`. There is no descriptor for them. On failure, `PatternStringExtractor.ExtractSegmentsWithErrors` still returns a single `LiteralDefinition` of the raw pattern, and that literal is registered. Runtime `PatternParser` rejects the same input.

Evidence: `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` (`MapParseErrorToDiagnostic`), `source/timewarp-nuru-analyzers/generators/extractors/pattern-string-extractor.cs`, `source/timewarp-nuru-parsing/parsing/parser/parse-error.cs`. Parent record: `review/analyzers.md` A-2.

## Checklist

- [ ] `{a}{b}`, `{*name?}`, and `{my-param}` each report a NURU diagnostic with an id, category, and a user-facing message
- [ ] Those patterns do not emit a fallback literal route
- [ ] Add an asserting test for each of the three errors
- [ ] A pattern that already has a diagnostic (unbalanced braces, bad option) still reports that diagnostic and still does not emit a matching route

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~InvalidPattern
```

Expect: Each new test expects a diagnostic and no generated route for `{a}{b}`, `{*name?}`, and `{my-param}`.

## Session

- Created: 532275 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
