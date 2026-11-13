# Review framework — task 573

**Date:** 2026-11-14
**Host task:** kanban/in-progress/573-devcli-internal-handler-classes-trip-ca1812-in-consumer-projects/
**Diff scope:** branch `task/573-devcli-internal-handler-classes-trip-ca1812-in` vs merge-base `d3f5856c` (master). Product diff is the generator factory, the `NuruConsumerCheck` CA1812 gate, generator-73, and the consumer-check project. Kanban `task.md` is in the same range and is not product code.
**Plan / brief:** DevCli's nested internal `Handler` types are activated only by the mediator, so a consumer with CA1812 as an error cannot compile the shipped content. Make that instantiation visible in generated code, without suppression and without bringing back CA1034 or CA2000, and compile the content with CA1812 on in `tests/devcli-consumer-check`.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** reviewer grok (Grok 4.7), body tw-implementation-review, print mode. Session id not reported. Turn cap 120; cursor session uncapped (no `--max-turns` flag).

## Budget (by-diff)

- Lines changed: 490
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
