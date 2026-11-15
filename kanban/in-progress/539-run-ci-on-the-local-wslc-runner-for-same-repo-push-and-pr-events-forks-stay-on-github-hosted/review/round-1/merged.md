# Round 1 — merged findings
**Date:** 2026-11-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: .github/actions/install-pwsh/action.yml:1 (also .github/workflows/workflow.yml:184-189; task.md `## Results` "pwsh" bullet and `### How to validate`)
- Description: The premise that the wslc image has no pwsh is false. A targeted `wslc run --rm twe-runner-01 … command -v pwsh` returned `/usr/bin/pwsh`; the dotnet SDK base image ships PowerShell. The install-pwsh action always exits at its `command -v pwsh` guard on wslc, so it is unused code. Its description, the workflow step comment and the Results bullet all state something false. `### How to validate` expects `Installed 7.7.0`, but the step will print `pwsh is already installed: …`.
- Suggestion: Remove the action and the `Install PowerShell` step, then correct the Results bullet and the How to validate expectation. If the action stays as a guard against a future base image, rewrite the three comments to say so and change the expected output.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
