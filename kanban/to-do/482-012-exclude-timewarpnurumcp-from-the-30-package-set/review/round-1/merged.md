# Round 1 — merged findings
**Date:** 2026-10-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: documentation/user/tools/overview.md:33
- Description: The tools overview banner says `TimeWarp.Nuru.Mcp` is not part of 3.0, but the highlights row and the Installation section on the same edited page still present `dotnet tool install --global TimeWarp.Nuru.Mcp` as the current install. The root readme in this commit already limits that command to older prereleases.
- Suggestion: Qualify the Installation subsection as not part of 3.0 / older prereleases only, and mark the highlights row as frozen and not part of 3.0.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. No overlaps.
