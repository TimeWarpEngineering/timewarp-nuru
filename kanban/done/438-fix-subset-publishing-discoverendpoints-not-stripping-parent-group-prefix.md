# Fix subset publishing - DiscoverEndpoints not stripping parent group prefix

## Description

GitHub Issue #184: When using `.DiscoverEndpoints(typeof(GroupBase))` for subset publishing, the parent group prefix is not being stripped from command routes as documented.

**Current Behavior:**
- Commands registered with full prefix: `repo repo base sync` (requires double prefix)
- Capabilities show: `"pattern": "repo base sync"` (should be `"base sync"`)

**Expected Behavior:**
- `.DiscoverEndpoints(typeof(GroupBase))` should:
  1. Filter to only endpoints inheriting from the specified group
  2. Strip the parent group prefix from routes
- So `repo base sync` should become `base sync` in the subset edition

**Affected:** All 8 subset editions in ganda project (agent, config, kanban, nuget, repo, workflow, workspace, worktree)

## Checklist

- [x] Reproduce the issue with a test case
- [x] Identify where prefix stripping logic should be applied
- [x] Implement fix in source generator or runtime
- [x] Add unit tests for subset publishing with group prefix stripping
- [x] Verify fix works with nested group hierarchies
- [ ] Update documentation if needed

## Notes

**Group hierarchy example:**
```csharp
[NuruRouteGroup("repo")]
public abstract class RepoGroupBase;

[NuruRouteGroup("base")]
public abstract class RepoBaseGroupBase : RepoGroupBase;

[NuruRoute("sync")]
public sealed class RepoBaseSyncCommand : RepoBaseGroupBase, ICommand<Unit>
```

The source generator detects endpoints correctly (they show in capabilities), but the prefix stripping logic is not applied during route registration for subset editions.

**Environment:** Nuru version 3.0.0-beta.52, .NET 10.0.x

## Implementation Plan

### Problem Summary
When using `.DiscoverEndpoints(typeof(GroupBase))` for subset publishing, the parent group prefix is not being stripped from command routes when the matched type is the ROOT in the hierarchy.

### Root Cause
The `StripGroupPrefixAboveMatchedType` method in `nuru-generator.cs` and `interceptor-emitter.cs` only strips prefixes ABOVE the matched type, not including the matched type itself. When filtering by a root group (matchedIndex = 0), there's nothing above it, so no prefix is stripped.

### Files to Modify
1. `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` - lines 505-514
2. `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - lines 803-812

### Fix Logic
For root groups (matchedIndex == 0), strip the first prefix segment:
```csharp
int startIndex = matchedIndex.Value == 0 
  ? matchedIndex.Value + 1  // Strip root prefix
  : matchedIndex.Value;      // Keep matched type's prefix
string[] effectivePrefixes = allPrefixes[startIndex..];
```

### Test to Add
Add `FilterByRootType_StripsRootPrefix` test in `tests/timewarp-nuru-tests/generator/generator-19-group-filtering.cs`

### Verification
1. Clear runfile cache
2. Run group filtering tests
3. Run full CI test suite
4. Test the sample application

## Results

### What Was Implemented
Fixed subset publishing to strip root group prefix when filtering by root group type.

### Files Changed
1. `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` - Modified `StripGroupPrefixAboveMatchedType` to strip root prefix when `matchedIndex == 0`
2. `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - Applied identical fix to the duplicate method
3. `tests/timewarp-nuru-tests/generator/generator-19-group-filtering.cs` - Added test `FilterByRootType_StripsRootPrefix`

### Key Decision
The fix only strips the first prefix segment when the matched type is at index 0 (root). This preserves the existing behavior for non-root groups while enabling subset editions from root groups.

### Test Outcomes
- Group filtering tests: 7/7 passed
- CI tests: 1086 passed, 7 skipped, 0 failed

### Example
Before: filtering by `RepoGroupBase` kept `repo base` prefix → command was `repo base sync`
After: filtering by `RepoGroupBase` strips `repo` → command is `base sync`
