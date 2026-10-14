# Review framework

## Budget (by-diff)

- Lines changed: 345
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-14
**Host task:** kanban/to-do/502-decide-the-timewarpamuru-prerelease-dependency-before-the-stable-300-package/
**Diff scope:** branch `task/502-decide-the-timewarpamuru-prerelease-dependency-bef` vs `master` (`git diff master...HEAD`, through b77df563)
**Plan / brief:** move `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` from 2.0.0-beta.2 to 2.0.0, update the changelog, the migration guide, and the 487 release notes and launch gate #5; 3.0.0 pack has no NU5104.
**Effort:** 1 (general only)
**Reviewer roster:** general (the review oracle, Claude Opus 5.5)
**Session IDs:** review oracle, Claude Opus 5.5 (session id not exposed to the headless oracle)

**Round 2 (2026-10-14):** re-review of fix pass 06acf71a plus the full `master...HEAD` diff. Effort is 2 by diff
size (238 lines). The roster stays `general`, because the host listed only that axis. The reviewer is the review
oracle, Claude Opus 5.5.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo and nuget.org
- Prior rounds are immutable; new work goes in `round-(N+1)/`

**Round 3 (2026-10-14):** re-review of fix pass b3ab7c4c (M3, M4) plus the full `master...HEAD` diff. Effort is 2 by
diff size (345 lines). The roster is `general` (review oracle, Claude Opus 5.5). Outcome: 0 open, disposition `clean`.
