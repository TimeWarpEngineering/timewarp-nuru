# Round 3 — merged findings
**Date:** 2026-11-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-help-emitter.cs
- Description: Group help answered `<group> --help` / `-h` itself when a raw route's literal prefix was exactly that group's words, so the tokens never reached the handler.
- Source: general (round 1)
- Disposition notes: Fixed in 1fa4a72e and re-verified in round 2. 4711ceed does not change this.

### M2 — Severity: suggestion — Status: fixed
- File: tests/timewarp-nuru-tests/routing/routing-43-raw-catchall.cs
- Description: No runtime test covered the endpoint `IsRaw` form or the alias path.
- Source: general (round 1)
- Disposition notes: Fixed in 1fa4a72e. routing-43 stays in the multi assembly and passed there (18).

### M3 — Severity: nit — Status: fixed
- File: documentation/user/features/routing.md
- Description: routing.md did not say what a configuration override does on a raw route.
- Source: general (round 1)
- Disposition notes: Fixed in 1fa4a72e.

### M4 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru/builders/endpoint-builder.cs; changelog.md
- Description: The changelog does not name the new public fluent `.WithAlias(string)`.
- Source: general (round 2)
- Disposition notes: wontfix (review orchestrator, 2026-11-27). The rationale is unchanged; see `review/disposition.md`.

## Round 3 delta

- 4711ceed (CI `test-multi` CS0433): generator-81 is excluded from the multi assembly and runs in the standalone phase, the same as generator-80. No new findings.

## Duplicates / conflicts

- None. One reviewer.
