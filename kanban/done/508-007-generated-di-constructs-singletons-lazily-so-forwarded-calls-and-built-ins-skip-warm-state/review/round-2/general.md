# Round 2 — general
**Date:** 2026-10-20
**Scope reviewed:** delta `b3c94eeb..53d490aa` (M1 fix). Emitters, `DetectLifetimeMismatches`, `configuration.md`, changelog, `generator-57`, `generator-58`.

## Summary

The fix moves a Scoped service that injects a session or command service off the static process field and onto the call's `__cmd_` slot. `EndSingleRunServicesAsync` and `EndReplCommandServicesAsync` clear that slot. In `ProcessInit`, a per-run dependency is an untracked `new`, so a singleton keeps its own copy. `NURU056` now reports Singleton → that Scoped service. Direct Singleton → session or command already warned, and that constructor now takes the same untracked copy.

Re-checked against `generator-51`: a Scoped service that injects a session service still shares that session instance with the handler (`ReferenceEquals` in `Check`), in source-generated DI and in Microsoft DI. `daemon-05` still passes 5 of 5. No new defect on this delta.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/service-scope-expressions.cs:66
- Round 2 check: `generator-57` passed 1 of 1 (two calls, distinct live holders, shared singleton, root token not the run's and not disposed). `generator-58` passed 3 of 3 (NURU056 for command and session, no warning for a plain Scoped service).

## Issues

<!-- None. -->
