# Review framework — task 558

**Date:** 2026-10-30
**Host task:** kanban/in-progress/558-devcli-source-content-never-causes-a-consumer-diagnostic-auto-generated-headers-rcs1194-on-review-suppressed-rules/
**Diff scope:** branch `task/558-devcli-source-content-never-causes-a-consumer-diag` vs `master` (merge-base `70e8b068`). Product diff is commit `5fb01122` (39 files, +513/−300). Kitchen land is `88249518`.
**Plan / brief:** DevCli shipped source must pass consumer analyzers on its own. Re-enable RCS1194 and fix every hit, including `PackageFeedUnreadableException`. Classify the other 20 `severity = none` entries as preference (kept, reason on the line) or debt (re-enabled and fixed, or kept off with a follow-up count). No new suppression of DevCli content. `[ganda.audit]` and the package version stay as they are.
**Effort:** 3 (by-diff, 813 lines). Roster is general only.
**Reviewer roster:** general
**Session IDs:** implementer session `b2f8fb14-050b-4a05-b393-97c549e97adf`. This review launch has no vendor session id.

## Budget (by-diff)

- Lines changed: 813
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
