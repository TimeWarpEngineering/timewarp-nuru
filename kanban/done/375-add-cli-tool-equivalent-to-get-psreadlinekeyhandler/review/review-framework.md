# Review framework

## Budget (by-diff)

- Lines changed: 1630
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 375

**Date:** 2026-10-03
**Host task:** kanban/to-do/375-add-cli-tool-equivalent-to-get-psreadlinekeyhandler/
**Diff scope:** branch `task/375-add-cli-tool-equivalent-to-get-psreadlinekeyhandle` vs `master` (commit e965b383)
**Plan / brief:** `key-bindings` command (REPL built-in + `tools/nuru-key-bindings`) listing built-in key-binding catalogs PSReadLine-style; see task.md Results.
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (2026-10-03); general reviewer = Claude subagent

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
