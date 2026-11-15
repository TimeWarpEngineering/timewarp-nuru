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
