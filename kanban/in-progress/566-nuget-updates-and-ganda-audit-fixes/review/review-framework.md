# Review framework — task 566

**Date:** 2026-11-01
**Host task:** kanban/in-progress/566-nuget-updates-and-ganda-audit-fixes/
**Diff scope:** branch `task/566-nuget-updates-and-ganda-audit-fixes` vs `master` (`master...HEAD`, 266 files, +1714/−1260). Product commits `9eb4a433`, `47df067e`, `e4743a16`, plus kanban land commits.
**Plan / brief:** Adopt TimeWarp.Mediator 15.0.0-beta.4 with TimeWarp.Contracts 2.0.0-beta.31, clear `ganda repo audit` (`bin/dev` gitignore entries), and bump every other dependency that has a newer version on its current line.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** reviewer session not reported (grok-4.7). Turn cap 200; cursor session uncapped.

## Budget (by-diff)

- Lines changed: 2974
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
