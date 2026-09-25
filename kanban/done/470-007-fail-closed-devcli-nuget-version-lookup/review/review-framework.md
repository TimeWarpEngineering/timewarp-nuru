# Review framework — task 470-007

**Date:** 2026-09-23
**Host task:** kanban/to-do/470-007-fail-closed-devcli-nuget-version-lookup/
**Diff scope:** branch task/470-007-fail-closed-devcli-nuget-version-lookup vs origin/master (merge-base 41f2204e, head b8139f70); product files under source/timewarp-nuru-devcli/content/any/{services,endpoints}, new tests under tests/timewarp-nuru-tests/devcli/check-version-05..07, Directory.Build.props edits
**Plan / brief:** Fail-closed NuGet version lookup (M9), trimmed Version via shared PropsVersionReader (M10), NuGet package id validation + escaped path segment (M31), plus tests. See task.md Description / Requirements / Results.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle = Claude Fable 5.1 via ganda task work (this session); general reviewer = Claude sub-agent spawned from this session

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
