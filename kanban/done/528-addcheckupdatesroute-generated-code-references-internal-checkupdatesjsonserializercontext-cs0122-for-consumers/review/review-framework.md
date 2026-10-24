# Review framework — task 528

**Date:** 2026-10-24
**Host task:** kanban/in-progress/528-addcheckupdatesroute-generated-code-references-internal-checkupdatesjsonserializercontext-cs0122-for-consumers/
**Diff scope:** branch `task/528-addcheckupdatesroute-generated-code-references-int` vs merge-base `b41bfff3` (master). Implementation commit `f2e18481`. 414 lines changed.
**Plan / brief:** A consumer that calls public `AddCheckUpdatesRoute()` must compile. Generated `FetchGitHubReleasesAsync` named internal `CheckUpdatesJsonSerializerContext` (CS0122). Fix is public `CheckUpdatesGitHubRelease.ParseReleases`; the JSON context stays internal. Purpose/Design regions on the three check-updates files. Regression test compiles a consumer assembly with no InternalsVisibleTo grant. Local-feed consumer probe restores dependencies from nuget.org.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** cursor review oracle, session not reported, max-turns uncapped (review-budget ladder turn cap 120)

## Budget (by-diff)

- Lines changed: 414
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
