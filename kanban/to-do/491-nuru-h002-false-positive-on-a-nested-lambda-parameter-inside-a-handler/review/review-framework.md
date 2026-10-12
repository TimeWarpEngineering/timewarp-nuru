# Review framework — task 491

**Date:** 2026-10-12
**Host task:** kanban/to-do/491-nuru-h002-false-positive-on-a-nested-lambda-parameter-inside-a-handler/
**Diff scope:** commit `573541d4` on `task/491-nuru-h002-false-positive-on-a-nested-lambda-parame` (322 lines). Product files: `source/timewarp-nuru-analyzers/validation/handler-validator.cs`, `tests/timewarp-nuru-tests/generator/generator-54-h002-nested-lambda-parameter.cs`, `tests/ci-tests/Directory.Build.props`, `changelog.md`. Surrounding: `tests/ci-tests/run-ci-tests.cs` standalone list, `diagnostic-descriptors.handler.cs` NURU_H002 message, generator-29 and generator-38.
**Plan / brief:** `NURU_H002` must not treat a nested lambda parameter or a handler-local (including pattern variables) as a closure. Real captures, including ones used only inside a nested lambda, must still report. Containment via the semantic model, not name matching.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** Grok `01a12ee7-458f-7b23-9bfe-736891911b4d` (2026-10-12, review oracle)

## Budget (by-diff)

- Lines changed: 322
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
