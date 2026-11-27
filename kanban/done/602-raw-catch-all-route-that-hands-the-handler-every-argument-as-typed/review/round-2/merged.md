# Round 2 — merged findings
**Date:** 2026-11-27
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-help-emitter.cs:361
- Description: Group help intercepted `<group> --help` / `-h` for a raw route whose literal prefix equals the group prefix.
- Source: general (round 1)
- Disposition notes: Fixed in 1fa4a72e. The group summary is skipped when a raw route's literal prefix equals the group words. routing-43 pins `--help`, `-h`, `--version`, and sibling `show --help`.

### M2 — Severity: suggestion — Status: fixed
- File: tests/timewarp-nuru-tests/routing/routing-43-raw-catchall.cs
- Description: No runtime test covered the endpoint `IsRaw` form or the alias path.
- Source: general (round 1)
- Disposition notes: Fixed in 1fa4a72e. A grouped endpoint via `.Map<T>()` with the full token set, and a fluent `.WithAlias("t")` raw route.

### M3 — Severity: nit — Status: fixed
- File: documentation/user/features/routing.md
- Description: The configuration-override side effects on a raw route were not documented.
- Source: general (round 1)
- Disposition notes: Fixed in 1fa4a72e. routing.md states both facts and routing-43 pins them.

### M4 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru/builders/endpoint-builder.cs:111; changelog.md:11
- Description: The new public `EndpointBuilder<TBuilder>.WithAlias(string)` turns on fluent route aliases, and the changelog does not name it.
- Suggestion: One changelog line for the fluent `.WithAlias(...)`.
- Source: general (round 2)
- Disposition notes: wontfix (review orchestrator, 2026-11-27). The beta package gains an additive stub that the generator already handled. It has a runtime test and is recorded in task.md Results. The changelog line goes with the next change that touches fluent aliases.

## Duplicates / conflicts

- None. One reviewer.
