# Round 2 — general
**Date:** 2026-10-03
**Scope reviewed:** fix delta for M1 and M2 (add-configuration-locator.cs, generator-52 test).

## Summary

M1 re-verified. A `ConfigureServices`-registered service with an `IConfiguration` primary constructor now yields full configuration sources, and the new test covers it. A `Handle(IConfiguration)` method on a type with no Nuru handler interface still yields minimal, and so does a static helper method. The Design region matches the new rule. M2 re-verified. generator-52: 16 passed. generator-14: 5 passed. Full CI (`run-ci-tests.cs`, after a runfile cache clear) exited 0. No new issues.

## Carried findings

- M1 — bug — fixed (verified)
- M2 — nit — fixed (verified)
