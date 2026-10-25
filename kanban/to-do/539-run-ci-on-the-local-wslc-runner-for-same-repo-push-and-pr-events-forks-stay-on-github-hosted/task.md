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

## Checklist

- [ ] Confirm runner capacity and the runner-group public-repo setting (timewarp-infrastructure 004)
- [ ] Confirm fork-PR approval setting with Steven; record it
- [ ] Bake or adapt tools the image lacks (zsh, any `sudo apt` steps)
- [ ] `runs-on` expression: same-repo push/PR to `wslc`, forks to `ubuntu-latest`
- [ ] Prove: a same-repo PR runs on `twe-runner-01-*`; a fork PR (or a dry evaluation of the expression) stays hosted
- [ ] Record before/after wall time and queue time
- [ ] Open a PR with run links as proof

## Notes

- Related nuru tasks: 537 (path filters), 538 (parallel jobs). Not a hard depends-on edge; ordering vs 538 is decided by the capacity check.
- Cross-repo: timewarp-infrastructure 004 (runner capacity, wslc targeting, image tools). timewarp-architecture has the matching task.
- Steven's yes before any workflow edit lands. The nuru repo bot owns the edit.
- WSLC runner detail is from Akili's read of the repos and job API (intel #75); Busara did not verify it.

## Session

- Created: 985504 (2026-10-25)

## Results
