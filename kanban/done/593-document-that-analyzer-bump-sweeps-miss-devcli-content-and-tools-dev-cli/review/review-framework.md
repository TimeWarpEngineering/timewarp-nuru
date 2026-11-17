# Review framework — task 593

**Date:** 2026-11-17
**Host task:** kanban/in-progress/593-document-that-analyzer-bump-sweeps-miss-devcli-content-and-tools-dev-cli/
**Diff scope:** branch `task/593-document-that-analyzer-bump-sweeps-miss-devcli` vs `master` (merge-base `e3e98485`). Product path: `.agent/local/nuru-specific.md` (one bullet under `### Code Style`).
**Plan / brief:** Record, in the shared agent rules, that a slnx-wide `dotnet format` sweep misses `source/timewarp-nuru-devcli/content` and `tools/dev-cli`, with `why: task 588 (2026-11-15)`. Claude memory deletion stays post-merge.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok session `01a1ea22-a229-7dd0-b2ad-b0b61fc1afb3` (2026-11-17). Implementer: claude session `1814d311-7e29-436a-96d1-8ccf9a344c51`.

## Budget (by-diff)

- Lines changed: 105
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
