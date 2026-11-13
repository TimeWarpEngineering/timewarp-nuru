# Review framework — task 581

**Date:** 2026-11-13
**Host task:** kanban/in-progress/581-split-c-files-over-the-500-line-cap-and-restore-twa0038/
**Diff scope:** branch `task/581-split-c-files-over-the-500-line-cap-and-restore` vs `master` (`master...HEAD`). Product change is the 500-line split plus the SourceGenerators beta.15 bump. 144 files, +24473/−22588.
**Plan / brief:** Split every C# file over 500 lines in `source/`, `tools/`, and `tests/`, then restore TWA0038 (500-line cap) at the package default.
**Effort:** 3 (general only; by-diff budget, not extra specialists)
**Reviewer roster:** general
**Session IDs:** review session id not exposed (print mode). Turn cap 200.

## Budget (by-diff)

- Lines changed: 47061
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
- Build, test, and audit stay on the implementer's recorded Results. This round did not run `./bin/dev build`, `./bin/dev test`, `ganda repo audit`, or the git hooks.
