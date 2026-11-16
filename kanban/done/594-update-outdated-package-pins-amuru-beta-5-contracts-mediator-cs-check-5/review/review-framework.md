# Review framework — task 594

**Date:** 2026-11-16
**Host task:** kanban/in-progress/594-update-outdated-package-pins-amuru-beta-5-contracts-mediator-cs-check-5/
**Diff scope:** branch `task/594-update-outdated-package-pins-amuru-beta-5` vs merge-base `cbe25e38` (`master`). Product paths: `Directory.Packages.props`, `tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs`. Kitchen move of `task.md` is in the same range.
**Plan / brief:** Bring the six outdated central pins to the versions in the task table (Amuru and Amuru.Tools 3.0.0-beta.5, Contracts 2.0.0-beta.33, Mediator and Mediator.Generators 15.0.0-beta.9, CsCheck 5.0.1). Audit `--fix` for the non-major bumps; hand-bump CsCheck. Keep source and the named runfile sources compiling, and keep the existing tests passing. No new product behavior.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review grok `01a1e240-a847-7340-8fdb-bbaf339f95c4` (2026-11-16). Implement oracle `e7035b14-987c-4e3e-beba-75397f6e601b`. Implementer opencode `ses_e1dd251b3ffeYlN87OdRWte6v4`.

## Budget (by-diff)

- Lines changed: 173
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
