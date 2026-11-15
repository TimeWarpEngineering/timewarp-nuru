# Round 1 — general
**Date:** 2026-11-15
**Scope reviewed:** same as framework (branch vs `master`, commit d7f84ffd), plus the runner image source in timewarp-infrastructure `runner/Dockerfile`, `runner/run-loop.ps1` and `runner/entrypoint.sh` (origin/master 447cb65).

## Summary

The `runs-on` expression is right. Same-repo `push` and `pull_request` go to `["self-hosted","twe-001","wslc"]`. Fork PRs, `workflow_dispatch` merge mode and the `CI_HOSTED_ONLY=true` kill switch go to `ubuntu-latest`. `release` and probe have no `stage` job. When the head repo is deleted, `head.repo` is null, and that case also goes hosted. Check names, the hosted `changes`/`ci`/`release` jobs and OIDC are unchanged. Skipping the zsh cache and the NuGet Actions cache when the runner is not GitHub-hosted is correct. The fork-safety record (approval `all_external_contributors`, runner group 3, Steven's yes in infra 004) matches the task's hard rule. The container is a fresh `wslc run --rm` per job, so `dotnet tool install --global dotnet-coverage` cannot hit an "already installed" failure.

The goal restatement matches the task. The task named the `ci` job, but task 538 has since moved the work into the `stage` matrix, and the restatement follows that. This is not a misread.

One finding. The new `install-pwsh` action, its workflow step, and the Results line "The image has no PowerShell" all rest on a premise that is false.

**Targeted command (one, per the recorded-gates exception).** I suspected that `Runner size` (`nproc && free -h`) would fail on wslc, because the Dockerfile does not install `procps`. To check, I ran:
`"/mnt/c/Program Files/WSL/wslc.exe" run --rm twe-runner-01 bash -c 'for t in free nproc pwsh ps curl sha256sum tar zsh gh; do printf "%s: " $t; command -v $t || echo MISSING; done'`
This uses the image's documented smoke-test path: `entrypoint.sh` runs its arguments when it gets any, and registers no runner. Output: every tool was present. `free` is at `/usr/bin/free`, so that suspicion was wrong. `pwsh` is at `/usr/bin/pwsh`, which led to Issue 1.

## Issues

### Issue 1 — Severity: suggestion
- File: .github/actions/install-pwsh/action.yml:1 (and .github/workflows/workflow.yml:184-189; task.md `## Results` "pwsh" bullet and `### How to validate`)
- Description: The wslc image already has PowerShell at `/usr/bin/pwsh`. The base image `mcr.microsoft.com/dotnet/sdk:10.0-noble` ships PowerShell as a global tool linked into `/usr/bin`; the targeted command above confirms it. Several things in the change say otherwise:
  - The Results say "It has no PowerShell … so every `run` step would fail on wslc". The implementer seems to have read this from the infra Dockerfile without checking the built image.
  - The action description says "The wslc image is the .NET SDK on Ubuntu (bash, no pwsh, no sudo)".
  - The workflow step comment says "The wslc image has bash and no pwsh".
  - `### How to validate` expects "Each stage's `Install PowerShell` prints `Installed 7.7.0`". The step will instead print `pwsh is already installed: <image version>`, so whoever validates the PR will see output that does not match the record.

  The action never reaches its download path on the only runner that calls it. That leaves 32 lines of unused code plus three comments that state something false. The pinned 7.7.0 tarball and sha256 are never exercised.
- Suggestion: Remove `.github/actions/install-pwsh/` and the `Install PowerShell` step. Correct the Results "pwsh" bullet and the `### How to validate` expectation: the image ships pwsh from the SDK base image, so no install step is needed. If the action should stay as a guard against a future base image without pwsh, keep it, but rewrite all three comments to say that, and change the How to validate expectation to `pwsh is already installed`.
- Status: open

## Re-run (host restart) — 2026-11-15

**Scope:** `origin/feature/groom-2026-11-15...HEAD` (HEAD `0486ecc5`). The product files are byte-for-byte the `d7f84ffd` change reviewed above; the new commits are the kanban land (`c02469cd`) and a merge of the groom branch (`0486ecc5`).

- Issue 1 still holds. `.github/actions/install-pwsh/action.yml`, the `Install PowerShell` step comment (`.github/workflows/workflow.yml:185`), the Results "pwsh" bullet, the Goal restatement's "The image has no `pwsh`" acceptance line, and the How to validate expectation `Installed 7.7.0` are all unchanged. Status: open.
- No targeted command was run in the re-run; Issue 1 rests on the first run's `wslc run` check.
- Re-read the `runs-on` expression, the `nuget-cache` input (`cache: ${{ inputs.nuget-cache == 'true' }}` gets a string `'true'`/`'false'` from `runner.environment == 'github-hosted'`, so the comparison is right on both runner kinds), the zsh gating and `defaults.run.shell: pwsh`. No new correctness issue.

### Issue 2 — Severity: nit
- File: kanban/in-progress/539-…/task.md `## Results` → "Branch base" bullet (and `## Session` Grok line, `### Gates` preamble)
- Description: The record says the walk reset the task branch to `origin/master` and kept only the 539 change, and that the gates were re-run on that tree. HEAD `0486ecc5` has since merged `origin/feature/groom-2026-11-15` back in, and the host measures this walk against the groom branch (Budget.ByDiff 446 lines = `origin/feature/groom-2026-11-15...HEAD`). The groom branch is the base, so the reset was not needed, and the Results now describe a tree the branch no longer has. The product delta is the same, so the gates record still covers the CI-file change; only the narrative is stale.
- Suggestion: Rewrite the "Branch base" bullet to say the base is `feature/groom-2026-11-15` and that 496, 497 and 520 arrive from that base, not from this task. Note in `### Gates` that they ran on the `origin/master`-based tree before the groom merge.
- Status: open
