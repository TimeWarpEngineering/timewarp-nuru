# Remove Stale Scratch Files From Repo

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M25).

## Description

Stale scratch/one-off files are committed and orphaned (nothing references them):

- `optimization-results.md` (repo root) — one-off results dump
- `tests/test-status-report.md` — hand-maintained pass/fail table, guaranteed to rot
- `tests/temp-test-chained.cs` — bug-#295 repro scratch at tests/ root, outside any project
- `tests/timewarp-nuru-tests/generator/temp-iconfig-test.cs` — temp test (also referenced
  by committed `internals-visible-to.g.cs` files; regenerate those after removal via
  `scripts/generate-internals-visible-to.cs` — see 454-032)

## Checklist

- [ ] Confirm each file is truly unreferenced (grep + git log)
- [ ] Delete or relocate content worth keeping (e.g. into kanban/documentation)
- [ ] Regenerate internals-visible-to.g.cs if temp tests removed
- [ ] Build + CI tests still green
