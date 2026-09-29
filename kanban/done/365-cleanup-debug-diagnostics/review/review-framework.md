# Review framework — task 365

**Date:** 2026-09-29
**Host task:** kanban/to-do/365-cleanup-debug-diagnostics/
**Diff scope:** branch `task/365-cleanup-debug-diagnostics` vs `origin/master` (merge-base `c464190982e6c7939791ab9e0aea35089e48ccd0`, commit `bf3580c6`)
**Plan / brief:** Remove leftover converter-lookup debug comments from generated route matchers, confirm `nuru-generator.cs` has no leftover debug output, and turn `TreatWarningsAsErrors` back on for tests and samples. Fix the warnings that surfaces, including binding service-diagnostic locations to a syntax tree so `#pragma warning disable NURU056` applies.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
