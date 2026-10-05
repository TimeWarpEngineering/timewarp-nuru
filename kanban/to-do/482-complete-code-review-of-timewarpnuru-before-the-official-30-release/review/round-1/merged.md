# Round 1 — merged findings
**Date:** 2026-10-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.configuration.cs:50 (also :78, :126; logging/nuru-logging-extensions.cs:21)
- Description: public XML examples still call removed `CreateBuilder([])`; R-7 incomplete.
- Suggestion: use `NuruApp.CreateBuilder()`.
- Source: general
- Disposition notes: fixed on this task by the review oracle; `dotnet build source/timewarp-nuru` 0 warnings / 0 errors. `grep 'CreateBuilder(\(args\|\[\]\)' source --include=*.cs` now hits nothing.

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:392, generators/extractors/app-extractor.cs:326, .agent/local/nuru-specific.md
- Description: stale `CreateBuilder([])` / `CreateBuilder(args)` in comments and agent instructions.
- Suggestion: update to `CreateBuilder()`.
- Source: general
- Disposition notes: fixed on this task. Generator test inputs (string literals in `tests/timewarp-nuru-tests/generator/`) intentionally untouched.

## Duplicates / conflicts

- None.
