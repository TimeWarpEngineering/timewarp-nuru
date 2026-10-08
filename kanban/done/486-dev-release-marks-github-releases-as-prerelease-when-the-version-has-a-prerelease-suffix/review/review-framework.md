# Review framework — task 486

**Date:** 2026-10-08
**Host task:** kanban/to-do/486-dev-release-marks-github-releases-as-prerelease-when-the-version-has-a-prerelease-suffix/
**Diff scope:** branch `task/486-dev-release-marks-github-releases-as-prerelease-wh` vs `origin/master` (merge-base `3b3cc5da`). Product commits `7eaece03` and `184edebb`. Files: `source/timewarp-nuru-devcli/content/any/services/release-guard.cs`, `source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs`, `tests/timewarp-nuru-tests/devcli/release-02-prerelease-flag.cs`, `source/timewarp-nuru-devcli/readme.md`, `documentation/developer/guides/releasing.md`, `changelog.md`.
**Plan / brief:** `dev release` must pass `gh release create --prerelease` when `<Version>` has a SemVer prerelease suffix, and omit it for a stable version. The printed dry-run/recovery command and the executed arguments come from one list. Docs and the Unreleased changelog match. Do not edit existing GitHub Releases.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a11c3f-1285-7b72-a806-f65957b8dd6c` (2026-10-08)

## Budget (by-diff)

- Lines changed: 170
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
