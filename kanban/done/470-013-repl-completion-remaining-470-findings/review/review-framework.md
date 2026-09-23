# Review framework — task 470-013

**Date:** 2026-09-23
**Host task:** kanban/to-do/470-013-repl-completion-remaining-470-findings/
**Diff scope:** local uncommitted — REPL key-binding profiles (Emacs/Vi/VSCode Shift+Enter), bash/zsh/pwsh dynamic completion templates, `DynamicCompletionScriptGenerator`, plus completion-20 / repl-23 / repl-41 tests
**Plan / brief:** Close parent-470 leftovers M18, M19, M37, M44 (Shift+Enter on all profiles; quoting-safe bash COMPREPLY; remove zsh numeric exit-code strip; escape pwsh APP_PATH)
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task-work print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
