# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** branch `task/574-collect-ci-code-coverage-through-jaribu-runfile` vs `master`. Product diff in `workflow.yml`, `Directory.Packages.props`, `run-ci-tests.cs`, `run-standalone-tests.cs`, `test-command.cs`, and `workflow-command.cs`.

## Summary

CI test legs now collect one Cobertura report per leg through Jaribu 1.0.0-beta.16 runfile coverage and upload it. `dev test --coverage` sets `JARIBU_COVERAGE`, `JARIBU_COVERAGE_RESULTS` (`artifacts/coverage`), and a module exclude that keeps assemblies whose file names start with `TimeWarp.Nuru`. `dev workflow` turns that flag on only when `GITHUB_ACTIONS=true`. The multi runner returns the relaunched child's exit code so phase `all` does not run standalone twice; the standalone runner calls `RunAllTests(allowEmpty: true)` before its per-file `dotnet run` children so one collector profiles that leg. The workflow installs `dotnet-coverage` on `test-*` stages and uploads `artifacts/coverage` as `coverage-test-multi` and `coverage-test-standalone`. `ci-code-coverage.min-line` is unset. The implementer restatement matches the task. Recorded build, test, audit PASS, and the first line rates cover this diff.

## Issues

<!-- none -->
