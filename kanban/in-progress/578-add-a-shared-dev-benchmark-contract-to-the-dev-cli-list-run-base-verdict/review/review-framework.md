# Review framework — task 578

**Date:** 2026-11-21
**Host task:** kanban/in-progress/578-add-a-shared-dev-benchmark-contract-to-the-dev-cli-list-run-base-verdict/
**Diff scope:** branch `task/578-add-a-shared-dev-benchmark-contract-to-the-dev-cli` vs merge-base `aee2e073` (`master...HEAD`, 4 commits, 29 files, +2078/−263)
**Plan / brief:** Shared `dev benchmark` contract in the DevCli source-only package (`list`, `run --base`, ratio, noise band, verdict), Nuru benchmarks `aot-startup` and `operation-dispatch`, stored absolute baseline removed.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** review grok `01a1fc98-a2d9-7362-abfc-1bd422410c67` (2026-11-21). Implementer session `8843d6fc-e0a0-4adc-97fc-ac45cef640e3`.

## Budget (by-diff)

- Lines changed: 2341
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
