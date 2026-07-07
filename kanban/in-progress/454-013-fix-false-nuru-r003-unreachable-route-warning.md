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

## DECISION (APPROVED by user 2026-07-07 — implement as specified below)

**Adopt a handler-binding rule for boolean flag matching:**

1. A boolean flag **bound** to a handler parameter (or endpoint property) stays
   **optional at match time** — presence binds `true`, absence binds `false`. This is
   the overwhelmingly common case, matches universal CLI convention (flags are
   optional everywhere: getopt, System.CommandLine, clap, cobra), and preserves all
   existing samples/tests.
2. A boolean flag **not bound to anything** is a route DISCRIMINATOR — semantically a
   literal — and becomes **required to match**. The only reason to write
   `.Map("commit --amend").WithHandler(() => ...)` with no bool parameter is to select
   this route when the flag is present. Today such a flag is an always-optional no-op,
   which is clearly never what the author meant.
3. Explicit `--flag?` remains optional regardless (marker keeps its meaning).
4. Value options are untouched (they already respect IsOptional).

**Then** rework the overlap validator to judge shadowing by EFFECTIVE optionality:
- `list --all` (unbound → required) no longer shadows `list {filter?}` → the false
  NURU_R003 disappears for discriminator routes.
- A BOUND optional flag route + flagless route both matching bare input is genuine
  ambiguity → NURU_R003 legitimately still fires.

**Breaking-change surface:** only routes with unbound flags that relied on matching
without the flag — i.e., code depending on a no-op. Expected blast radius ≈ zero;
verify via full CI.

**Implementation notes:** the generator already knows binding (it maps flags to
params); compute effective-required = unbound && !markedOptional, emit the same
`goto route_skip` guard value options use (route-matcher-emitter
EmitFlagParsingWithIndexTracking), flow effective optionality into ComputedSpecificity
/ reduced-signature logic in overlap-validator.cs:397, and fix the display artifact
(short-only option rendering as `--,-bl`). Extend parser-16's e2e test back to the
two-route flag/plain shape as noted above. Update route docs (routing.md,
route-pattern-anatomy.md) with the binding rule.
