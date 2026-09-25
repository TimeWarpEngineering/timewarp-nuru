# Review framework — task 470-010

**Date:** 2026-09-23
**Host task:** kanban/to-do/470-010-run-generator-17-and-check-version-04-in-ci/
**Diff scope:** local uncommitted — `tests/ci-tests/run-ci-tests.cs`, `tests/ci-tests/Directory.Build.props`, `tests/timewarp-nuru-tests/devcli/check-version-04-endpoint-zero-package.cs`, `tests/timewarp-nuru-tests/generator/generator-19-group-filtering.cs`, `tests/timewarp-nuru-tests/generator/generator-20-parameterized-service-constructor.cs`
**Plan / brief:** Wire CI standalone second phase so generator-17, check-version-04, and generator-19/20 gated cases actually run (parent 470 M14/M15/M29).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task-work tw-implementation-review)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
