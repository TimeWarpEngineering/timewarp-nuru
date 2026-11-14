# Review framework — task 526

**Date:** 2026-11-14
**Host task:** kanban/in-progress/526-json-member-schema-publish-typed-ids-and-other-parsable-converter-bound-types-as-strings/
**Diff scope:** product commit `b3b7702c` on `task/526-json-member-schema-publish-typed-ids-and-other` (parent `66a3efb8`). The branch also merges `origin/master`; those commits are other landed tasks and are not this review's product diff. Kanban `task.md` in the same commit is kitchen, not product code.
**Plan / brief:** Publish parsable converter-bound types (typed IDs) as `{"type":"string"}` in JSON member schemas, with `"format":"uuid"` when a public `Guid Value` is cheap to see, and check the top-level typed-ID option.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** reviewer grok (Grok 4.7), body tw-implementation-review, print mode. Session id not reported. Turn cap 120; cursor session uncapped (no `--max-turns` flag).

## Budget (by-diff)

- Lines changed: 770
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
