# Round 1 — merged findings
**Date:** 2026-11-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.constructors.cs:268
- Description: `IsScalar` strips the nullable `?` only for `FileInfo?`, `DirectoryInfo?`, and `IPAddress?`
  by exact string match. No test reaches that branch (every `[Operation]` property in `routing-44` is
  `required` and non-nullable). `Uri?` and `Version?` in the same scalar list still classify as JSON members.
- Suggestion: compare the unannotated display for every reference type
  (`type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)` when annotated) instead of the per-type
  special case. Add a nullable `[Operation]` case to `routing-44` (`FileInfo?`, `DirectoryInfo?`,
  `IPAddress?`, `Uri?`) that binds when given and stays unset when omitted. Record the extra test run in
  `## Results`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.

## Checked and clean

- `GetTypeConstraintFromClrType` mapping: the three new names match `TypeConversionMap` keys; the nullable
  suffix is handled by the existing `?` branch, so `DirectoryInfo?` maps to `directoryinfo`.
- Repeated / list elements: `BuiltInElementParse` already converts `fileinfo`, `directoryinfo`, and (via
  `GetBuiltInTryConversion`) `ipaddress`, so a `FileInfo[]` element that now resolves to a constraint has an
  emitter.
- Test compile in CI multi-mode: `System.Net` is a global using in both `tests/timewarp-nuru-tests` and
  `tests/ci-tests` props; the file uses `.Map<T>()`, and its route names are prefixed `t591007-`.
- Docs claim "a path the constructor rejects is an invalid-value error": the scalar `fileinfo` /
  `directoryinfo` emitter wraps the constructor in try/catch and calls `WriteArgumentError`.
- Goal restatement matches the task; no misread.
- Recorded gates: `./bin/dev build`, the targeted runfile (7 passed), and `ganda repo audit` exit 0 are in
  `## Results`; the full suite is CI.
