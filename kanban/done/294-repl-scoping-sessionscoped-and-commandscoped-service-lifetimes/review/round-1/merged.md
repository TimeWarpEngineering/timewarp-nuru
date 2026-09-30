# Round 1 — merged findings
**Date:** 2026-09-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 4 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:1071
- Description: Source-generated DI opens a single-run command scope for the REPL host (`fromRepl` false), so command-scoped services and process `AddScoped` instances are created for the whole session.
- Suggestion: Skip command scope and non-REPL scoped initialization on the REPL host, matching `NuruCommandLease`.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:1068
- Description: `hostRepl` matches only exact `--interactive` / `-i` on raw `args`. `AutoStartWhenEmpty` leaves the runtime command cache active, and the first command's `Begin` throws.
- Suggestion: Decide the host from `routeArgs`, including empty routes when `AutoStartWhenEmpty` is set.
- Source: general
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: source/timewarp-nuru/dependency-injection/nuru-service-collection-extensions.cs:338
- Description: Session services are transients. A command scope disposes a session instance injected into a scoped service when the command ends.
- Suggestion: Remove cache-owned session instances from the scope disposable list before the scope disposes.
- Source: general
- Disposition notes:

### M4 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:576
- Description: Single-run source generation constructs scoped services before session and command fields are assigned, so a scoped service that injects them gets null.
- Suggestion: Initialize those scoped services in `BeginSingleRunServices` after the session exists, once per process.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. One reviewer.
