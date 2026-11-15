# Run CI on the local wslc runner for same-repo push and PR events (forks stay on GitHub-hosted)

## Description

Move nuru's `ci` job off `ubuntu-latest` onto the local wslc container runner on TWE-001 (`twe-runner-01`, labels `self-hosted,twe-001,wslc`) for **same-repo** `push` and `pull_request` events only. PRs from forks stay on GitHub-hosted runners.

timewarp-nuru is a **PUBLIC** repo. Untrusted fork code must never run on Steven's machines. That is the hard rule for this task.

Filed 2026-10-25 at Steven's request ("change Nuru and architecture to run on the local wslc runner", 23:44 Asia/Bangkok). This is Steven's explicit decision and supersedes intel PR #74 action I (Busara: KILL, "no self-hosted runners for the public repos"), so the fork-safety requirements below are mandatory, not optional.

**Savings are estimates** from intel [PR #74](https://github.com/TimeWarpEngineering/timewarp-intel/pull/74) / [PR #75](https://github.com/TimeWarpEngineering/timewarp-intel/pull/75): today nuru runs 11.4 m median / 12.4 m p90 on hosted 4 vCPU / 15 GiB; the main gains on wslc are no hosted-minute use and possibly faster setup, but **no before/after has been measured** and TWE-001's cores and RAM are unverified.

## Requirements

- `runs-on` picks `["self-hosted", "twe-001", "wslc"]` only when the event is `push` to this repo, or `pull_request` whose `head.repo.full_name == github.repository`. Everything else (fork PRs) uses `ubuntu-latest`.
  - Ganda's expression is the model, but it also requires `github.event.repository.private == true`, which is false for nuru. Don't copy it verbatim; drop only the private check, keep the same-repo check.
  - Target the `wslc` label so jobs land on the container runner, not the bare-WSL systemd runner `twe-001`.
- **Fork safety (public repo):** a fork PR runs the workflow file from the PR, so a fork can edit `runs-on`. The `runs-on` expression alone does not protect Steven's machines. Before switching, confirm with Steven and record in Results:
  - the org/repo setting for fork PR workflows is "Require approval for all external contributors" (or stricter), and approvers check workflow diffs before approving;
  - the runner group (org is on the Free plan, so the runner group is "Default") is allowed for public repos only with Steven's yes; that setting is org-wide. See timewarp-infrastructure task 004.
- `release` and `workflow_dispatch` (release / probe / merge modes) stay on GitHub-hosted unless Steven says otherwise; OIDC trusted publishing is unchanged.
- Keep the required check names unchanged (the stable `ci` check / aggregate), so branch protection and 537's skip still work.
- Tools the wslc image lacks: the "Install zsh" step uses `sudo apt-get`, and the image runs as non-root `runner` with no sudo. zsh must be baked into the image (timewarp-infrastructure 004) or the step adapted. Check every other step for the same problem.
- Interaction with **538** (parallel jobs): one local runner instance runs one job at a time, so parallel jobs may serialize and wall time may go up, not down. Check runner capacity first (`nproc`, free RAM, number of runner instances: timewarp-infrastructure 004) and decide the order of 538 vs this task with that data.
- Measure: before (hosted) vs after (wslc) wall time over several runs; record queue time too, since nuru, architecture and ganda will share the runner.
- Use the newest current releases of actions and tools.

## Goal (implementer restatement)

Run nuru's build and test work on the local wslc container runners on TWE-001 (labels `self-hosted, twe-001, wslc`) when the code is trusted: a `push` to this repo or a `pull_request` whose head branch is in this repo. Fork PRs, `release`, and `workflow_dispatch` (merge, release, probe) stay on `ubuntu-latest`. The public repo must never route fork code to Steven's machines.

Acceptance:

- The `stage` matrix job's `runs-on` picks `["self-hosted", "twe-001", "wslc"]` only for same-repo push and PR events; everything else gets `ubuntu-latest`.
- The fork-safety settings are confirmed live and recorded: fork PR approval `all_external_contributors` (org and repo), and the runner group that lets this public repo use the runners was created with Steven's yes (infrastructure 004).
- No step needs `sudo` on the wslc image. zsh is baked and the apt fallback is hosted-only. The image has no `pwsh`, so each stage job extracts PowerShell 7.7.0 into `~/.local` with bash before any `pwsh` step. The GitHub NuGet cache stays off on wslc.
- Check names (`changes`, `build`, `samples`, `test-multi`, `test-standalone`, `ci`) are unchanged; release/OIDC unchanged.
- Hosted baseline wall and queue times recorded; the wslc numbers come from this PR's own CI run.

## Checklist

- [x] Confirm runner capacity and the runner-group public-repo setting (timewarp-infrastructure 004)
- [x] Confirm fork-PR approval setting with Steven; record it
- [x] Bake or adapt tools the image lacks (zsh, any `sudo apt` steps)
- [x] `runs-on` expression: same-repo push/PR to `wslc`, forks to `ubuntu-latest`
- [ ] Prove: a same-repo PR runs on `twe-runner-01-*` (this PR's CI run); fork path proven by dry evaluation (below)
- [ ] Record before/after wall time and queue time (before recorded; after = this PR's CI run)
- [ ] Open a PR with run links as proof (host open-pr)

## Notes

- Related nuru tasks: 537 (path filters), 538 (parallel jobs). Not a hard depends-on edge; ordering vs 538 is decided by the capacity check.
- Cross-repo: timewarp-infrastructure 004 (runner capacity, wslc targeting, image tools). timewarp-architecture has the matching task.
- Steven's yes before any workflow edit lands. The nuru repo bot owns the edit.
- WSLC runner detail is from Akili's read of the repos and job API (intel #75); Busara did not verify it.

## Session

- Created: 985504 (2026-10-25)
- 2026-11-15 implement (Claude Opus 5.5, ganda task work): live checks of runners, runner groups and fork approval; workflow edit; gates.
- 2026-11-15 implement (Grok 4.7, ganda task work): the claim branch had been cut from `feature/groom-2026-11-15` and would have pulled tasks 496, 497, and 520 into the PR. Replayed only the 539 workflow and kitchen onto `origin/master`. The wslc image has no `pwsh`; the stage job extracts PowerShell 7.7.0 without sudo before any `pwsh` step. Gates re-run on that tree.
- 2026-11-15 review round 1 (Claude Opus 5.5, ganda task work review oracle): effort 3, roster general, turn cap 200. One targeted command: `wslc run --rm twe-runner-01` tool check.
- 2026-11-15 review round 1 re-run after host restart (Claude Opus 5.5, ganda task work review oracle): effort 2 (Budget.ByDiff 446 lines vs `origin/feature/groom-2026-11-15`), roster general, turn cap 120. No targeted command.

## Results

- 2026-11-15 groom: timewarp-infrastructure 004 is done; runner capacity, wslc targeting, and image-tool decisions live there. Start from that data instead of re-checking.

### 2026-11-15 implement

**What changed**

- `.github/workflows/workflow.yml`, `stage` job (the matrix that does all the build and test work: `build`, `samples`, `test-multi`, `test-standalone`): `runs-on` is now
  `vars.CI_HOSTED_ONLY != 'true' && (push || (pull_request && head.repo.full_name == github.repository)) && ["self-hosted","twe-001","wslc"] || 'ubuntu-latest'`.
  Ganda's model expression minus its `repository.private == true` check, plus a repo-variable kill switch: set `CI_HOSTED_ONLY=true` (Settings, Actions variables) to send every run back to hosted when TWE-001 is offline. Without it, same-repo jobs would queue until the runners return.
- `changes`, `ci` (the required aggregate) and `release` stay on `ubuntu-latest`. `changes` needs `yq` (not in the wslc image) and is a 10 s gate; `ci` only downloads/uploads artifacts and calls `gh api`. Keeping them hosted leaves all four wslc slots for the four stages and keeps the `ci` check from queueing behind other repos. `workflow_dispatch` (all modes) and `release` stay hosted; OIDC trusted publishing unchanged. Job and check names unchanged.
- zsh: the wslc image bakes zsh 5.9 and has no sudo. `Read OS codename` and `Cache zsh packages` now run only when `runner.environment == 'github-hosted'`; `Install zsh` already exits 0 when zsh is present, so the `sudo` path is hosted-only.
- pwsh: the image is `mcr.microsoft.com/dotnet/sdk:10.0-noble` plus tools (infra 004 Dockerfile). It has no PowerShell, and `defaults.run.shell` is `pwsh`, so every `run` step would fail on wslc. `workflow-pwsh-shell` rejects a bash step in the workflow file, so `.github/actions/install-pwsh` does the extract (official `powershell-7.7.0-linux-x64.tar.gz`, sha256 pinned, no sudo) and the stage job calls it only when `runner.environment == 'self-hosted'`. The 7.7.0 dotnet global tool is not used: NuGet has no `powershell.linux-x64` pack, so `dotnet tool install --global PowerShell` fails (PowerShell issue 28392). `~/.local/bin` is appended to `GITHUB_PATH`. Hosted runners skip the step.
- Branch base: the first implement commit sat on `feature/groom-2026-11-15`, which is not in `origin/master` and carries tasks 496, 497, and 520. This walk reset the task branch to `origin/master` (`08f89145`) and kept only the 539 workflow and kitchen. The remote task branch is updated with `--force-with-lease` before review.
- `ganda repo audit --fix` deployed the current `prepare-commit-msg` runfile (the Task trailer hook). The branch did not have the attest baseline, so `memsearch-scaffold` warned.
- `.github/actions/setup-dotnet/action.yml`: new input `nuget-cache` (default `true`). The stage job passes `runner.environment == 'github-hosted'`. Each wslc instance mounts a NuGet volume shared with ganda and architecture jobs, so saving it to nuru's Actions cache would carry their packages. Side effect: master pushes on wslc no longer refresh the hosted NuGet cache, so a later fork PR may restore an older key or download fresh.
- `dotnet-coverage` installs to `~/.dotnet/tools`, which the image has on `PATH`. `setup-dotnet 11.0.x` installs into `/usr/share/dotnet`, which the image makes writable by `runner` (infra 004).

**Fork safety (public repo), checked live 2026-11-15 with `gh api`**

- Fork PR approval: org `all_external_contributors`, timewarp-nuru `all_external_contributors`. A fork PR runs its own copy of the workflow and can rewrite `runs-on`, so this approval is the real gate; approvers must read the workflow diff. Steven chose this setting (infra 004 Results, 2026-10-26).
- Runner groups: 1 `Default` has `allows_public_repositories=false` (unchanged). Group 3 (created as `twe-001-wslc-public`, now named `self-hosted`) is `visibility=selected`, `allows_public_repositories=true`, and lists timewarp-nuru. Created with Steven's verbatim yes on 2026-10-26 00:46 (UTC+7), infra 004.
- Runners, rechecked this walk: four online, labels `self-hosted, twe-001, wslc, linux`: `twe-runner-01-i1-20261115-120920`, `-i2-20261115-120946`, `-i3-20261115-122015` (idle) and `-i4-20261115-120722` (busy). Names rotate because the loops mint a JIT runner per job. The bare-WSL `twe-001` runner is no longer registered (infra 004), so `wslc` targeting lands only on containers. Group 3 still lists timewarp-nuru.
- Still exposed (infra 004): each instance's NuGet volume persists across jobs and repos, and the container can reach the LAN. Same-repo Dependabot PRs also go to wslc (head repo is this repo).

**Dry evaluation of `runs-on`** (`H` = head repo of the PR, `R` = `github.repository`, kill switch unset)

| Event | Result |
|---|---|
| `push` to master | wslc |
| `pull_request`, `H == R` (same-repo branch, incl. Dependabot) | wslc |
| `pull_request`, `H != R` (fork) | `ubuntu-latest` |
| `workflow_dispatch` mode `merge` | `ubuntu-latest` |
| `release`, dispatch `release` / `probe` | no `stage` job; `release` job on `ubuntu-latest` |
| any event with `CI_HOSTED_ONLY=true` | `ubuntu-latest` |

**Capacity and 538 ordering.** 538 (parallel jobs) is already merged: `stage` is a four-leg matrix. Infra 004 runs four wslc instances, each `--cpus 6 --memory 12g` (hosted is 4 vCPU / 16 GiB) on a 32-thread / 62 GiB wslc VM. One nuru run fills all four slots, so a concurrent ganda or architecture run queues behind it (or nuru queues behind them). Queue time on wslc is the number to watch.

**Before (hosted `ubuntu-latest`), six recent successful runs.** Run wall = created to last job done; queue = job created to started.

| Run | Event | Run wall | Longest stage (test-multi) | Stage queue |
|---|---|---|---|---|
| [43675675853](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/43675675853) | PR | 6 m 9 s | 5 m 15 s | 2 to 3 s |
| [43670847029](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/43670847029) | PR | 8 m 26 s | 7 m 57 s | 2 s |
| [43666397671](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/43666397671) | PR | 8 m 46 s | 8 m 23 s | 2 to 3 s |
| [43630489431](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/43630489431) | push | 7 m 4 s | 6 m 42 s | 2 to 3 s |
| [43628389257](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/43628389257) | push | 8 m 51 s | 8 m 25 s | 2 s |
| [43623967108](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/43623967108) | push | 8 m 21 s | 7 m 46 s | 2 to 3 s |

Median run wall about 8 m 23 s. `changes` and `ci` add about 10 s each.

**After (wslc): not measured yet.** This branch's workflow only runs on `pull_request` and master `push`, and dispatch stays hosted by design, so the first wslc run is this task's own PR CI. Proof to collect from it: `stage` jobs' `runner_name` = `twe-runner-01-i*`, run wall and per-job queue (`gh api /repos/TimeWarpEngineering/timewarp-nuru/actions/runs/<id>/jobs --jq '.jobs[] | "\(.name) \(.runner_name) \(.created_at) \(.started_at) \(.completed_at)"'`). Several-run comparison follows from the next master pushes and PRs.

### How to validate

- Smoke: `gh pr checks <this PR>` then `gh api /repos/TimeWarpEngineering/timewarp-nuru/actions/runs/<run id>/jobs --jq '.jobs[] | "\(.name) \(.runner_name)"'`
- Expect: `build`, `samples`, `test-multi`, `test-standalone` show `twe-runner-01-i*` runner names and pass; `changes` and `ci` show `GitHub Actions *`; the `ci` check is green. Each stage's `Install PowerShell` prints `Installed 7.7.0`. In `test-multi`, `Read OS codename` and `Cache zsh packages` are skipped and `Install zsh` prints `zsh is already installed.`
- Smoke: `actionlint -shellcheck= .github/workflows/workflow.yml` (actionlint v1.7.12)
- Expect: exit 0, no output.

### Gates

Re-run on this walk after the branch was reset onto `origin/master`. The earlier record was from the `feature/groom-2026-11-15` base and is not the proof for this diff.

| Command | Result |
|---|---|
| `./bin/dev build` | passed, 0 warnings, 0 errors |
| `./bin/dev test` | passed, exit 0. Multi-mode execute finished in 206.7 s. Standalone finished in 27.0 s (0 failed). `Tests completed successfully!` |
| `ganda repo audit` | first run failed: `workflow-pwsh-shell` (the bash install step) and `memsearch-scaffold` (outdated `.githooks/prepare-commit-msg`). The install moved into `.github/actions/install-pwsh`. `ganda repo audit --fix` deployed `.githooks/prepare-commit-msg.cs` and linked the hook. That run then passed: 44 passed, 0 failed, 1 skipped (`self-hosted-runner`, public repo) |

### 2026-11-15 review round 1

- Roster general, effort 3, 1 round so far. Counts: bug 0; suggestion 1 open (M1); nit 0.
- M1: the wslc image already ships `pwsh` (`/usr/bin/pwsh`, from the dotnet SDK base image), so `install-pwsh` is unused code, the "no PowerShell" Results bullet is wrong, and the How to validate expectation `Installed 7.7.0` will not match. Fix pass needed.
- The `runs-on` routing, fork safety record, zsh/NuGet-cache gating, and unchanged check names are correct.
- Re-run (host restart, effort 2): M1 still open, product files unchanged since `d7f84ffd`. New M2 (nit, open): the "Branch base" bullet, the Grok Session line and the `### Gates` preamble say the branch was reset to `origin/master`, but HEAD `0486ecc5` merged `origin/feature/groom-2026-11-15` back in and that branch is this walk's base.
- Counts after re-run: bug 0; suggestion 1 open (M1); nit 1 open (M2).
- Disposition: pending (no `review/disposition.md` yet; 2 open, fix pass needed). Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
