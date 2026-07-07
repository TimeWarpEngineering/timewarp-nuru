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

- [x] Fix the always-true specificity guard / reduced-signature logic
- [x] Test: `list {filter?}` + `list --all` → no NURU_R003
- [x] Test: genuinely shadowed route still reports NURU_R003
- [x] `ganda runfile cache --clear` + run CI tests

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

## Notes

### Implementation Plan (2026-07-07)

#### Decision: effective-required = unbound && !markedOptional
- Bound flag → optional (presence=true, absence=false)
- Unbound flag → required discriminator
- `--flag?` → always optional regardless

#### Step 1: Add IsFlagBound helper to OptionDefinition
File: `source/timewarp-nuru-analyzers/generators/models/segment-definition.cs`
- Add `public bool IsFlagBound(RouteDefinition route)` method to `OptionDefinition`
- Checks `route.Handler.Parameters` for `ParameterBinding` with `Source == BindingSource.Flag` and `SourceName` matching `LongForm ?? ShortForm`

#### Step 2: Emit route_skip guard for effectively-required flags
File: `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- `EmitFlagParsingWithIndexTracking`: add `RouteDefinition route` parameter, update call site
- After the scan loop, emit `if (!{varName}) goto route_skip_{routeIndex};` when `!option.IsOptional && !option.IsFlagBound(route)`

#### Step 3: Include effectively-required flags in required signature
File: `source/timewarp-nuru-analyzers/validation/overlap-validator.cs`
- `ComputeRequiredSignature`: add case `option.IsFlag && !option.IsOptional && !option.IsFlagBound(route)` → include in signature
- Update outdated comment ("boolean flags - always optional at runtime" → "bound boolean flags - optional at match time")

#### Step 4: Fix PatternSyntax null-LongForm bug
File: `source/timewarp-nuru-analyzers/generators/models/segment-definition.cs`
- `PatternSyntax`: use tuple switch `(LongForm, ShortForm)` to handle null LongForm correctly (was producing `--,-bl`)

#### Step 5: Extend parser-16 test
File: `tests/timewarp-nuru-tests/parser/parser-16-multi-char-short-options.cs`
- Update comment on existing bound-flag test
- Add `Should_route_unbound_short_flag_as_discriminator_two_routes` — two routes (plain + flag), unbound flag is discriminator

#### Step 6: New tests
- `tests/timewarp-nuru-tests/routing/routing-31-unbound-flag-discriminator.cs` (CI-eligible e2e) — `list --all` + `list {filter?}` → no NURU_R003 (app compiles under TreatWarningsAsErrors)
- `tests/timewarp-nuru-tests/generator/generator-30-nuru-r003-overlap.cs` (standalone, generator-hosted) — asserts no R003 for discriminator + R003 still fires for genuine shadow (bound flag + flagless)
- Add `generator-30` to CiTestExcludes (CS0433 collision, same as generator-28)
- Regenerate `internals-visible-to.g.cs` for routing-31

#### Step 7: Doc updates
- `documentation/user/features/routing.md` — add "Boolean Flag Binding" subsection
- `documentation/developer/design/parser/route-pattern-anatomy.md` — correct "always optional" claim in §6.1

#### Step 8: Verify
1. `ganda runfile cache --clear` (generator code changed)
2. `dotnet run tests/ci-tests/run-ci-tests.cs` (full CI — no regressions, routing-31 passes)
3. `dotnet run tests/timewarp-nuru-tests/parser/parser-16-multi-char-short-options.cs` (standalone)
4. `dotnet run tests/timewarp-nuru-tests/generator/generator-30-nuru-r003-overlap.cs` (standalone)
5. Regenerate internals-visible-to.g.cs
6. Build analyzer + main library clean

#### Files touched
- Edit: segment-definition.cs (IsFlagBound + PatternSyntax fix)
- Edit: route-matcher-emitter.cs (route param + skip guard)
- Edit: overlap-validator.cs (required signature + comments)
- Edit: parser-16-multi-char-short-options.cs (comment + new test)
- Create: routing-31-unbound-flag-discriminator.cs
- Create: generator-30-nuru-r003-overlap.cs
- Edit: tests/ci-tests/Directory.Build.props (CiTestExcludes)
- Regenerate: internals-visible-to.g.cs
- Edit: routing.md, route-pattern-anatomy.md

#### Risk assessment
- Breaking change: only routes with unbound flags that relied on matching without the flag (no-op). Expected blast radius ≈ zero.
- CI count must increase by the routing-31 test count (the 454-023 lesson).
- generator-30 is standalone (excluded from CI multi-mode) — run standalone.

## Results

### What was implemented

Adopted a handler-binding rule for boolean flag matching: bound flags stay optional (presence=true, absence=false), unbound flags become required discriminators, explicit `--flag?` stays optional regardless. This eliminates the false NURU_R003 for `list --all` + `list {filter?}` while preserving genuine shadow detection.

- **IsFlagBound helper**: Added `OptionDefinition.IsFlagBound(RouteDefinition route)` — checks `route.Handler.Parameters` for a `ParameterBinding` with `Source == BindingSource.Flag` and `SourceName` matching `LongForm ?? ShortForm`.
- **Matcher guard**: `EmitFlagParsingWithIndexTracking` now emits `if (!{varName}) goto route_skip_{routeIndex};` when `!option.IsOptional && !option.IsFlagBound(route)` — effectively-required flags must be present to match. Mirrors the value-option guard pattern.
- **Required signature**: `ComputeRequiredSignature` now includes effectively-required flags (`option.IsFlag && !option.IsOptional && !option.IsFlagBound(route)`), so `list --all` reduces to `list --all` and no longer groups with `list {filter?}` (which reduces to `list`).
- **PatternSyntax fix**: Short-only options no longer render as `--,-bl` — tuple switch handles null LongForm correctly, producing `-bl`.
- **Tests**: Extended parser-16 to two-route discriminator shape, added routing-31 (CI e2e), added generator-30 (standalone generator-hosted R003 assertion in both directions).
- **Docs**: Updated routing.md and route-pattern-anatomy.md with the binding rule.

### Files changed

- `source/timewarp-nuru-analyzers/generators/models/segment-definition.cs` — `IsFlagBound` method + `PatternSyntax` tuple switch
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` — `route` param + skip guard for effectively-required flags
- `source/timewarp-nuru-analyzers/validation/overlap-validator.cs` — effectively-required flag case in `ComputeRequiredSignature` + updated comments
- `tests/timewarp-nuru-tests/parser/parser-16-multi-char-short-options.cs` — updated comment + new two-route discriminator test
- `tests/timewarp-nuru-tests/routing/routing-31-unbound-flag-discriminator.cs` (new) — CI e2e test
- `tests/timewarp-nuru-tests/generator/generator-30-nuru-r003-overlap.cs` (new) — standalone generator-hosted R003 assertion
- `tests/ci-tests/Directory.Build.props` — added generator-30 to CiTestExcludes
- `tests/ci-tests/run-ci-tests.cs` — added generator-30 to standalone runner list
- `source/timewarp-nuru/internals-visible-to.g.cs` — regenerated for routing-31
- `documentation/user/features/routing.md` — added "Boolean Flag Binding" subsection
- `documentation/developer/design/parser/route-pattern-anatomy.md` — corrected "always optional" claim

