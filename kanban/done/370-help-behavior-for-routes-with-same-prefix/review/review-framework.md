# Review framework — task 370

**Date:** 2026-09-29
**Host task:** kanban/to-do/370-help-behavior-for-routes-with-same-prefix/
**Diff scope:** commit `019c7ecd2566` on branch `task/370-help-behavior-for-routes-with-same-prefix` (merge-base with `origin/master` `0f3901890141`). The branch also contains the already-reviewed help-pass merge (task 435); this review covers the shared-prefix help change only.
**Plan / brief:** `deploy --help` lists every route whose leading literal segments equal `deploy`, most specific first, in the existing per-route layout. A single match stays that one route. `deployment`, `deploy status`, and another group's `git deploy` stay on their own help invocations. Fluent `.Map`, `[NuruRoute]` endpoints, and `.WithGroupPrefix` share that check. Shared-prefix help is emitted before the group summary.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
