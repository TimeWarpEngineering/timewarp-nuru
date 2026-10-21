# Round 4 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:193
- Description: Re-checked on `fb21baaf` through `routing-37` (30 of 30). A close typo of a required option or unbound flag on a route with a leading literal is still suggested. The alias guard test returned.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Fixed in `72cf0f08`. Still passing on `fb21baaf`.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:199
- Description: Re-checked on `fb21baaf`. A route with no leading literal still records the unknown option in `EmitUnknownOptionShapeCheck` before the option-guard skip. `--check-update` on `--check-updates`, and `readme --outpt out` on `{file} --output {output}`, suggest the declared form. The handler does not run. A missing value stays `Unknown command`.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Fixed in `0ef92207`. Still passing on `fb21baaf`.

### M3 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/invocation/nuru-unknown-option.cs:70
- Description: Re-checked on `fb21baaf`. `__unknownAt` is set only when `NuruUnknownOption.TakesValue` finds the closest declared form in the route's value-taking forms and the token has no `=value`. `--check-update extra` on `--check-updates`, `readme --check-update extra` on `{file} --check-updates`, and `readme --forc extra` on `{file} --output {output} --force` exit 1 with `Unknown command` and no suggestion. `readme --outpt out` on that last route still suggests `--output`. Handlers do not run. `routing-37` passed 30 of 30.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Fixed in `fb21baaf`. Verified by `routing-37` on this round.

## Duplicates / conflicts

- Single reviewer. M1, M2, and M3 stay fixed. No new findings on this delta.
