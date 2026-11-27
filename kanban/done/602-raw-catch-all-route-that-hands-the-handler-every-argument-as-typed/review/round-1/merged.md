# Round 1 — merged findings
**Date:** 2026-11-27
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-help-emitter.cs:330 (`EmitGroupHelpChecks`); runs before user routes at interceptor-emitter.method-body.cs:118
- Description: Group-level help still intercepts `<group> --help` / `-h` for a raw route whose literal prefix equals its group prefix (for example `[NuruRouteGroup("tail")]` with `[NuruRoute("")]` and `[Parameter(IsCatchAll = true, IsRaw = true)]`). The handler never sees `--help`. This breaks the task requirement "`--help` and `--version` are passed through on a raw route", and it is the `ganda tail` shape in timewarp-ganda task 731. Per-route and shared-prefix help were exempted; group help was not.
- Suggestion: Skip the group help check when a raw route's literal prefix equals the group's words. Add a routing test with a grouped endpoint raw route that receives `--help`.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tests/timewarp-nuru-tests/routing/routing-43-raw-catchall.cs; tests/timewarp-nuru-tests/generator/generator-81-raw-catchall-diagnostic.cs:75
- Description: No runtime test covers the endpoint form (`IsRaw`) or the alias path (`EmitAliasMatch` raw branch). The endpoint check is a generated-text match only.
- Suggestion: Add an endpoint runtime test via `.Map<T>()` with the full `Tokens` set and one `.WithAlias(...)` raw route test.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:193-197; documentation/user/features/routing.md
- Description: On a raw route a configuration override reaches the handler and is also applied to configuration. An override typed before the raw route's leading literal stops that route from matching. Neither is documented.
- Suggestion: State both in routing.md's raw catch-all rules, or match leading literals against `routeArgs` and take the raw tail from `__rawRouteArgs`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. One reviewer.
