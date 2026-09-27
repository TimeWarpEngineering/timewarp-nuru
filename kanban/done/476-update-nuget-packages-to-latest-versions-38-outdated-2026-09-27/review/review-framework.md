# Review framework — task 476

**Date:** 2026-09-28
**Host task:** kanban/to-do/476-update-nuget-packages-to-latest-versions-38-outdated-2026-09-27/
**Diff scope:** branch `task/476-update-nuget-packages-to-latest-versions-38-outdat` vs `origin/master`. Product diff is `Directory.Packages.props` only (`f549d5d3`, `a4fcb5f4`, `acadfd60`, `b1a9a3e0`, `dfc3cee3`).
**Plan / brief:** Bump central packages to latest stable in bisect order (patches, minors, then each major). Keep `Microsoft.CodeAnalysis.CSharp` pinned where raising it would raise the consumer compiler minimum. Keep `Microsoft.Build.Utilities.Core` on the latest stable that still ships `lib/net10.0`. MCP 2.x only if it needs no source changes; Roslynator 5 only if warnings-as-errors stays clean.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
