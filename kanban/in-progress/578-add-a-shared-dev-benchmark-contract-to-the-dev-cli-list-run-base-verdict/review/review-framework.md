# Review framework — task 578

**Date:** 2026-11-21
**Host task:** kanban/in-progress/578-add-a-shared-dev-benchmark-contract-to-the-dev-cli-list-run-base-verdict/
**Diff scope:** Round 2 re-reviews the fix delta since round 1 (commit `744cd3a5`, plus attest-retry note `5021e768`), not the whole branch. Round 1 scope remains branch `task/578-add-a-shared-dev-benchmark-contract-to-the-dev-cli` vs merge-base `aee2e073`.
**Plan / brief:** Shared `dev benchmark` contract in the DevCli source-only package (`list`, `run --base`, ratio, noise band, verdict), Nuru benchmarks `aot-startup` and `operation-dispatch`, stored absolute baseline removed. Round 2 checks the M1 fix: ready-made benchmarks opt-in so consumers pass CA1812.
**Effort:** Round 1 was 3 (general). Round 2 is 1 (general).
**Reviewer roster:** general
**Session IDs:** Round 1 review grok `01a1fc98-a2d9-7362-abfc-1bd422410c67` (2026-11-21). Round 2 review grok `01a1fcad-8660-7600-b4cf-c539c04aa481` (2026-11-21). Implementer session `8843d6fc-e0a0-4adc-97fc-ac45cef640e3`.

## Budget (by-diff)

Round 2 re-review budget (host). Round 1 was 2341 lines, effort 3, turn cap 200.

- Lines changed: 172
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Round 2

- **Date:** 2026-11-21
- **Diff scope:** `744cd3a5` (opt-in ready-made benchmarks, readme, changelog, test and dev-cli props, runfile executable bit) and `5021e768` (attest-retry note on `task.md`)
- **Effort:** 1, roster general
- **Session:** grok `01a1fcad-8660-7600-b4cf-c539c04aa481`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
