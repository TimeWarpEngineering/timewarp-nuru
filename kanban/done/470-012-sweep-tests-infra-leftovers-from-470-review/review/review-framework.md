# Review framework — task 470-012

**Date:** 2026-09-23
**Host task:** kanban/to-do/470-012-sweep-tests-infra-leftovers-from-470-review/
**Diff scope:** local uncommitted changes on `task/470-012-sweep-tests-infra-leftovers-from-470-review` vs `origin/master` (M16, M25–M28, M42, M43 sweep)
**Plan / brief:** Sweep tests-infra leftovers from parent 470 review — AOT bench path, recursive search/mcp globs, IVT regen + banner, legacy script delegates, delete engine-01, samples comment + unused Logging GeneratePathProperty
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** implementer-cursor / review oracle (ganda task-work, 2026-09-23)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
