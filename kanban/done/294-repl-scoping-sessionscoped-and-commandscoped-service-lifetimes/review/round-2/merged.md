# Round 2 — merged findings
**Date:** 2026-09-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 4 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:1129
- Description: Source-generated DI opened a single-run command scope for the REPL host.
- Suggestion: Skip that scope on the host.
- Source: general
- Disposition notes: Fixed. Idle lease; process scoped fields are not initialized for the host.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:1117
- Description: `hostRepl` ignored `AutoStartWhenEmpty` and config-only argument lists.
- Suggestion: Decide the host from `routeArgs`.
- Source: general
- Disposition notes: Fixed. Expression includes `--interactive`, `-i`, and empty `routeArgs` when auto-start is on.

### M3 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/dependency-injection/nuru-command-lease.cs:137
- Description: Command scopes disposed injected session instances.
- Suggestion: Remove them from the scope disposable list first.
- Source: general
- Disposition notes: Fixed. `ReleaseSessionInstances` before `DisposeAsync`.

### M4 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:700
- Description: Scoped services that inject session or command services were constructed too early on a single run.
- Suggestion: Construct them after those instances exist.
- Source: general
- Disposition notes: Fixed. Built in `BeginSingleRunServices` on every single-run.

## Resolved prior

- M1–M4 carried from round 1. Re-verified against `generator-51-repl-service-scopes.cs` (6 passed).

## Duplicates / conflicts

- None.
