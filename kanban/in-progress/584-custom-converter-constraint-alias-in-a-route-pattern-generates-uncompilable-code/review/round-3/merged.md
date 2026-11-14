# Round 3 — merged findings
**Date:** 2026-11-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.unresolved-constraints.cs:50
- Description: Round 1: an unresolved constraint still emitted CS0103 or CS0029 beside `NURU_S010`. Fixed in `d3a444e1` and confirmed in round 2. This delta does not reopen it. No command was run on this round.
- Suggestion: Done in `d3a444e1`.
- Source: general
- Disposition notes: Fixed in round 2. Carried forward. Not reopened by `280434b0`.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/validation/unknown-type-constraint-validator.cs:136
- Description: Round 2: a typed catch-all or repeated option of `uri`, `FileInfo`, or `DirectoryInfo` reported `NURU_S010` because `IsKnownListElement` only accepted `GetBuiltInTryConversion`. `280434b0` uses `TypeConversionMap.IsBuiltInType` (`unknown-type-constraint-validator.cs:136`), which is true for those three (`type-conversion-map.cs:167-170`). `BuiltInElementParse.TryEmit` (`built-in-element-parse.cs:67-83`) emits `Uri.TryCreate` and the `FileInfo` / `DirectoryInfo` constructor. The route matcher calls it from `EmitBuiltInArrayConversion` for a catch-all and a repeated option (`route-matcher-emitter.helpers.cs:214-222`). The `--json-args` list path calls it from `EmitAssignListFromStrings` (`json-args-emitter.assign.cs:235-252`). generator-76 `Should_convert_uri_fileinfo_and_directoryinfo_lists` requires no `NURU_S010` and no compile errors. routing-16 `Should_bind_uri_fileinfo_and_directoryinfo_lists` checks `FileInfo.Name`, `Uri.Host`, and `DirectoryInfo.Name`. `glossary.md` defines built-in type. ## Results records build, test, and audit for this pass. No command was run on this round.
- Suggestion: Done in `280434b0`.
- Source: general
- Disposition notes: Fixed. Re-read the validator, `BuiltInElementParse`, both list emitters, generator-76, routing-16, and `glossary.md` against the round-2 description.

## Duplicates / conflicts

- Single reviewer. M1 carried forward as fixed. M2 is fixed on this delta. No new findings.
