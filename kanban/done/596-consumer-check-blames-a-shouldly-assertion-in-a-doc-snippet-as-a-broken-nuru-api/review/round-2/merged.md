# Round 2 — merged findings
**Date:** 2026-11-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: skills/tw-nuru-verify/SKILL.md:105
- Description: Round 1 asked the skill's `broken` bullet to match `nuru_errors`. The bullet now does. Host extension points still cover `CS1061` and `CS0117`. Built-in keyword receivers (same list as `nuru-consumer-check.sh:325`, optional `?`/`[]`) cover `CS1061` only. `object`, `dynamic`, and package types stay blamed.
- Suggestion: Already applied. No further change.
- Source: general
- Disposition notes: confirmed fixed in round 2 by reading `skills/tw-nuru-verify/SKILL.md` against `nuru_errors`. No new findings on the fix delta.

## Duplicates / conflicts

- One reviewer. M1 carried forward from round 1. Nothing new to collapse.
