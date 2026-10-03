# Round 1 — merged findings
**Date:** 2026-10-03
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tests/timewarp-nuru-tests/generator/Directory.Build.props:10
- Description: Fixtures include is conditioned on `MSBuildProjectName` == `generator-26-constructor-dependency-resolution.cs` (with `.cs` suffix); not statically verifiable.
- Suggestion: Confirm via standalone run, or accept both suffixed and unsuffixed names.
- Source: general
- Disposition notes: Verified empirically by the orchestrator — after `ganda runfile cache --clear`, the standalone `dotnet run …/generator-26-constructor-dependency-resolution.cs` compiles the fixtures and passes 10/10 (exit 0), so the condition matches. A dual condition would be speculative. Decided by: review orchestrator.

## Verification run by orchestrator

- `ganda runfile cache --clear`, then standalone runs: generator-26 10 passed; routing-05-option-modifier-matrix 11; routing-05-boolean-mixed-typed-options 7; routing-05-option-aliases 13 — all exit 0.
- `dotnet build tests/ci-tests/run-ci-tests.cs` exit 0.
- `ganda repo audit` — passes all checks.

## Duplicates / conflicts

- None (single reviewer).
