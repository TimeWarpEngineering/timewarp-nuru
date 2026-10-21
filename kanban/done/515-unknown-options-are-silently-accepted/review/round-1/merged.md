# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:712
- Description: A required value option, a required value that is missing, or an unbound discriminator flag executes `goto route_skip` inside option parsing, before the unknown-option scan. The route does not run, but the token is not recorded, so the call exits 1 as `Unknown command` and does not suggest the close declared form. Confirmed in generated code: `kanban create {title} --folder {folder}` with `--foldr` jumps at `if (!__folder_flagFound_0) goto route_skip_0` before the scan; `commit --amend` with `--ammend` jumps at `if (!amend) goto route_skip_0`. Both typos are within the suggestion threshold. Same helper is used for aliases. Tests only cover optional options and bound flags.
- Suggestion: When the literal prefix matches, record the unknown option and its forms before those skips, then still skip the route. Cover a required value option and an unbound discriminator flag whose typo is close, plus the case where the required form is present and a second token is unknown.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. No overlap.
