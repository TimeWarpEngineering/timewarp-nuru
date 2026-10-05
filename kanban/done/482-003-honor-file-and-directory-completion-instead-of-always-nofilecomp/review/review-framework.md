# Review framework — task 482-003

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-003-honor-file-and-directory-completion-instead-of-always-nofilecomp/
**Diff scope:** commit `f814c86d` on `task/482-003-honor-file-and-directory-completion-instead-of-alw` (not the stacked branch vs `origin/master`). Product files: `completion-candidate.cs`, `dynamic-completion-handler.cs`, `icompletion-source.cs`, `templates/bash-completion-dynamic.sh`, `templates/fish-completion-dynamic.fish`, `templates/zsh-completion-dynamic.zsh`, `tests/timewarp-nuru-tests/completion/completion-29-directive.cs`, `tests/timewarp-nuru-tests/repl/repl-41-lowsev-sweep.cs`.
**Plan / brief:** Finding R-4 from task 482. `DynamicCompletionHandler` must not force `NoFileComp` when a `File` or `Directory` candidate is present, candidate `NoSpace` / `KeepOrder` flags must reach the directive line, and zsh must keep a spaced suggestion as one word. Parent record: `kanban/done/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/runtime-core.md` R-4.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** grok 01a10bc9-6abd-7f80-b644-2541da60e83c (2026-10-05)

## Budget (by-diff)

- Lines changed: 395
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
