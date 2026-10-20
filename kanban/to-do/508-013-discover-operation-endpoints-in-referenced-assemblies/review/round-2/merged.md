# Round 2 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:1040
- Description: `AttributeLocation` returns the fallback when the attribute syntax is absent or not in this compilation. NURU_H008, route-pattern validation, and NURU_H009 use that location. generator-59 reports NURU_H008 at `app.cs` for a CompilationReference and asserts the generator exception is null.
- Suggestion: none remaining
- Source: general
- Disposition notes: confirmed fixed in round 2 against `11cc4e85`. generator-59 9/9 passed in the review session.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1245
- Description: A record parameter default is taken only from the constructor `new()` calls. An explicit default is copied. A parameter with no default is required only when that constructor does not exist. Otherwise the member is `default!` plus `[DefaultValue]`, so a chained parameterless constructor keeps its value. generator-59 covers `RefRecordQuery` (required `Name`, default `Count`) and `RefNoteQuery` (optional `Title`) for a DLL reference and a CompilationReference.
- Suggestion: none remaining
- Source: general
- Disposition notes: confirmed fixed in round 2 against `11cc4e85`. generator-59 9/9 passed in the review session.

## Duplicates / conflicts

- No new findings. M1 and M2 carry forward from round 1 as fixed. One reviewer.
