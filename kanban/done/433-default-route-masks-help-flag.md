# Default route masks --help flag

## Description

When a CLI app has a default route (e.g., `getleads electricians`), the `--help` flag is being intercepted by the default route handler instead of displaying help.

Current behavior:
```
$ getleads --help
Searching for '--help'...
Found 3 leads.
Results saved to: leads.csv
```

Expected behavior:
```
$ getleads --help
Usage: getleads [search-term]
Options:
  --help    Show help information
```

## Checklist

- [ ] Investigate how route matching prioritizes default routes over built-in flags
- [ ] Determine if `--help` should be handled before route matching
- [ ] Implement fix to ensure built-in flags are processed before default route
- [ ] Add test cases for `--help` with default routes
- [ ] Verify `--version` also works correctly with default routes

## Notes

Issue observed in TimeWarp.Parade's `getleads` command which uses a default route pattern like:
```csharp
.Map("{searchTerm}").WithHandler((string searchTerm) => ...)
.AsCommand().Done()
```

The `--help` argument is being treated as a literal search term instead of triggering the help system.

## Implementation Plan

### Current State Analysis

**Root Cause Location**: `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:112-123`

The existing fix for Issue #179 handles routes with **no literals** (empty patterns like `Map("")`), but routes with **required parameters** like `Map("{searchTerm}")` may have an edge case.

### Implementation Phases

**Phase 1: Create Tests to Verify Bug**
- Create `tests/timewarp-nuru-tests/help/help-04-parameter-default-route-help.cs`
- Test `--help` with `Map("{searchTerm}")` 
- Test `-h` short form
- Test `--version`
- Test route still works with valid arguments

**Phase 2: Fix Implementation** (if tests fail)
- Update `route-matcher-emitter.cs` lines 112-123
- Fix alias matching in `EmitAliasMatch` method
- Handle all parameter-only routes: single, multiple, optional, catch-all

**Phase 3: Verification**
- Clear runfile cache
- Run new tests
- Run CI tests

**Phase 4: Documentation**
- Update Design region in route-matcher-emitter.cs

## Results

**Finding:** The bug was already fixed by the Issue #179 solution. The existing code in `route-matcher-emitter.cs:112-123` correctly handles parameter-only routes.

**Files Changed:**
- Added: `tests/timewarp-nuru-tests/help/help-06-parameter-only-route-help.cs`

**What was implemented:**
Created comprehensive tests to verify the fix works for all parameter-only route patterns:
- Single parameter route (`{searchTerm}`)
- Multiple parameters route (`{category} {item}`)
- Optional parameter route (`{searchTerm?}`)
- Catch-all route (`{*args}`)
- Short form `-h` and `--version` variants

**Test Results:**
- New test file: 7/7 passed
- CI tests: 1086 passed, 7 skipped, 0 failed

**Key Decision:**
The existing `hasNoLiterals` check correctly identifies routes with no literal segments and skips built-in flags for all such routes. No code changes were needed - only verification tests were added.
