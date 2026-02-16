# Implement NuruRouteAlias propagation for group base classes

## Description

Implement the fix for GitHub Issue #178. The issue reports that `[NuruRouteAlias]` defined on `[NuruRouteGroup]` base classes does not propagate to subcommands.

**Prerequisite:** Task #429 (Test NuruRouteAlias on group base classes) must be completed first to validate the failure.

## Checklist

- [ ] Extract `[NuruRouteAlias]` from command class in `endpoint-extractor.cs`
- [ ] Add `ExtractNuruRouteAliasAttribute()` method to extract aliases from the command class
- [ ] Extract group aliases from base class hierarchy in `ExtractGroupInfo()`
- [ ] Modify `GroupInfo` record to include collected aliases
- [ ] Pass aliases to `RouteDefinition.Create()` via builder
- [ ] Generate alias route match branches in `route-matcher-emitter.cs`
- [ ] Update capabilities emitter if needed for alias display
- [ ] Run tests to verify fix works

## Notes

### Implementation Details

1. **Extract from command class:**
   - Add method to read `[NuruRouteAlias]` attributes from the command class
   - Handle `params string[]` constructor argument
   - Add to `Aliases` collection in route definition

2. **Extract from base class hierarchy:**
   - In `ExtractGroupInfo()`, while walking the inheritance chain, also collect `[NuruRouteAlias]` attributes
   - Combine with aliases from the command class

3. **Generate match branches:**
   - For each alias, generate an alternative route match pattern
   - Alias substitutes for the group segment where it's defined
   - E.g., if `workspace` has alias `ws`, then `ws repo info` should match the same handler as `workspace repo info`

### Files Affected

| File | Changes |
|------|---------|
| `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` | Add alias extraction from command class and base hierarchy |
| `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` | Generate alias match branches |
| `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs` | May need update |

### Alias Semantics

An alias substitutes for the group segment where it's defined:
- `[NuruRouteGroup("workspace")] [NuruRouteAlias("ws", "work")]` on base class
- Route `workspace repo info` becomes accessible via:
  - `workspace repo info` (original)
  - `ws repo info` (alias)
  - `work repo info` (alias)

### References

- GitHub Issue: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/178
- Analysis: `.agent/workspace/2026-02-16T09-00-00_gh-issue-178-nururoutealias-group-propagation.md`
- Depends on: Task #429 (Test NuruRouteAlias on group base classes)