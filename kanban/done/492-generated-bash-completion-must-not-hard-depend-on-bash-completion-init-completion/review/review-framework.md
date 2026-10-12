# Review framework — task 492

**Date:** 2026-10-12
**Host task:** kanban/to-do/492-generated-bash-completion-must-not-hard-depend-on-bash-completion-init-completion/
**Diff scope:** `task/492-generated-bash-completion-must-not-hard-depend-on` vs `origin/master` (`a6bc5dc1`, `0dfcc6fa`). Product files: `source/timewarp-nuru/completion/completion/templates/bash-completion-dynamic.sh`, `source/timewarp-nuru/completion/completion/templates/zsh-completion-dynamic.zsh`, `tests/timewarp-nuru-tests/completion/completion-30-bare-bash.cs`, `changelog.md`.
**Plan / brief:** Generated bash completion must complete under plain bash. Use `_init_completion` / `_filedir` when bash-completion is loaded; otherwise read `COMP_WORDS` / `COMP_CWORD` and complete files with `compgen`. Check the zsh template for the same assumption. Test under `bash --norc --noprofile` that `app dep` yields `deploy`.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** grok 01a12ecf-4f19-7bd1-827d-594d86c179c0 (2026-10-12)

## Budget (by-diff)

- Lines changed: 215
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
