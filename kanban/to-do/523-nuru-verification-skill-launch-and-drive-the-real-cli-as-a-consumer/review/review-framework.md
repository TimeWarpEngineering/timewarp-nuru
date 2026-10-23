# Review framework — task 523

**Date:** 2026-10-23
**Host task:** kanban/to-do/523-nuru-verification-skill-launch-and-drive-the-real-cli-as-a-consumer/
**Diff scope:** branch `task/523-nuru-verification-skill-launch-and-drive-the-real` vs merge-base `8c9a0562` (master). Two commits, 1844 insertions. Product surface is `skills/tw-nuru-verify/` plus task-folder `evidence/`.
**Plan / brief:** Hand-authored consumer verification skill (Launch, Drive, Evidence, Doctor, Cleanup) that installs a shipped TimeWarp.Nuru package and the `nuru` CLI from a private NuGet cache and catches nuru#242, nuru#296, and state#647.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** review oracle session not reported (cursor, grok-4.7). Bound turn cap 200; Cursor has no `--max-turns`, so the session line records the cap as uncapped.

## Budget (by-diff)

- Lines changed: 1850
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/` and the kitchen sections this oracle may update (`task.md` Results, Session, Learnings; `review/`)
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Goal restatement in `task.md` matches the original task. No misread.
