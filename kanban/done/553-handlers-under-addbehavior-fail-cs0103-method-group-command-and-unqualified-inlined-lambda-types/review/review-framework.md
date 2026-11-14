# Review framework — task 553

**Date:** 2026-11-14
**Host task:** kanban/in-progress/553-handlers-under-addbehavior-fail-cs0103-method-group-command-and-unqualified-inlined-lambda-types/
**Diff scope:** task commits `589eb507..2096c080` (after the origin/master merge). Product files: `lambda-body-qualifier.cs`, `handler-extractor.cs`, `generator-48-async-expression-body-emission.cs`, `generator-74-inlined-lambda-qualified-names.cs`. Kanban `task.md` is in the same range and is not product code. The three-dot diff against local master also contains landed tasks 533, 530, and 575 from that merge; those are out of this review.
**Plan / brief:** Inlined delegate handler bodies must compile inside `TimeWarp.Nuru.Generated`. Qualify caller-scoped names from the SemanticModel. Cover expression and block bodies, with and without `.AddBehavior(...)`, and a handler that takes a DI service. Regression test runs in the CI multi-mode assembly.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** reviewer grok (Grok 4.7), body tw-implementation-review, print mode. Session id not reported. Turn cap 120; cursor session uncapped (no `--max-turns` flag).

## Budget (by-diff)

- Lines changed: 625
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
