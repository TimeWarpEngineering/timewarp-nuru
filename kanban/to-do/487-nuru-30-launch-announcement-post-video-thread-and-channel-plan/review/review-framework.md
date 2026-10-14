# Review framework — task 487

**Date:** 2026-10-09
**Host task:** kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/
**Diff scope:** branch `task/487-nuru-30-launch-announcement-post-video-thread-and` vs `origin/master` (merge-base `215b0426`). 11 files, 481 insertions, 48 deletions (529 lines changed). Uncommitted `review/` is out of the product diff.
**Plan / brief:** task.md — Nuru 3.0 launch assets (blog, blips, release notes, X thread, Show HN / r/dotnet copy, video script, demo app) verified against `TimeWarp.Nuru 3.0.0-beta.79`. No product code. Nothing is posted from this task. Four launch gates stay open and each needs its own task.
**Effort:** 2 (Budget.ByDiff: 529 lines changed; roster axes: general; turn cap 120)
**Reviewer roster:** general
**Session IDs:** review oracle — Grok session `01a11c82-bdbc-75d2-ad87-8d0ebcd6a339` (2026-10-09)

## Budget (by-diff)

- Lines changed: 369
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 3 scope (2026-10-14)

- **Diff scope:** branch vs `origin/master` (merge-base `27ed3d92`), commits `70aec67b` and `94e68fd2`: the beta.80 re-verify pass. 18 files, 273 lines changed. The budget above counts this delta.
- **Plan / brief:** task.md "Re-verify on 3.0.0-beta.80" block: smoke, the three fixed gates, a copy re-read, `shot-2.png` … `shot-6.png`, and a Nostr probe. No product code.
- **Effort / roster:** 2, general.
- **Session IDs:** review oracle — Claude (claude-opus-5-5) under headless `ganda task work`, session id not exposed to the oracle.

## Round 4 scope (2026-10-14)

- **Diff scope:** fix commit `76150f37` (round-3 M7, M8), cross-checked against the full branch vs `origin/master` (merge-base `27ed3d92`). The budget above (369 lines) counts the branch delta.
- **Effort / roster:** 2, general.
- **Session IDs:** review oracle — Claude (claude-opus-5-5) under headless `ganda task work`, session id not exposed to the oracle.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- The four Launch gates already recorded on `task.md` are known pre-publish blockers with their own future tasks. Do not refile them unless the public copy states them incorrectly or would ship a false claim after those gates close.
