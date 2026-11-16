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
- No step needs `sudo` on the wslc image. zsh is baked and the apt fallback is hosted-only. The image already has `pwsh` (`/usr/bin/pwsh` from the .NET SDK base image), so the `pwsh` run steps need no install step. The GitHub NuGet cache stays off on wslc.
- Check names (`changes`, `build`, `samples`, `test-multi`, `test-standalone`, `ci`) are unchanged; release/OIDC unchanged.
- Hosted baseline wall and queue times recorded; the wslc numbers come from this PR's own CI run.
- (CI red on PR #417) Every test passes on the wslc image without skips. The stdin-TTY test in `routing-33b` gets its pty from util-linux `script` (in the image and on ubuntu-latest) instead of `python3` (not in the image). Only stdin is the pty; stdout and stderr stay separate so the assertions keep their meaning. No other wslc stage needs `python3`.

## Checklist

- [x] Confirm runner capacity and the runner-group public-repo setting (timewarp-infrastructure 004)
- [x] Confirm fork-PR approval setting with Steven; record it
- [x] Bake or adapt tools the image lacks (zsh, any `sudo apt` steps)
- [x] `runs-on` expression: same-repo push/PR to `wslc`, forks to `ubuntu-latest`
- [ ] Prove: a same-repo PR runs on `twe-runner-01-*` (this PR's CI run); fork path proven by dry evaluation (below)
- [ ] Record before/after wall time and queue time (before recorded; after = this PR's CI run)
- [ ] Open a PR with run links as proof (host open-pr)

## Review

- Round 1: Blocked — open M1, M2
- Round 2: Done
- Round 3: Blocked — open M3
## Notes

- Related nuru tasks: 537 (path filters), 538 (parallel jobs). Not a hard depends-on edge; ordering vs 538 is decided by the capacity check.
- Cross-repo: timewarp-infrastructure 004 (runner capacity, wslc targeting, image tools). timewarp-architecture has the matching task.
- Steven's yes before any workflow edit lands. The nuru repo bot owns the edit.
- WSLC runner detail is from Akili's read of the repos and job API (intel #75); Busara did not verify it.

## Session

- Created: 985504 (2026-10-25)
- 2026-11-15 implement (Claude Opus 5.5, ganda task work): live checks of runners, runner groups and fork approval; workflow edit; gates.
- 2026-11-15 implement (Grok 4.7, ganda task work): the claim branch had been cut from `feature/groom-2026-11-15` and would have pulled tasks 496, 497, and 520 into the PR. Replayed only the 539 workflow and kitchen onto `origin/master` (`d7f84ffd`). Added an `install-pwsh` action on the wrong premise that the wslc image has no `pwsh` (removed in the round-2 fix pass, M1). Gates re-run on that tree. A later walk step merged `origin/feature/groom-2026-11-15` back in (`0486ecc5`).
- 2026-11-15 review round 1 (Claude Opus 5.5, ganda task work review oracle): effort 3, roster general, turn cap 200. One targeted command: `wslc run --rm twe-runner-01` tool check.
- 2026-11-15 review round 1 re-run after host restart (Claude Opus 5.5, ganda task work review oracle): effort 2 (Budget.ByDiff 446 lines vs `origin/feature/groom-2026-11-15`), roster general, turn cap 120. No targeted command.
- 2026-11-16 review round 2 (Grok 4.7, ganda task work review oracle): effort 3 (Budget.ByDiff 7104 lines vs `master`), roster general, turn cap 200. One targeted command: `git merge-tree --write-tree master HEAD`. Round 1 files were not edited.
- 2026-11-16 implement-fix-2 (Claude Opus 5.5, ganda task work): M1 removed `.github/actions/install-pwsh` and the `Install PowerShell` step; Goal, Results and How to validate corrected. M2 rewrote the Branch base, Session and Gates preamble. Merged `origin/master` to clear `package-pin-currency`. Gates re-run.
- 2026-11-16 implement (Claude Opus 5.5, ganda task work): CI red on PR #417. The `routing-33b` stdin-TTY test now gets its pty from `script`, not `python3`. Gates re-run.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1e2c3-7a19-73b3-b14c-d7502b85246c, max-turns 200, artifacts general.md, merged.md — 2026-11-16T05:20:59Z
- 2026-11-16 review-2 post-fix (Grok 4.7, ganda task work review oracle): effort 2 (Budget.ByDiff 393 lines), roster general, turn cap 120. Re-reviewed `aabb1737..fea0ecf3`. M1 and M2 fixed. No new findings. Session 01a1e2d9-dfac-7083-baea-4764e549234f.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-16T06:01:26Z
- 2026-11-16 implement-fix-3 (Claude Opus 5.5, ganda task work): M3. Merged `origin/master` (`51693a74`) into the branch (`871a7691`, no reset or rebase). Gates re-run.
- 2026-11-16 review round 3 (Grok 4.7, ganda task work review oracle): effort 2 (Budget.ByDiff 305 lines), roster general, turn cap 120. Re-reviewed HEAD `8839dfd6`. M1 and M2 still fixed. M3 open (`task.md` conflicts with master `4747af42`). One targeted command: `git merge-tree --write-tree master HEAD`. Session 01a1e2f2-5637-7363-90d6-23534a0fa267.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1e2f2-5637-7363-90d6-23534a0fa267, max-turns 120, artifacts general.md, merged.md, disposition.md — 2026-11-16T06:12:28Z
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-16T06:20:57Z

## Results

- 2026-11-15 groom: timewarp-infrastructure 004 is done; runner capacity, wslc targeting, and image-tool decisions live there. Start from that data instead of re-checking.

### 2026-11-15 implement

**What changed**

- `.github/workflows/workflow.yml`, `stage` job (the matrix that does all the build and test work: `build`, `samples`, `test-multi`, `test-standalone`): `runs-on` is now
  `vars.CI_HOSTED_ONLY != 'true' && (push || (pull_request && head.repo.full_name == github.repository)) && ["self-hosted","twe-001","wslc"] || 'ubuntu-latest'`.
  Ganda's model expression minus its `repository.private == true` check, plus a repo-variable kill switch: set `CI_HOSTED_ONLY=true` (Settings, Actions variables) to send every run back to hosted when TWE-001 is offline. Without it, same-repo jobs would queue until the runners return.
- `changes`, `ci` (the required aggregate) and `release` stay on `ubuntu-latest`. `changes` needs `yq` (not in the wslc image) and is a 10 s gate; `ci` only downloads/uploads artifacts and calls `gh api`. Keeping them hosted leaves all four wslc slots for the four stages and keeps the `ci` check from queueing behind other repos. `workflow_dispatch` (all modes) and `release` stay hosted; OIDC trusted publishing unchanged. Job and check names unchanged.
- zsh: the wslc image bakes zsh 5.9 and has no sudo. `Read OS codename` and `Cache zsh packages` now run only when `runner.environment == 'github-hosted'`; `Install zsh` already exits 0 when zsh is present, so the `sudo` path is hosted-only.
- pwsh: the image is `mcr.microsoft.com/dotnet/sdk:10.0-noble` plus tools (infra 004 Dockerfile). The SDK base image ships PowerShell (`/usr/bin/pwsh`, 7.6.x) and the infra Dockerfile keeps it, so `defaults.run.shell: pwsh` works on wslc with no install step. Round 1 review confirmed it with `wslc run --rm twe-runner-01 … command -v pwsh`. An earlier `install-pwsh` action was removed in the round-2 fix pass (M1).
- Branch base: the first implement commit sat on `feature/groom-2026-11-15` (tasks 496, 497, 520). The Grok walk reset the task branch to `origin/master` (`08f89145`) and replayed only the 539 change (`d7f84ffd`). The walk then merged `origin/feature/groom-2026-11-15` back in (`0486ecc5`). Master has since merged that groom branch (PR #415) and task 594, and is 16 commits ahead (`5f0d2c7c`). Round 2 review ran `git merge-tree --write-tree master HEAD`: clean (tree `826d65f3`), keeps Amuru beta.5, CsCheck 5.0.1, tasks 496, 497, 520 in done, and the 539 workflow; it does not revert 594. The round-2 fix pass merged `origin/master` (`5f0d2c7c`) into the branch (`c964078c`, no reset or rebase) because `package-pin-currency` failed on the stale pins; master's copy of this kitchen was older, so the kitchen took the branch side. After that merge the product delta against master is `.github/workflows/workflow.yml` and `.github/actions/setup-dotnet/action.yml` only.
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
- Expect: `build`, `samples`, `test-multi`, `test-standalone` show `twe-runner-01-i*` runner names and pass; `changes` and `ci` show `GitHub Actions *`; the `ci` check is green. There is no `Install PowerShell` step; `Runner size` (the first `pwsh` step) passes on wslc. In `test-multi`, `Read OS codename` and `Cache zsh packages` are skipped and `Install zsh` prints `zsh is already installed.`
- Smoke: `dotnet run tests/timewarp-nuru-tests/routing/routing-33b-json-args-stdin-and-defaults.cs` (on a machine without `python3`, or in the wslc image)
- Expect: 4 of 4 pass, including `Should_cover_stdin_tty_and_a_builtin_that_does_not_read_stdin`; `grep -n python3` on that file finds only the comment.
- Smoke: `actionlint -shellcheck= .github/workflows/workflow.yml` (actionlint v1.7.12)
- Expect: exit 0, no output.

### Gates

The 2026-11-15 rows ran on the `origin/master` replay (`d7f84ffd`), before the groom merge `0486ecc5`. The round-2 fix pass rows below ran on HEAD after that merge, with the `install-pwsh` removal.

| Command | Result |
|---|---|
| `./bin/dev build` (2026-11-15) | passed, 0 warnings, 0 errors |
| `./bin/dev test` (2026-11-15) | passed, exit 0. Multi-mode execute finished in 206.7 s. Standalone finished in 27.0 s (0 failed). `Tests completed successfully!` |
| `ganda repo audit` (2026-11-15) | first run failed: `workflow-pwsh-shell` (the bash install step) and `memsearch-scaffold` (outdated `.githooks/prepare-commit-msg`). The install moved into `.github/actions/install-pwsh`. `ganda repo audit --fix` deployed `.githooks/prepare-commit-msg.cs` and linked the hook. That run then passed: 44 passed, 0 failed, 1 skipped (`self-hosted-runner`, public repo) |
| `./bin/dev build` (2026-11-16 fix pass, after the master merge `c964078c`) | passed, 0 warnings, 0 errors (also passed before the merge) |
| `./bin/dev test` (2026-11-16 fix pass, after the master merge, runfile cache cleared) | passed, exit 0. Multi-mode execute finished in 77.6 s. Standalone finished in 32.8 s (0 failed). `Tests completed successfully!` |
| `ganda repo audit` (2026-11-16 fix pass) | first run failed `package-pin-currency` (Amuru beta.2, Contracts beta.32, Mediator beta.8 behind; master already had the bumps). Merged `origin/master`; rerun: `Repository passes all audit checks.` |
| `actionlint` | not installed on this machine; not run this pass. The edit only deletes one `uses:` step |
| `./bin/dev build` (2026-11-16 CI-red fix, python3 to `script`) | passed, 0 warnings, 0 errors |
| `./bin/dev test` (2026-11-16 CI-red fix) | passed, exit 0. Multi-mode execute finished in 79.7 s (`Should_cover_stdin_tty_and_a_builtin_that_does_not_read_stdin` passed). Standalone finished in 25.6 s (0 failed). `Tests completed successfully!` |
| `ganda repo audit` (2026-11-16 CI-red fix) | `Repository passes all audit checks.` |
| `./bin/dev build` (2026-11-16 fix-3, after the master merge `871a7691`) | passed, 0 errors |
| `./bin/dev test` (2026-11-16 fix-3, runfile cache cleared) | passed, exit 0. Multi-mode execute finished in 73.0 s. Standalone finished in 27.0 s (0 failed). `Tests completed successfully!` |
| `ganda repo audit` (2026-11-16 fix-3) | `Repository passes all audit checks.` |

### 2026-11-16 CI-red fix (python3 missing on wslc)

- `tests/timewarp-nuru-tests/routing/routing-33b-json-args-stdin-and-defaults.cs`: `RunOnPty` now runs `script -q -e -c '<dotnet run …> >out 2>err' /dev/null` (util-linux) instead of a `python3` pty helper. The child's stdin is the pty; its stdout and stderr go to temp files, as with the python helper, so the `requires redirected stdin` assertion means the same thing. `-e` returns the child's exit code. The test still runs on wslc; nothing is skipped.
- Checked in the image: `wslc.exe run --rm twe-runner-01 bash -c "… printf '' | script -q -e -c 'sleep 2; test -t 0 && echo stdin-tty …; exit 1' /dev/null …"` printed `no-python3`, uid `1001`, `script-exit=1`, `stdin-tty`.
- No other wslc stage needs python: `grep -rl python` outside `bin/`, `obj/`, `artifacts/`, `kanban/` hits only this test (and two old `.agent/workspace` notes).
- Proof on wslc is the next PR #417 CI run (`test-multi` green on a `twe-runner-01-i*` runner).

### 2026-11-15 review round 1

- Roster general, effort 3, 1 round so far. Counts: bug 0; suggestion 1 open (M1); nit 0.
- M1: the wslc image already ships `pwsh` (`/usr/bin/pwsh`, from the dotnet SDK base image), so `install-pwsh` is unused code, the "no PowerShell" Results bullet is wrong, and the How to validate expectation `Installed 7.7.0` will not match. Fix pass needed.
- The `runs-on` routing, fork safety record, zsh/NuGet-cache gating, and unchanged check names are correct.
- Re-run (host restart, effort 2): M1 still open, product files unchanged since `d7f84ffd`. New M2 (nit, open): the "Branch base" bullet, the Grok Session line and the `### Gates` preamble say the branch was reset to `origin/master`, but HEAD `0486ecc5` merged `origin/feature/groom-2026-11-15` back in and that branch is this walk's base.
- Counts after re-run: bug 0; suggestion 1 open (M1); nit 1 open (M2).
- Disposition: pending (no `review/disposition.md` yet; 2 open, fix pass needed). Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

### 2026-11-16 review round 2

- Roster: general. Effort: 3 (7104-line three-dot count vs `master`). Turn cap 200. Rounds: 2. Round 1 files were left as they were.
- Counts (this round's ledger, `review/round-2/merged.md`): bug 0 open; suggestion 1 open (M1); nit 1 open (M2); fixed 0; wontfix 0.
- M1 still open: the wslc image already has `/usr/bin/pwsh` (SDK image PowerShell 7.6.9). `install-pwsh` never downloads 7.7.0, and How to validate expects `Installed 7.7.0`.
- M2 still open: the Branch base bullet still describes a pure `origin/master` replay. HEAD includes the groom merge, and master is 16 commits ahead. `git merge-tree --write-tree master HEAD` is clean (tree `826d65f3`) and does not revert task 594.
- Disposition: not written. Two findings are open, so this is not `clean` or `accepted-exceptions`. Fix pass needed for M1 and M2.
- Paths: `review/review-framework.md`, `review/round-2/general.md`, `review/round-2/merged.md`. No `review/disposition.md`.

### 2026-11-16 implement-fix-2 (round 2 findings)

- M1 fixed: deleted `.github/actions/install-pwsh/action.yml` and the stage job's `Install PowerShell` step. The wslc image's `/usr/bin/pwsh` (SDK base image) runs the `pwsh` steps. Goal acceptance, the pwsh Results bullet and How to validate now say so.
- M2 fixed: the Branch base bullet, the Grok Session line and the Gates preamble now record the replay `d7f84ffd`, the groom merge `0486ecc5`, and this pass's `origin/master` merge `c964078c`.

### 2026-11-16 review round 2 (post-fix)

- Roster: general. Effort: 2 (Budget.ByDiff 393 lines). Turn cap 120. This subsection is the disposition. The pre-fix round 2 record above is the open ledger from before the fix pass.
- Counts (live ledger `review/round-2/merged.md`): bug 0; suggestion 1 fixed (M1); nit 1 fixed (M2); open 0; wontfix 0.
- M1 fixed: `.github/actions/install-pwsh/` and the `Install PowerShell` step are gone. Goal, the pwsh Results bullet, and How to validate say the image already has `/usr/bin/pwsh`.
- M2 fixed: Branch base, the Grok Session line, and the Gates preamble record `d7f84ffd`, `0486ecc5`, and `c964078c`. `git merge-base HEAD master` is `5f0d2c7c`.
- No new findings. No targeted command. Gates were not re-run; the fix-pass record covers HEAD after the deletion and the master merge.
- Disposition: clean. Final open count 0. No wontfix.
- Paths: `review/review-framework.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

### 2026-11-16 review round 3

- Roster: general. Effort: 2 (Budget.ByDiff 305 lines). Turn cap 120. Rounds: 3. Round 1 and round 2 files were left as they were.
- Counts (live ledger `review/round-3/merged.md`): bug 0; suggestion 1 open (M3), 1 fixed (M1); nit 1 fixed (M2); wontfix 0.
- M1 still fixed: no `install-pwsh` action or step. The kitchen says the image already has `/usr/bin/pwsh`.
- M2 still fixed: Branch base, the Grok Session line, and the Gates preamble record `d7f84ffd`, `0486ecc5`, and `c964078c`.
- M3 open: `git merge-tree --write-tree master HEAD` exits 1. The only conflict is `task.md` against master `4747af42` (`land kanban 539`). Product files merge clean. Keep this branch's `task.md` when merging master.
- The `script` change matches the CI-red addendum. `RunOnPty` uses util-linux `script` so only stdin is a pty. The tty assertion is unchanged. Recorded gates from the CI-red fix pass cover the test. This review did not re-run build, test, or audit.
- Disposition: not closed. One finding is open, so this is not `clean` or `accepted-exceptions`. Fix pass needed for M3.
- Paths: `review/review-framework.md`, `review/round-3/general.md`, `review/round-3/merged.md`, `review/disposition.md`.

### 2026-11-16 implement-fix-3 (round 3 finding)

- M3 fixed: the two master commits the branch lacked (`4747af42`, `51693a74`, both `land kanban 539`) only carry this task's kitchen and review files, which the branch already had through its own `land task 539` commits. `git merge-tree --write-tree --name-only origin/master HEAD` was clean (tree `6e7ae4a9`), so `git merge origin/master` made `871a7691` with the same tree: the kitchen is the branch side, no content changed. The branch is now 0 behind master. The product delta against master is still `.github/workflows/workflow.yml`, `.github/actions/setup-dotnet/action.yml`, and `routing-33b-json-args-stdin-and-defaults.cs`.

## Learnings

- The routing restatement matches the task. "Move the ci job" means the post-538 `stage` matrix (`build`, `samples`, `test-multi`, `test-standalone`). The required `ci` aggregate stays on `ubuntu-latest`, which the task also requires. That is not a misread.
- Misread, corrected in the round-2 fix pass: the Goal acceptance line used to say the image has no `pwsh` and that each stage extracts PowerShell 7.7.0. The task does not say that. It says to adapt steps that need `sudo` or a tool the image lacks (the named case is zsh). The image base already provides `pwsh`. The line now says that, and there is no install step. The corrected restatement matches the task.
- Round 3: the `script` acceptance line matches the CI-red addendum (do not skip the test; take the pty from `script`; no other wslc stage needs `python3`). That is not a misread.

## CI red on PR #417 (2026-11-16)

`test-multi` failed on the wslc runner (run 43812271591): 1 of 2238 tests.

- Test: `Should_cover_stdin_tty_and_a_builtin_that_does_not_read_stdin`
  (`tests/timewarp-nuru-tests/routing/routing-33b-json-args-stdin-and-defaults.cs:179`).
- Error: `Win32Exception: Failed to start a process with file path 'python3'`.
- Cause: the test starts `python3` to give the child a pty on stdin. The `twe-runner-01`
  image has no `python3` and no `python`. Checked with
  `wslc.exe run --rm twe-runner-01 bash -c 'command -v python3 python script apt-get; id -u'`:
  it prints `/usr/bin/script`, `/usr/bin/apt-get`, and uid `1001`. GitHub-hosted
  `ubuntu-latest` has `python3`, so the test passed there.
- This PR is the first to run that test on wslc, so the failure belongs to this task.

Fix it so the test still runs on the wslc runner. Do not skip it there. Either the
workflow makes `python3` available on the wslc runner, or the test gets its pty from
`script` (present in the image and on ubuntu-latest) and no longer needs python.
Check the other wslc stages for the same dependency.

why: Steven's cockpit 2026-11-16, `ganda pr merge 417` parked on CI red

## Blocked

2026-11-16: review verdict refused: Done with no roster findings. Round folder: /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-539-run-ci-on-the-local-wslc-runner-for-same-repo-push/kanban/in-progress/539-run-ci-on-the-local-wslc-runner-for-same-repo-push-and-pr-events-forks-stay-on-github-hosted/review/round-4. Oracle log: /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-539-run-ci-on-the-local-wslc-runner-for-same-repo-push/kanban/in-progress/539-run-ci-on-the-local-wslc-runner-for-same-repo-push-and-pr-events-forks-stay-on-github-hosted/oracle-review-2-20261116t054405011z.log.
