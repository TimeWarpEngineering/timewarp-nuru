# Review framework — task 470-011

**Date:** 2026-09-23
**Host task:** kanban/to-do/470-011-restrict-repl-history-file-mode/
**Diff scope:** local uncommitted — `repl-history.cs`, `repl-options.cs`, `repl-03b-history-security.cs`, REPL docs (`using-repl-mode.md`, `nuru-app-options.md`), folderized task
**Plan / brief:** Parent 470 M17 (world-readable REPL history) + M32 (ignore-pattern gaps). Owner-only Unix file/dir modes on create/replace; extend default `HistoryIgnorePatterns`; document Windows ACLs and best-effort ignores; assert modes in `repl-03b`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** implementer-cursor / review oracle (ganda task-work)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
