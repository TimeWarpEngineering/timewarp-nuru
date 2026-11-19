# Round 3 — merged findings
**Date:** 2026-11-19
**Sources:** general
**Scope:** fix delta `6b29609c..HEAD`; carries M1–M6.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 4 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

- M1 (bug), M2 (bug), M3 (bug), M4 (suggestion), M5 (nit) — fixed in round 2; unchanged by this delta.

### M6 — Severity: bug — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:349
- Description: `GetCommand` and `RunCommand` declared two `[Parameter]` properties without `Order`.
- Source: general (round 2)
- Disposition notes: implement-fix-3 (`be4635e5`) sets `Order = 0` / `Order = 1` on both, and on the getting-started
  add command, the readme's second add command, and the endpoint guide's required/optional pair. Verified in round 3.

## Issues

None new.

## Duplicates / conflicts

- Single reviewer; none.
