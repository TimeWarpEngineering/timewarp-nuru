# Review framework — task 490

**Date:** 2026-10-12
**Host task:** kanban/to-do/490-fix-migrating-to-30-guide-capabilities-description-to-match-the-flat-endpoints-output/
**Diff scope:** commit `d2abca0f` on `task/490-fix-migrating-to-30-guide-capabilities-description` (this commit only; the branch is stacked on `feature/489-492-launch-gates`, so not `origin/master...HEAD`). Product files: `documentation/user/guides/migrating-to-3.0.md`, `documentation/user/features/agent-invocation.md`.
**Plan / brief:** Rewrite the migrating-to-3.0 `--capabilities` section so it describes the flat `endpoints` list (`pattern`, `groupPath`, `aliases`, `kind`, `parameters[]`, `options[]`) and the `invocation` object. Say what changed versus 2.x. Drop the "package under review" sentence. Make `agent-invocation.md` agree.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok 01a12ef7-fb61-7840-9643-175e64876bf4 (2026-10-12)

## Budget (by-diff)

- Lines changed: 185
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
