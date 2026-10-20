# Review framework — task 508-007

**Date:** 2026-10-20
**Host task:** kanban/to-do/508-007-generated-di-constructs-singletons-lazily-so-forwarded-calls-and-built-ins-skip-warm-state/
**Diff scope:** branch `task/508-007-generated-di-constructs-singletons-lazily-so-forwa` vs `master` (2 commits, 503 lines). Product diff is the generator emitters, `daemon-05-lazy-singletons.cs`, `generator-35`, the served-app sample, `served-apps.md`, and the changelog.
**Plan / brief:** Source-generated DI constructs singletons on first resolution so a forwarded call, a built-in, and a parse error do not build user singletons. Lifetimes otherwise stay as they are.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** grok `01a159d0-7e82-7272-80f0-af06c514b1c3` (2026-10-20)

## Budget (by-diff)

- Lines changed: 503
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
