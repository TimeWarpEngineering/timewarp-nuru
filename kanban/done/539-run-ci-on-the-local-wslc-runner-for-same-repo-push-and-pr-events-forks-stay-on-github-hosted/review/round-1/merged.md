# Round 1 — merged findings
**Date:** 2026-11-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: .github/actions/install-pwsh/action.yml:1 (also .github/workflows/workflow.yml:184-189; task.md `## Results` "pwsh" bullet and `### How to validate`)
- Description: The premise that the wslc image has no pwsh is false. A targeted `wslc run --rm twe-runner-01 … command -v pwsh` returned `/usr/bin/pwsh`; the dotnet SDK base image ships PowerShell. The install-pwsh action always exits at its `command -v pwsh` guard on wslc, so it is unused code. Its description, the workflow step comment and the Results bullet all state something false. `### How to validate` expects `Installed 7.7.0`, but the step will print `pwsh is already installed: …`.
- Suggestion: Remove the action and the `Install PowerShell` step, then correct the Results bullet and the How to validate expectation. If the action stays as a guard against a future base image, rewrite the three comments to say so and change the expected output.
- Source: general
- Disposition notes: Re-run 2026-11-15 (host restart): still open; product files unchanged since `d7f84ffd`.

### M2 — Severity: nit — Status: open
- File: kanban/in-progress/539-…/task.md `## Results` "Branch base" bullet (also `## Session` Grok line and `### Gates` preamble)
- Description: Results say the branch was reset to `origin/master` with only the 539 change and the gates re-ran on that tree. HEAD `0486ecc5` merged `origin/feature/groom-2026-11-15` back in, and the host's base for this walk is the groom branch (Budget.ByDiff 446 lines matches `origin/feature/groom-2026-11-15...HEAD`). The narrative is stale; the product delta and its gates record are unaffected.
- Suggestion: Say the base is `feature/groom-2026-11-15` (496, 497, 520 come from that base), and note the gates ran before the groom merge.
- Source: general (round-1 re-run)
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
