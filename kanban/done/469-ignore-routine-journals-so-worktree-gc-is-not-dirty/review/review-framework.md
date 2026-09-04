# Review framework — task 469

**Date:** 2026-09-04
**Host task:** kanban/in-progress/469-ignore-routine-journals-so-worktree-gc-is-not-dirty/
**Diff scope:** branch `task/469-ignore-routine-journals-so-worktree-gc-is-not-dirt` vs `origin/master` (commit `93210f43`)
**Plan / brief:** Consumer sweep so `ganda pr merge` / `worktree gc` is not refused as dirty. Root `.gitignore` must ignore `*.journal.json` (ganda 268 one-glob, not the six 262 names). Untrack any leftover `*.journal.json` with `git rm --cached`; do not commit journal contents. Audit check `routine-journals-gitignore` must PASS.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle 2026-09-04 (`ganda task work` review body)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
