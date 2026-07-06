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

## Concrete repro (found during 454-005)

Two routes in one app:
```
.Map("p16-build -bl").WithHandler(() => "binary-log-on").AsQuery().Done()
.Map("p16-build").WithHandler(() => "plain-build").AsQuery().Done()
```
fails the BUILD with:
`error NURU_R003: Route 'p16-build' is unreachable. Route 'p16-build --,-bl' ... will
match all the same inputs with equal or higher specificity (1075 vs 1000 points).`

IMPORTANT nuance (verified in the generated matcher, route-matcher-emitter.cs
EmitFlagParsingWithIndexTracking): BOOLEAN flags never check IsOptional — the route
matches with or without the flag (flag binds false when absent). So the R003 report
above is CONSISTENT with current matcher semantics: 'p16-build -bl' really does match
bare 'p16-build'. The task is therefore two-sided:
1. Decide the semantics: should an unmarked boolean flag be REQUIRED to match (with
   `--flag?` opting into optional), or stay always-optional? Value options already
   respect IsOptional; flags do not — the asymmetry is the root problem.
2. Then make the overlap validator agree with whichever semantics wins, so reports
   like `list {filter?}` vs `list --all` are judged correctly.
Also note the display artifact
`'p16-build --,-bl'` — a short-only option renders with an empty long form; fix the
route display string while in here. When fixed, extend
`tests/timewarp-nuru-tests/parser/parser-16-multi-char-short-options.cs`
(`Should_route_multi_char_short_end_to_end`) back to the two-route flag/plain shape.
