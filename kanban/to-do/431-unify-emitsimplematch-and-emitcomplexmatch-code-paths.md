# Unify EmitSimpleMatch and EmitComplexMatch code paths

## Summary

Eliminate the dual code path in `RouteMatcherEmitter` that has caused at least 5 bugs (#179, #301, #302, #303, #403) due to divergence. Delete `EmitSimpleMatch`, `EmitSimpleAliasMatch`, `BuildListPattern`, and `BuildAliasListPattern`. Route all routes through `EmitComplexMatch` (renamed to `EmitMatch`). This removes ~250 lines of duplicated emitter code and eliminates the entire class of "forgot to add it to both paths" bugs.

## Checklist

### Phase 1: Delete Simple Path Methods
- [ ] Delete `EmitSimpleMatch` method (route-matcher-emitter.cs lines 85-127)
- [ ] Delete `EmitSimpleAliasMatch` method (lines 133-173)
- [ ] Delete `BuildListPattern` method (lines 1186-1222)
- [ ] Delete `BuildAliasListPattern` method (lines 1228-1256)

### Phase 2: Update Route Classification
- [ ] Remove branch at line 57 -- call `EmitComplexMatch` unconditionally
- [ ] Rename `EmitComplexMatch` -> `EmitMatch`
- [ ] Rename `EmitComplexAliasMatch` -> `EmitAliasMatch`

### Phase 3: Verification
- [ ] Run full CI test suite: `dotnet run tests/ci-tests/run-ci-tests.cs`
- [ ] Verify all existing tests pass (no behavior change)
- [ ] Verify issue #179 is fixed by existing tests

### Phase 4: Add Missing Test Coverage
- [ ] Add test: `[NuruRoute("")]` endpoint with `[Parameter]` should show help on `--help`
- [ ] Add test: `[NuruRoute("")]` endpoint with `[Option]` should show help on `--help`  
- [ ] Add test: `[NuruRoute("")]` endpoint executes correctly with empty args `[]`
- [ ] Add test: `[NuruRoute("")]` endpoint executes correctly with valid args

## Notes

### Why This Matters

The dual code path has caused 5 bugs:
- **#301**: `EmitComplexMatch` did not call `EmitTypeConversions()`
- **#302**: Optional positional params generated wrong list pattern (forced routes into complex)
- **#303**: Required options not enforced in route matching
- **#403**: Default route with options intercepts `--help` (skip guard only in complex)
- **#179**: Default route with parameter intercepts `--help` (skip guard missing from simple)

The simple path saves ~15 lines of generated code per simple route but costs ~250 lines of duplicated source generator code and ongoing maintenance burden.

### What Simple Matching Provides

For a route like `greet {name}`:
- Simple: `if (routeArgs is ["greet", var __name_0])`
- Complex: ~15 extra lines with HashSet, List, Array.IndexOf

For a CLI tool, this overhead is irrelevant (route matching is sub-microsecond vs I/O time).

### Risk Assessment

The existing ~500 test suite extensively covers both simple and complex routes. If `EmitComplexMatch` produces the same behavior for simple routes (which it does), all tests pass without modification. Any test failure indicates a real behavioral difference worth investigating.

### References

- Analysis: `.agent/workspace/2026-02-17T11-30-00_gh-issue-179-revised-eliminate-dual-codepath.md`
- Related bugs: #179, #301, #302, #303, #403
- Source: `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
