# Round 2 — merged findings
**Date:** 2026-11-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.unresolved-constraints.cs:50
- Description: Round 1: an unresolved constraint still emitted CS0103 or CS0029 beside `NURU_S010`. `d3a444e1` declares the bound handler type with `default!` for a positional, optional positional, value option, repeated option, and catch-all (`route-matcher-emitter.unresolved-constraints.cs`), and the `--json-args` binder assigns `default!` instead of the raw string (`json-args-emitter.assign.cs:285-288`). The declared name matches `BuildArgumentListFromRoute`. generator-76 requires zero compile errors for the unknown-constraint and non-constant-alias cases, and the unknown case covers those five routes. ## Results records build, test, and audit for this pass. No command was run on this round.
- Suggestion: Done in `d3a444e1`.
- Source: general
- Disposition notes: Fixed. Re-read the emitters, the validator, and generator-76 against the round-1 description.

### M2 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/validation/unknown-type-constraint-validator.cs:128
- Description: A typed catch-all or repeated option of `uri`, `FileInfo`, or `DirectoryInfo` reports `NURU_S010`, and the message says the constraint is not a built-in type. `IsKnownListElement` only treats `GetBuiltInTryConversion` hits as built-in list elements. Those three return null there, while `IsBuiltInType` is true and the scalar emitters convert them. The array emitters have no constructor path for them, so flipping the known-check alone would emit `string[]` and bring back CS0029 for a `FileInfo[]`, `Uri[]`, or `DirectoryInfo[]` handler. generator-76 does not cover these list forms. See `review/round-2/general.md`.
- Suggestion: Convert those three per element on the catch-all and repeated-option paths, treat them as known, and lock that in generator-76 (no `NURU_S010`, generated compilation has no errors). Define built-in type in `glossary.md` as the `TypeConversionMap` set, including Uri, FileInfo, and DirectoryInfo.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. M1 carried forward as fixed. M2 is new on this delta.
