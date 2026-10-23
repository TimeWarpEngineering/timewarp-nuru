# Review framework — task 522

**Date:** 2026-10-23
**Host task:** kanban/to-do/522-analyzer-flag-discoverendpoints-in-a-compilation-that-builds-more-than-one-app/
**Diff scope:** branch `task/522-analyzer-flag-discoverendpoints-in-a-compilation-t` vs merge-base `380ace11` (local master). Product diff is commits `cb2b3094`, `c53167dd`, `0acc83c1` (561 lines in the budget count; 16 files, +546/−15).
**Plan / brief:** NURU_A003 warns on unfiltered `DiscoverEndpoints()` when one compilation builds Nuru apps in more than one source file. Prove the sites before any test conversion. Temporary CI `NoWarn` for this id only.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** not reported (print mode)

## Budget (by-diff)

- Lines changed: 745
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 2 scope

- Branch vs `master` `b5958cd7` after merging `origin/master` (`a5c339bb`, task 521) and renumbering the test to generator-64 (`d505a786`). Product diff: 15 files, +396/−9. Roster general, effort 2.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
