# Review framework — task 485

**Date:** 2026-10-07
**Host task:** kanban/to-do/485-run-ganda-nuget-outdated-and-update-all-packages-to-latest/
**Diff scope:** branch `task/485-run-ganda-nuget-outdated-and-update-all-packages-t` vs `origin/master` (merge-base `cc310f1a`). Product diff is `Directory.Packages.props` and `changelog.md` (`c8696134`). The kitchen folderize is kanban-only.
**Plan / brief:** Re-run `ganda nuget outdated` and take latest stable. Pin `Microsoft.CodeAnalysis.CSharp` if 5.9.0 would raise the consumer SDK above what TimeWarp repos use. Pin `Microsoft.Build.Utilities.Core` if 18.10.1 would drop the `net10.0` build-task payload. Fix any new Roslynator diagnostics instead of suppressing them. Note consumer-facing dependency changes in `changelog.md` Unreleased.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a11726-522b-73f3-8db9-ecc916953ecb` (2026-10-07)

## Budget (by-diff)

- Lines changed: 153
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
