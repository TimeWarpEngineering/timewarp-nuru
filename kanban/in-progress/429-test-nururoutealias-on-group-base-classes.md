# Test NuruRouteAlias on group base classes

## Description

Create test case to validate the failure reported in GitHub Issue #178. The issue reports that `[NuruRouteAlias]` defined on `[NuruRouteGroup]` base classes does not propagate to subcommands.

## Checklist

- [x] Examine existing routing test patterns in `tests/timewarp-nuru-tests/routing/`
- [x] Create test file for group alias propagation (e.g., `routing-28-group-alias.cs`)
- [ ] Test `[NuruRouteAlias]` directly on command class (verify current state)
- [ ] Test `[NuruRouteAlias]` on group base class (verify reported bug)
- [x] Run tests and confirm failure
- [ ] Document expected vs actual behavior in test

## Notes

### Test Results (2026-02-16)

Tests confirm the bug as documented in GitHub Issue #178.

**Results:**
- ✅ 2 passed: Non-alias routes work correctly
- ❌ 5 failed: All alias routes fail with "Unknown command"

**Failed tests (expected behavior, testing the bug):**
1. `Should_match_command_with_direct_alias_bye` - FAILED (exit code 1)
2. `Should_match_command_with_direct_alias_cya` - FAILED (exit code 1)  
3. `Should_match_workspace_info_with_group_alias` - FAILED (exit code 1)
4. `Should_match_workspace_info_with_second_group_alias` - FAILED (exit code 1)
5. `Should_match_nested_workspace_repo_info_with_parent_alias` - FAILED (exit code 1)

**Passed tests:**
1. `Should_match_workspace_info_without_alias` - PASSED (primary route works)
2. `Should_match_nested_workspace_repo_info_without_alias` - PASSED (nested route works)

**Analysis:** The test failures confirm that:
1. `[NuruRouteAlias]` on command classes is NOT processed by the endpoint extractor
2. `[NuruRouteAlias]` on group base classes does NOT propagate to derived commands

### Test File Created

- **File**: `tests/timewarp-nuru-tests/routing/routing-28-group-alias.cs`
- **Date**: 2026-02-16
- **Status**: Created with all test cases defined

The test file includes:
- Direct alias tests on command class (`issue178-bye`, `issue178-cya`)
- Group base class alias tests (`issue178-ws info`, `issue178-work info`)
- Nested group alias propagation tests (`issue178-ws repo info`)

### Test Cases to Create

1. **Direct alias on command class:**
   ```csharp
   [NuruRoute("goodbye")]
   [NuruRouteAlias("bye", "cya")]
   public sealed class GoodbyeCommand : ICommand<Unit>
   ```
   - Test: `goodbye` works
   - Test: `bye` works
   - Test: `cya` works

2. **Alias on group base class:**
   ```csharp
   [NuruRouteGroup("workspace")]
   [NuruRouteAlias("ws")]
   public abstract class WorkspaceGroupBase;

   [NuruRoute("info")]
   public sealed class WorkspaceInfoCommand : WorkspaceGroupBase, ICommand<Unit>
   ```
   - Test: `workspace info` works
   - Test: `ws info` should work (alias)

3. **Alias on nested group base class:**
   ```csharp
   [NuruRouteGroup("workspace")]
   [NuruRouteAlias("ws")]
   public abstract class WorkspaceGroupBase;

   [NuruRouteGroup("repo")]
   public abstract class WorkspaceRepoGroupBase : WorkspaceGroupBase;

   [NuruRoute("info")]
   public sealed class WorkspaceRepoInfoCommand : WorkspaceRepoGroupBase, ICommand<Unit>
   ```
   - Test: `workspace repo info` works
   - Test: `ws repo info` should work (alias propagates through hierarchy)

### References

- GitHub Issue: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/178
- Analysis: `.agent/workspace/2026-02-16T09-00-00_gh-issue-178-nururoutealias-group-propagation.md`
- Existing routing tests: `tests/timewarp-nuru-tests/routing/`
