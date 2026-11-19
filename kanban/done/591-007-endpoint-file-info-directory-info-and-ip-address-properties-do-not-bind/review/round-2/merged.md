# Round 2 — merged findings
**Date:** 2026-11-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.constructors.cs:277
- Description: (round 1) `IsScalar` stripped the nullable `?` only for `FileInfo?`, `DirectoryInfo?`, and
  `IPAddress?`; no test reached that branch; `Uri?` and `Version?` stayed JSON members.
- Suggestion: (round 1) compare the unannotated display for every reference type; add nullable
  `[Operation]` coverage to `routing-44`.
- Source: general
- Disposition notes: fixed in 038fa1e3. `IsScalar` uses `WithNullableAnnotation(NotAnnotated)` for any
  annotated reference type. `routing-44` `t591007-optional` binds `FileInfo?`, `DirectoryInfo?`,
  `IPAddress?`, `Uri?` and leaves them unset when omitted; recorded 9 passed.

## New findings

None.

## Duplicates / conflicts

- None. Single reviewer.
