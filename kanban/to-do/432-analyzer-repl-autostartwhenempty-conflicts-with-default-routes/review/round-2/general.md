# Round 2 — general
**Date:** 2026-09-28
**Scope reviewed:** M1 fix on `ReplDefaultRouteValidator.IsTopLevelDefaultRoute`, the NURU_R004 descriptor description, `specificity-algorithm.md`, and the two new generator-49 cases. Prior NURU_R004 cases re-run with the same driver.

## Summary

M1 is fixed. A top-level `""` route is a conflict only when it can match `routeArgs.Length == 0`: no required non-catch-all parameter and no required option. Catch-all parameters still conflict because the matcher binds an empty remainder. Grouped `""` routes, AutoStartWhenEmpty false, and no `AddRepl()` are unchanged. `dotnet run tests/timewarp-nuru-tests/generator/generator-49-nuru-r004-repl-default-route.cs` reports 9 passed, 0 failed, including the required-parameter negative and the optional-parameter positive. No new defects on the fix delta.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/validation/repl-default-route-validator.cs:59
- Description: M1. `IsTopLevelDefaultRoute` now returns false when a required non-catch-all parameter or a required option is present. generator-49 `Should_not_emit_nuru_r004_for_required_parameter_on_empty_pattern` passes and still emits `R004RequiredParamDefault`. `Should_emit_nuru_r004_for_optional_parameter_on_empty_pattern` still reports NURU_R004.
- Suggestion: none
- Status: fixed
