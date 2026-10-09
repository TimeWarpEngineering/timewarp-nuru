# Review framework — task 494

**Date:** 2026-10-09
**Host task:** kanban/in-progress/494-update-all-nuget-packages-to-latest-incl-timewarpamuru-200-beta2/
**Diff scope:** branch task/494-update-all-nuget-packages-to-latest-incl-timewarpa vs master (514df40b)
**Plan / brief:** Bump central package pins to newest (Amuru 2.0.0-beta.2, Shouldly 5.0.0-preview.2, BenchmarkDotNet 0.16.0-preview.2); fix breaking changes.
**Effort:** 1 (general only) — Budget.ByDiff: 93 lines changed → effort 1; roster axes: general; turn cap 80
**Reviewer roster:** general
**Session IDs:** review oracle (ganda task work, Claude Opus 5.5, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
