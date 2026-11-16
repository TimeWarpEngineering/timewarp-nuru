# Round 2 — merged findings
**Date:** 2026-11-16
**Sources:** general

Prior round `review/round-1/` is unchanged. M1 and M2 stay the live ids. No new ids.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Resolved prior

- None. No fix pass ran after round 1.

## Issues

### M1 — Severity: suggestion — Status: open
- File: .github/actions/install-pwsh/action.yml:3 (also .github/workflows/workflow.yml:185-187; task.md Goal line 36, Results pwsh bullet, ### How to validate)
- Description: The wslc image already has pwsh. The base image `mcr.microsoft.com/dotnet/sdk:10.0-noble` links `/usr/bin/pwsh` (SDK Dockerfile installs PowerShell 7.6.9). The infra runner Dockerfile does not remove it. Round 1's `wslc run --rm twe-runner-01` reported `/usr/bin/pwsh`. The install action returns at `command -v pwsh` on that runner, so the 7.7.0 download never runs. The action description, the workflow comment, the Goal acceptance line, and the Results bullet say the image has no pwsh. How to validate expects `Installed 7.7.0`; the step prints `pwsh is already installed:`.
- Suggestion: Remove the action and the Install PowerShell step, and correct the Goal line, the Results bullet, and How to validate. If the action stays as a guard, say that and expect `pwsh is already installed`.
- Source: general (round 1, reconfirmed round 2)
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: kanban/in-progress/539-…/task.md:79 (also Session line 61 and ### Gates preamble line 127)
- Description: Results say the branch was reset to `origin/master` and contains only the 539 change. `d7f84ffd` was that reset. HEAD `aabb1737` includes merge `0486ecc5` of `origin/feature/groom-2026-11-15`. Master has since merged that branch (PR #415) and task 594, and is 16 commits ahead. The 7104-line three-dot diff is the older merge-base, not the merge result. `git merge-tree --write-tree master HEAD` exits 0 (tree `826d65f3`) and keeps Amuru beta.5, CsCheck 5.0.1, and tasks 496, 497, and 520 in done, plus the 539 workflow. The gates record still covers the CI file edit. The narrative is stale. Round 1's suggestion to call the groom branch the base is out of date.
- Suggestion: Rewrite the Branch base bullet, the Grok Session line, and the Gates preamble to the history above, including the clean merge-tree result. Do not reset or rebase for this wording fix.
- Source: general (round 1, updated round 2)
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer). Round 1 text was not copied over the top of round 1; the same ids are restated here with the round 2 evidence.
