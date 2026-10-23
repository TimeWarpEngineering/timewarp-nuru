# Round 1 — merged findings
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
  are indented two spaces. The rest of the type-level `///` block starts at column 0.
- Suggestion: Remove the two leading spaces.
- Source: general
- Disposition notes: wontfix (review oracle, 2026-10-23). This is whitespace inside a doc comment only.
  The build and `dotnet format` accept it, and behavior and published docs are unchanged. A full
  implementer fix pass plus a second review round costs more than the nit is worth. Fix it in passing
  the next time this file is touched.

## Duplicates / conflicts

- None (single reviewer).
