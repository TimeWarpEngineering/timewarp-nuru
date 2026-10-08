# Review framework — task 487

**Date:** 2026-10-09
**Host task:** kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/
**Diff scope:** branch `task/487-nuru-30-launch-announcement-post-video-thread-and` vs `origin/master` (merge-base `215b0426`). 11 files, 481 insertions, 48 deletions (529 lines changed). Uncommitted `review/` is out of the product diff.
**Plan / brief:** task.md — Nuru 3.0 launch assets (blog, blips, release notes, X thread, Show HN / r/dotnet copy, video script, demo app) verified against `TimeWarp.Nuru 3.0.0-beta.79`. No product code. Nothing is posted from this task. Four launch gates stay open and each needs its own task.
**Effort:** 2 (Budget.ByDiff: 529 lines changed; roster axes: general; turn cap 120)
**Reviewer roster:** general
**Session IDs:** review oracle — Grok session `01a11c82-bdbc-75d2-ad87-8d0ebcd6a339` (2026-10-09)

## Budget (by-diff)

- Lines changed: 529
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
- The four Launch gates already recorded on `task.md` are known pre-publish blockers with their own future tasks. Do not refile them unless the public copy states them incorrectly or would ship a false claim after those gates close.
