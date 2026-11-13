# Review framework — task 574

**Date:** 2026-11-14
**Host task:** kanban/in-progress/574-collect-ci-code-coverage-through-jaribu-runfile-coverage-mode/
**Diff scope:** branch `task/574-collect-ci-code-coverage-through-jaribu-runfile` vs `master` (product commit `53b7427a`). Files: `.github/workflows/workflow.yml`, `Directory.Packages.props`, `tests/ci-standalone/run-standalone-tests.cs`, `tests/ci-tests/run-ci-tests.cs`, `tools/dev-cli/endpoints/test-command.cs`, `tools/dev-cli/endpoints/workflow-command.cs`.
**Plan / brief:** Adopt Jaribu 1.0.0-beta.16 runfile coverage so CI test legs write Cobertura under `artifacts/coverage` and upload it. No `ci-code-coverage.min-line`.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** reviewer-cursor, session not reported

## Budget (by-diff)

- Lines changed: 282
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
