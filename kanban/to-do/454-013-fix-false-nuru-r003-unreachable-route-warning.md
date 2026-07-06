# Fix False NURU R003 Unreachable Route Warning

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M10).

## Description

`source/timewarp-nuru-analyzers/validation/overlap-validator.cs:397` — the group is
sorted DESCENDING by `ComputedSpecificity`, so the guard `higher >= lower` is always
true and adds nothing; unreachable-route reporting then hinges only on a shared *reduced*
signature. Distinct, both-reachable routes like `list {filter?}` vs `list --all` (both
reduce to `list`) trigger a false **NURU_R003** unreachable-route warning.

## Requirements

- Rework the overlap check so routes are only reported unreachable when one genuinely
  shadows the other (consider optional params and options as differentiators, not just
  the reduced literal signature).

## Checklist

- [ ] Fix the always-true specificity guard / reduced-signature logic
- [ ] Test: `list {filter?}` + `list --all` → no NURU_R003
- [ ] Test: genuinely shadowed route still reports NURU_R003
- [ ] `ganda runfile cache --clear` + run CI tests
