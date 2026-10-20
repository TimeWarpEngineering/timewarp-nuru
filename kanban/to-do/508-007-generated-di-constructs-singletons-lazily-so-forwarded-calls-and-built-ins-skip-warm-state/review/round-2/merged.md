# Round 2 — merged findings
**Date:** 2026-10-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/service-scope-expressions.cs:66
- Description: A singleton that injected a Scoped service over a session or command service kept the run's instance. The run disposed it, and the next call saw a dead dependency. Raised in round 1.
- Suggestion: Keep the per-run instance on the call and clear it at the end of the run. A process constructor must not read that slot. Report NURU056. Cover it with a two-call test.
- Source: general
- Disposition notes: Fixed in `53d490aa` and re-verified in round 2. The Scoped service has no process field. It reads `__cmd_` on the call, and that slot is cleared when the run ends. `ProcessInit` builds an untracked copy, so the singleton does not keep the run's instance. NURU056 reports the Singleton → late Scoped edge. `generator-57` passed 1 of 1. `generator-58` passed 3 of 3. `generator-51` passed 6 of 6. `daemon-05` passed 5 of 5.

## Duplicates / conflicts

- One reviewer. M1 is carried from round 1. No new findings.
