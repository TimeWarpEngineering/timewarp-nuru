# Round 1 — general
**Date:** 2026-09-28
**Scope reviewed:** branch `task/432-analyzer-repl-autostartwhenempty-conflicts-with-de` vs `origin/master` — NURU_R004 validator, descriptor, generator location change, generator-49 tests, CI standalone wiring, and the NURU_R documentation sites.

## Summary

`ReplDefaultRouteValidator` runs from `ModelValidator` on each app's combined fluent and endpoint routes. NURU_R004 is an Error when `HasRepl` and `ReplOptions.AutoStartWhenEmpty` are true and `IsTopLevelDefaultRoute` is true. That predicate is group-prefix empty and `OriginalPattern` empty. `EmitInteractiveFlag` only starts the REPL when `routeArgs.Length == 0`, and the matcher skips a `[NuruRoute("")]` route that has a required positional parameter (`routeArgs.Length >= minPositionalArgs` fails for length 0). The diagnostic still fires for that route and says it is unreachable. Fluent `Map("")`, attributed and discovered `""` routes, grouped `""` routes, AutoStartWhenEmpty false, and no `AddRepl()` match the brief. NURU_R004 is recorded in `AnalyzerReleases.Unshipped.md` and `specificity-algorithm.md` beside NURU_R001–R003. Empty `Map("")` locations are kept so the diagnostic can anchor on the literal.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/validation/repl-default-route-validator.cs:56
- Description: `IsTopLevelDefaultRoute` treats every top-level `OriginalPattern` of `""` as a conflict. `[NuruRoute("")]` plus a required `[Parameter]` (non-nullable, not catch-all) does not match an empty argument list. `RouteMatcherEmitter` sets `minPositionalArgs` to the required non-catch-all parameter count and skips the route when `routeArgs.Length` is below that. `AutoStartWhenEmpty` only intercepts length 0, which that route never accepted, so the route still runs when the parameter is present. The same shape is already in the repo (`tests/timewarp-nuru-tests/help/help-05-issue179-required-param.cs`). Reporting NURU_R004 there is a false Error, and the descriptor text says the route can never run. A required option (`OptionDefinition.IsOptional == false`) likewise `goto route_skip` when the option is absent, so an empty list does not select that route either. Catch-all parameters can bind an empty remainder and still conflict; optional parameters (`string?`) still match length 0 and should keep the diagnostic.
- Suggestion: Report NURU_R004 only when the top-level `""` route can match `routeArgs.Length == 0`: no required non-catch-all parameter and no required option. Add generator tests for a required parameter (no NURU_R004, route still emitted) and an optional parameter (NURU_R004 still reported).
- Status: open
