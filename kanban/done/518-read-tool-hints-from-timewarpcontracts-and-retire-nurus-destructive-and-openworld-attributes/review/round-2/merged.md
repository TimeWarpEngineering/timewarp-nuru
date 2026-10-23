# Round 2 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru-analyzers/generators/models/route-definition.cs:27
- Description: The rewritten `Destructive`, `OpenWorld`, and `Title` `<param>` doc lines (27–30, 41–45)
  are indented two spaces. The rest of the type-level `///` block starts at column 0. Still true at
  HEAD; the 518 sources are unchanged since round 1.
- Suggestion: Remove the two leading spaces.
- Source: general (round 1); re-verified round 2
- Disposition notes: wontfix (review oracle, 2026-10-23, round 1; confirmed round 2). This is
  whitespace inside a doc comment only. The build and `dotnet format` accept it, and behavior and
  published docs are unchanged. A fix pass costs more than the nit is worth. Fix it in passing the
  next time this file is touched. Round 2 does not reopen it.

## Duplicates / conflicts

- None. Round 2 adds no new ids.
