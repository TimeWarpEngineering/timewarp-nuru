# Round 1 — merged findings
**Date:** 2026-10-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: .github/workflows/workflow.yml:81
- Description: The `Install zsh` step has no `if:`. It runs in probe mode, where zsh is unused, and in release mode it runs between `nuget/login` and the pipeline.
- Suggestion: Add `if: github.event_name != 'workflow_dispatch' || inputs.mode != 'probe'`, and move the step above the NuGet login.
- Source: general
- Disposition notes: wontfix (review oracle). The step is guarded by `command -v zsh`, and it is cheap when zsh is already present. Running it in probe mode wastes a few seconds and does no harm. The apt risk in release mode is small: an apt failure would fail the run before anything is pushed, and the remedy is a rerun. Tidy it up the next time `workflow.yml` is touched. It is not worth a fix pass on this task.

## Duplicates / conflicts

- None (single reviewer).
