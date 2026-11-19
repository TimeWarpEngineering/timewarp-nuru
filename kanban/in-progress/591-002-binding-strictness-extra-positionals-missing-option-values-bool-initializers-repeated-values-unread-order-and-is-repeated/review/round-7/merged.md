# Round 7 — merged findings
**Date:** 2026-11-20
**Sources:** general
**Scope:** fix pass `1d29674c..HEAD`. Carries M1–M12.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 7 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 3 | 0 |

## Resolved prior

- M1–M10: fixed in earlier rounds.
- M11 (bug): fixed.
  - The repeated-option cases moved to `routing-46-repeated-option-binding.cs` (237 lines, its own test class,
    the same `#if !JARIBU_MULTI` entry point). All 11 cases moved with the same routes, argv, and expected
    output.
  - routing-45 (420 lines) has its blank lines back.
  - Builder chains are wrapped.
  - The gate record has 20 + 11 passing.
- M12 (nit): fixed.
  - The emitter comments say "string element, or one with no built-in conversion".
  - The guide row, checklist item 3, and the migrate skill say "string or enum (or any other element with no
    built-in conversion)".
  - This matches `RepeatedElementAccepts` returning null.

## Issues

None new.

## Duplicates / conflicts

- Single reviewer, so there is nothing to collapse.
