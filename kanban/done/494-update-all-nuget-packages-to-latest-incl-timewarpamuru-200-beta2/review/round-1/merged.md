# Round 1 — merged findings
**Date:** 2026-10-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: Directory.Packages.props:26
- Description: Shipped Nuru packages now depend on prerelease Amuru 2.0.0-beta.2. A stable Nuru 3.0.0 pack would hit NU5104.
- Suggestion: Record a release gate.
- Source: general
- Disposition notes: No code change on this task. Steven asked for pre-releases. Nuru is still 3.0.0-beta.x, so nothing breaks today. The gate is recorded in task 494 Notes for the 3.0.0 release. Decided by: review oracle.

## Duplicates / conflicts

- None (single reviewer).
