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

- [ ] Reproduce the issue with a test case
- [ ] Identify where prefix stripping logic should be applied
- [ ] Implement fix in source generator or runtime
- [ ] Add unit tests for subset publishing with group prefix stripping
- [ ] Verify fix works with nested group hierarchies
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
