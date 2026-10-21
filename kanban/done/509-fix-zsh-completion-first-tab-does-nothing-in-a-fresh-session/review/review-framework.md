# Review framework

## Budget (by-diff)

- Lines changed: 306
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 509

**Date:** 2026-10-19
**Host task:** kanban/to-do/509-fix-zsh-completion-first-tab-does-nothing-in-a-fresh-session/
**Diff scope:** branch `task/509-fix-zsh-completion-first-tab-does-nothing-in-a-fre` vs `master` (commits caa55646, ed5dd086)
**Plan / brief:** in the zsh template, call `_{{APP_NAME}}` when `zsh_eval_context[-1]` is `loadautofunc`, and call `compdef` otherwise. Adds the completion-31 zsh test and a CI step that installs zsh.
**Effort:** 2 (budget by-diff); roster general only
**Reviewer roster:** general (review oracle, claude-opus-5-5, single pass)
**Session IDs:** review oracle under ganda task work (headless), session id not exposed

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
