# Round 4 — merged findings
**Date:** 2026-11-20
**Sources:** general
**Scope:** restart pass `8fceec27..61bb293f` (merge `06f35a7a` of the 591 feature branch); carries M1–M6.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 4 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 1 | 1 | 0 |

## Resolved prior

- M1–M3 (bug), M4 (suggestion), M5 (nit), M6 (bug): fixed in earlier rounds. The merge keeps them.
  M7 below narrows the repeated-option rule that the M1–M5 pass left in place.

## Issues

### M7 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.option-parsing.cs:234
- Description: Greedy repeated-option consumption reserves nothing for a required positional. These routes
  now fail with `Unknown command`: `process --id {id:int}* --tag {t}* {script}` with `--tag A run.sh`, which
  was the routing-09 case before the merge changed its input to `--tag=A`;
  `docker build --build-arg {args}* {path}`, in `documentation/developer/design/parser/syntax-rules.md:393`
  and `resolver/specificity-algorithm.md:25`; and the `fluent-docker-run` sample
  (`samples/fluent/03-syntax/fluent-syntax-examples.cs:147`). `routing-test-plan.md:443-447` still expects
  the old match. Original item 3 called a leak into an *optional* parameter the defect. The restatement
  widened the fix so it also swallows a *required* positional. Editing routing-09 hides that change while
  the test plan, the design docs, and the sample still state the old contract.
- Suggestion: Give trailing greedy values back to required positionals that are still unbound (an optional
  parameter reserves nothing, so `--env A B {name?}` still binds both). Restore routing-09 to
  `--tag A run.sh`. Add fluent and endpoint cases to the binding-strictness runfile. Narrow the "swallowed"
  wording in the migration guide (line 226) and the skill (line 322) to an optional positional. If the
  operator keeps pure greedy consumption instead, update the test plan, both design docs, and the sample, and
  record the contract change.
- Source: general
- Disposition notes:

### M8 — Severity: nit — Status: open
- File: tests/timewarp-nuru-tests/routing/routing-43-binding-strictness.cs:1
- Description: `routing-43` is used twice, here and by `routing-43-cli-failures.cs` from 591-005.
- Suggestion: Rename this file to `routing-45-binding-strictness.cs` and update the path in `## Results`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer; none.
