# GitHub Issue #179 (Revised): Eliminate the Simple/Complex Dual Code Path

**Date:** 2026-02-17
**Issue:** [#179](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/179)
**Status:** Open
**Priority:** High
**Supersedes:** `2026-02-17T10-30-00_gh-issue-179-endpoint-default-route-help-analysis.md`

---

## Executive Summary

Issue #179 (`[NuruRoute("")]` intercepting `--help`) is the latest symptom of a systemic design problem: the `RouteMatcherEmitter` maintains two parallel code paths (`EmitSimpleMatch` and `EmitComplexMatch`) that must be kept in sync. At least **5 bugs** (#179, #301, #302, #303, #403) trace directly to these paths diverging. The recommended fix is to eliminate `EmitSimpleMatch` entirely and route everything through `EmitComplexMatch`.

---

## Critique of the Original Analysis

The original analysis correctly identified the root cause (skip guard in `EmitComplexMatch` but not `EmitSimpleMatch`) and the missing test coverage. However, it had two weaknesses:

1. **Analysis got lost in the weeds.** Sections 4-8 wander through increasingly uncertain hypotheses ("SHOULD be emitted", "wait - let me re-check"), contradicting earlier conclusions. The analysis should have committed to a clear root cause earlier.

2. **Recommended fix was too narrow.** "Option B" (force empty routes to complex matching) patches this one bug but does nothing to prevent the next divergence bug. The real question -- which the analysis failed to ask -- is whether the dual code path is worth maintaining at all.

---

## The Dual Code Path Problem

### Architecture

`RouteMatcherEmitter.Emit()` (line 57) branches on route complexity:

```
route.HasOptions || route.HasCatchAll || route.HasOptionalPositionalParams
  ? EmitComplexMatch()    (lines 182-324)  -- goto-based, two-pass
  : EmitSimpleMatch()     (lines 85-127)   -- C# list pattern
```

Each path also has an alias variant, giving **4 parallel methods** (+ 2 helper methods unique to each):

| Method | Lines | Purpose |
|--------|-------|---------|
| `EmitSimpleMatch` | 85-127 | List pattern matching for simple routes |
| `EmitSimpleAliasMatch` | 133-173 | Alias variant of simple matching |
| `EmitComplexMatch` | 182-324 | Two-pass matching for routes with options/catch-all |
| `EmitComplexAliasMatch` | 330-443 | Alias variant of complex matching |
| `BuildListPattern` | 1186-1222 | Used only by simple path |
| `BuildAliasListPattern` | 1228-1256 | Used only by simple alias path |

The shared tail (type conversions, behavior pipeline, handler invocation) is identical. The divergence is in how routes are matched and how parameters are extracted.

### What Simple Match Buys You

For a route like `greet {name}`, `EmitSimpleMatch` generates:

```csharp
if (routeArgs is ["greet", var __name_0])
{
  string name = __name_0;
  // handler invocation
  return 0;
}
```

For the same route, `EmitComplexMatch` generates:

```csharp
if (routeArgs.Length >= 2)
{
  HashSet<string> __optionForms_0 = [];
  int __endOfOptions_0 = Array.IndexOf(routeArgs, "--");
  if (__endOfOptions_0 < 0) __endOfOptions_0 = routeArgs.Length;
  HashSet<int> __consumed_0 = [];
  List<string> __positionalList_0 = [];
  for (int __i = 0; __i < routeArgs.Length; __i++)
  {
    if (__i == __endOfOptions_0 && routeArgs[__i] == "--") continue;
    if (__consumed_0.Contains(__i)) continue;
    __positionalList_0.Add(routeArgs[__i]);
  }
  string[] __positionalArgs_0 = [.. __positionalList_0];
  if (__positionalArgs_0.Length < 2) goto route_skip_0;
  if (__positionalArgs_0[0] != "greet") goto route_skip_0;
  string name = __positionalArgs_0[1];
  // handler invocation
  return 0;
}
route_skip_0:;
```

**What you gain from simple matching:**
- ~15 fewer lines of generated code per route
- No `HashSet`, `List`, or `Array.IndexOf` allocation per route attempt
- C# pattern matching instead of index arithmetic

**What you pay for it:**
- 4 extra methods in the emitter (~250 lines of source generator code)
- 2 extra helper methods (`BuildListPattern`, `BuildAliasListPattern`)
- Every behavioral concern must be duplicated (or forgotten) across both paths
- Every bug fix must be applied twice (or forgotten)

### The Bug History

| Bug | What happened | Root cause |
|-----|---------------|------------|
| **#301** | `EmitComplexMatch` did not call `EmitTypeConversions()` | Feature added to simple path, forgotten in complex path |
| **#302** | Optional positional params generated wrong list pattern | Simple path can't express optional elements, forced routes into complex path |
| **#303** | Required options not enforced in route matching | Validation only in complex path |
| **#403** | Default route `Map("")` with options intercepts `--help` | Skip guard added to complex path only |
| **#179** | Default route `[NuruRoute("")]` with parameter intercepts `--help` | Skip guard missing from simple path entirely |

The pattern is clear: **every time a concern is added to one path, it gets missed in the other**. Bug #302 is particularly telling -- the "optimization" of having a simple path created a bug, and the fix was to push more routes into the complex path anyway.

### Can Complex Handle Simple Routes Correctly?

Yes. `EmitComplexMatch` is a strict superset of `EmitSimpleMatch`:

- **Routes with no options:** The option forms set is empty (`[]`), no options are parsed, no indices are consumed. The positional array construction loop simply copies `routeArgs` as-is (minus any `--` separator).
- **Routes with no catch-all/optional params:** The segment matching falls through to required parameter extraction only.
- **Routes with only literals and required params:** All the complex machinery (HashSet, consumed indices, etc.) is overhead but produces the exact same matching result.

The only difference is generated code size and runtime allocation.

### Performance Impact of Unification

The extra overhead per route match attempt in complex mode for a simple route:
1. Empty `HashSet<string>` allocation for option forms
2. `Array.IndexOf` for `--` (linear scan, typically returns -1 immediately)
3. Empty `HashSet<int>` allocation for consumed indices
4. `List<string>` allocation + copy loop for positional args
5. One `string[]` allocation from the list

For a CLI tool, this overhead is **irrelevant**. Route matching happens once per invocation. The entire matching phase is sub-microsecond compared to the I/O the handler will do.

If a future benchmark shows this matters (it won't), the optimization can be reintroduced as a code generation optimization within a single `EmitMatch` method (emit the list-pattern shortcut *before* the complex fallback), rather than as a separate code path with separate concerns.

---

## Recommended Fix

### Primary: Eliminate `EmitSimpleMatch`

1. **Delete** `EmitSimpleMatch`, `EmitSimpleAliasMatch`, `BuildListPattern`, and `BuildAliasListPattern`
2. **Remove** the branching condition at line 57 -- all routes go through `EmitComplexMatch`/`EmitComplexAliasMatch`
3. **Rename** `EmitComplexMatch` -> `EmitMatch` and `EmitComplexAliasMatch` -> `EmitAliasMatch`

This:
- Fixes #179 immediately (the skip guard in complex match already handles empty default routes)
- Removes ~250 lines from `route-matcher-emitter.cs` (currently 1678 lines)
- Eliminates the entire class of "forgot to add it to both paths" bugs
- Reduces cognitive load for anyone modifying the emitter

### Secondary: Add Test Coverage

Regardless of the approach chosen:

- [ ] Add test: `[NuruRoute("")]` endpoint with `[Parameter]` should show help on `--help`
- [ ] Add test: `[NuruRoute("")]` endpoint with `[Option]` should show help on `--help`
- [ ] Add test: `[NuruRoute("")]` endpoint executes correctly with empty args `[]`
- [ ] Add test: `[NuruRoute("")]` endpoint executes correctly with valid args

### If Unification Is Too Risky Right Now

If you want the minimal fix for #179 without the refactor, the safest one-line change is at line 57:

```csharp
// Before
if (route.HasOptions || route.HasCatchAll || route.HasOptionalPositionalParams)

// After - also force empty/default routes through complex matching
bool isDefaultRoute = string.IsNullOrEmpty(route.OriginalPattern) &&
                      string.IsNullOrEmpty(route.GroupPrefix);
if (route.HasOptions || route.HasCatchAll || route.HasOptionalPositionalParams || isDefaultRoute)
```

This patches #179 but does not prevent the next divergence bug.

---

## Implementation Checklist

### Full unification (recommended)

- [ ] Delete `EmitSimpleMatch` (lines 85-127)
- [ ] Delete `EmitSimpleAliasMatch` (lines 133-173)
- [ ] Delete `BuildListPattern` (lines 1186-1222)
- [ ] Delete `BuildAliasListPattern` (lines 1228-1256)
- [ ] Remove branch at line 57 -- call `EmitComplexMatch` unconditionally
- [ ] Rename `EmitComplexMatch` -> `EmitMatch`
- [ ] Rename `EmitComplexAliasMatch` -> `EmitAliasMatch`
- [ ] Run full CI test suite (`dotnet run tests/ci-tests/run-ci-tests.cs`)
- [ ] Verify all existing tests pass (no behavior change for any route)
- [ ] Add endpoint default route tests (see above)

### Risk mitigation

The existing test suite (~500 tests) covers both simple and complex route matching extensively. If `EmitComplexMatch` produces the same behavior for simple routes (which it does by design), all tests should pass without modification. Any test failure would indicate a real behavioral difference worth investigating.

---

## References

- **GitHub Issue:** [#179](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/179)
- **Related Bugs:** #301, #302, #303, #403
- **Source File:** `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
  - Branch point: line 57
  - Simple match: lines 85-127, 133-173, 1186-1256
  - Complex match: lines 182-443
  - Skip guard: lines 202-213
- **Existing Tests:** `tests/timewarp-nuru-tests/help/help-02-default-route-help.cs`
