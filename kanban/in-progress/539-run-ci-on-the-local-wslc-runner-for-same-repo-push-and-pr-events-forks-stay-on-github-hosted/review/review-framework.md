# Review framework — task 539

**Date:** 2026-11-15
**Host task:** kanban/in-progress/539-run-ci-on-the-local-wslc-runner-for-same-repo-push-and-pr-events-forks-stay-on-github-hosted/
**Diff scope:** branch `task/539-run-ci-on-the-local-wslc-runner-for-same-repo-push` vs `master` (commit d7f84ffd): `.github/workflows/workflow.yml`, `.github/actions/setup-dotnet/action.yml`, new `.github/actions/install-pwsh/action.yml`, audit-deployed `.githooks/prepare-commit-msg(.cs)`, kanban move to-do → in-progress.
**Plan / brief:** route the `stage` matrix job to the wslc runners (`self-hosted, twe-001, wslc`) for same-repo push and PR events only; forks, release and dispatch stay on `ubuntu-latest`; no `sudo` on wslc; check names and OIDC unchanged.
**Effort:** 3 (Budget.ByDiff: host counted 1032 lines changed; turn cap 200)
**Reviewer roster:** general
**Session IDs:** review oracle, Claude Opus 5.5 (ganda task work, print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 1 re-run (host restart, 2026-11-15)

The first round-1 oracle wrote its files, but the host logged "review kitchen not found" and restarted the review node. The re-run keeps the round-1 files and appends to them; it does not open round 2, because no fix pass ran in between.

- **Diff scope (re-run):** `origin/feature/groom-2026-11-15...HEAD` (HEAD `0486ecc5`). After the first round-1 run, the branch merged `origin/feature/groom-2026-11-15` back in, so the walk's base is the groom branch, not `master`. Product delta is unchanged: `d7f84ffd` (`.github/workflows/workflow.yml`, `.github/actions/setup-dotnet/action.yml`, `.github/actions/install-pwsh/action.yml`) plus kanban files.
- **Budget.ByDiff:** 446 lines changed → effort 2; roster general; turn cap 120. (The first run recorded effort 3 from a 1032-line count against `master`.)
- **Session:** review oracle re-run, Claude Opus 5.5 (ganda task work, print mode). No targeted command run in the re-run.

## Budget (by-diff)

- Lines changed: 7104
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Round 2 (2026-11-16)

Round 1 stays as written. No fix pass landed between round 1 and this review, so M1 and M2 are carried forward. This round does not edit `review/round-1/`.

- **Diff scope:** branch `task/539-run-ci-on-the-local-wslc-runner-for-same-repo-push` (HEAD `aabb1737`) against current `master` (`5f0d2c7c`). `git merge-base --all` returns two bases (`08f89145` and groom tip `7ee4a2d`). `git diff master...HEAD` therefore counts 7104 lines (the groom feature plus the 539 edit). The tree diff `git diff master HEAD` is the CI files, `Directory.Packages.props`, the argv fuzz test, and kanban column drift from being 16 commits behind.
- **Product scope reviewed:** `.github/workflows/workflow.yml`, `.github/actions/setup-dotnet/action.yml`, `.github/actions/install-pwsh/action.yml`, and the kitchen `## Goal` / `## Results` / `### How to validate` / `### Gates` record. The behind-master drift was checked with one merge preview, not by re-reviewing tasks 496, 497, 520, or 594.
- **Effort:** 3. Roster: general. Turn cap: 200.
- **Session:** Grok 4.7, ganda task work review oracle, print mode.
- **Targeted command:** `git merge-tree --write-tree master HEAD`. Reason: the 7104-line three-dot diff looked like this branch would re-ship the groom feature and revert task 594. Exit 0, tree `826d65f3`. No conflicts. The merged tree keeps Amuru `3.0.0-beta.5`, CsCheck `5.0.1`, `Gen.OneOf`, task 496 under `kanban/done/`, and the 539 `runs-on` expression. No full build, test, or audit.
