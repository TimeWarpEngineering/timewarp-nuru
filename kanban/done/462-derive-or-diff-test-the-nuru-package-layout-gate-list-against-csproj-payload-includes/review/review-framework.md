# Review framework — task 462

**Date:** 2026-09-24
**Host task:** kanban/in-progress/462-derive-or-diff-test-the-nuru-package-layout-gate-list-against-csproj-payload-includes/
**Diff scope:** branch `task/462-derive-or-diff-test-the-nuru-package-layout-gate-l` vs `master` (product: nupkg-layout-check, workflow-command, timewarp-nuru.csproj, nupkg-layout-02 test)
**Plan / brief:** Diff-test csproj Pack=`true` None items under build/ and analyzers/ against `NuruRequiredPackageEntries`; add analyzer payload to the gate; keep static list on release path (461 fail-closed).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task-work tw-implementation-review)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
