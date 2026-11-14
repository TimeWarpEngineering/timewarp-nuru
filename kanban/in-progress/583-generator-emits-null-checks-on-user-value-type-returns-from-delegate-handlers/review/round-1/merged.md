# Round 1 — merged findings
**Date:** 2026-11-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 1 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: wontfix
- File: source/timewarp-nuru-analyzers/generators/emitters/type-conversion-map.cs:117
- Description: The diff against `master` (`560f013f`) includes task 586's `{name:string}` binding change, which that branch's land commit never put on `master`. The extra files are `type-conversion-map.cs` (`IsStringConstraint`), `route-matcher-emitter.type-conversions.cs:52`, `route-matcher-emitter.option-parsing.cs`, `json-args-emitter.slots.cs`, `handler-invoker-emitter.output.cs:286`, and `tests/timewarp-nuru-tests/routing/routing-38-string-type-constraint.cs`. They ride along from PR #405 (`75b4475f`) via the first parent of merge `203244cb`. `b6c50b72` is the value-kind fix and does not add them. ## Results does not describe this delta. Landing the branch would ship the string-constraint behavior under task 583.
- Suggestion: Replay `b6c50b72` onto `master` so this task's diff is only the value-kind work. Keep task 586's product change off this branch. Update ## Results if any of those files remain.
- Source: general
- Disposition notes: wontfix (fix pass 1, 2026-11-14). The finding uses the wrong base. Task 583 is the second
  task in the work-set `ganda task work-set --ids 586,583,584,585 --into feature/generator-fixes-2026-11-14`.
  The branch was cut from that integration branch's tip `75b4475f` (PR #405 merged into
  `feature/generator-fixes-2026-11-14`, not `master`), and this task's PR targets the integration branch.
  `git diff --stat origin/feature/generator-fixes-2026-11-14...HEAD` lists only the 583 files (the value-kind
  model, extractors, emitters, generator-75, routing-39, and the kanban move). None of 586's files
  (`type-conversion-map.cs` `IsStringConstraint`, routing-38, and the others) appear there. Replaying `b6c50b72` onto `master`
  would break the stack. 586's change reaches `master` through the work-set's master PR, under task 586.

## Duplicates / conflicts

- One finding. general Issue 1 is M1.