### Key decisions made

- **IsFlagBound as instance method on OptionDefinition**: Takes `RouteDefinition` parameter. Used by both the emitter and validator (same project, same namespace). `ArgumentNullException.ThrowIfNull(route)` for CA1062.
- **Generator-hosted test pattern**: `OverlapValidator` can't be unit-tested directly (analyzer internals not visible to CI, CS0433 collision if analyzer referenced as library). Used the generator-28 pattern: host `NuruGenerator` in `CSharpGeneratorDriver`, read diagnostics from `GeneratorDriverRunResult.Diagnostics`. Excluded from CI multi-mode, added to standalone runner list.
- **RunAsync in generator test source**: The in-memory test source must call `RunAsync` — without it, `AppExtractor` doesn't build an `AppModel` and validation (including NURU_R003) never runs.
- **No change to CheckForUnreachableRoutes**: The `higher >= lower` guard is redundant given descending sort but harmless. The real fix is in `ComputeRequiredSignature` — once effectively-required flags are in the signature, the grouping separates discriminator routes from plain routes.

### Test outcomes

- **parser-16 standalone**: 7 passed, 0 failed (including new two-route discriminator test)
- **generator-30 standalone**: 2 passed, 0 failed (no-R003 for discriminator + R003 for genuine shadow)
- **routing-31 standalone**: 1 passed, 0 failed
- **Full CI** (`dotnet run tests/ci-tests/run-ci-tests.cs`): 1376 passed, 7 skipped, 0 failed. CI count increased by 2 (from 1374 to 1376) — the two new multi-mode tests (parser-16 + routing-31). No regressions.
