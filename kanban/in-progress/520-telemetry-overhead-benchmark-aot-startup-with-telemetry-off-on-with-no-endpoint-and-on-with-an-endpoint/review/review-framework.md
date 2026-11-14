# Review framework — task 520

**Date:** 2026-11-15
**Host task:** kanban/in-progress/520-telemetry-overhead-benchmark-aot-startup-with-telemetry-off-on-with-no-endpoint-and-on-with-an-endpoint/
**Diff scope:** branch `task/520-telemetry-overhead-benchmark-aot-startup-with` vs `master` (three-dot). Product diff is `benchmarks/telemetry-overhead/` plus the audit-scaffolded `.githooks/prepare-commit-msg` runfile. Kanban `task.md` moves with the claim and is not product code.
**Plan / brief:** Repeatable AOT numbers for what `UseTelemetry()` costs one CLI run, in the off, no-endpoint, reachable-endpoint, and closed-port cases, with JIT runs of the off and no-endpoint apps beside task 517. Dated results include mean, standard deviation, min, max, environment, the case 3 grace-delay split, and a CI-stability note. No Nuru product change; defects go to Notes and a filed task. Not added to CI.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** reviewer grok (Grok 4.7), body tw-implementation-review, print mode. Session id not reported. Turn cap 200; cursor session uncapped (no `--max-turns` flag).

## Budget (by-diff)

- Lines changed: 5257
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
