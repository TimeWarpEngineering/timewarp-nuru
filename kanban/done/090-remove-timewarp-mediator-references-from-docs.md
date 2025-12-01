# Remove TimeWarp.Mediator References from Documentation

## Description

Nuru no longer uses TimeWarp.Mediator. The project migrated to martinothamar/Mediator (source-generator based) for better AOT support. All documentation and README files need to be updated to remove mentions of TimeWarp.Mediator.

## Files to Update

The following files contain references to TimeWarp.Mediator that need to be removed or updated:

1. **readme.md** (line 17) - Remove the commercial license comparison mentioning TimeWarp.Mediator
2. **claude.md** (line 76) - Remove "TimeWarp.Mediator integration" reference
3. **changelog.md** (line 54) - Update historical reference about TimeWarp.Mediator integration

## Checklist

### Implementation
- [x] Update readme.md to remove TimeWarp.Mediator mention
- [x] Update claude.md to remove TimeWarp.Mediator integration reference
- [x] Update changelog.md entry (or add migration note)
- [x] Search for any additional references that may have been missed
- [x] Verify no broken links or references remain

### Documentation
- [x] Ensure mediator references now point to martinothamar/Mediator where appropriate
- [x] Verify consistency across all documentation

## Implementation Notes

Updated the following files:
1. **readme.md** - Removed licensing note comparing TimeWarp.Nuru and TimeWarp.Mediator (entire note block removed)
2. **claude.md** - Changed "TimeWarp.Mediator integration" to "Uses martinothamar/Mediator (source-generator based, AOT-friendly)"
3. **changelog.md** - Updated historical note to "Initial mediator support for DI scenarios (later migrated to martinothamar/Mediator)"
4. **kanban/to-do/008-...** - Changed "Coordinate with TimeWarp.Mediator" to "martinothamar/Mediator"
5. **kanban/to-do/016-...** - Changed "Consider impact on TimeWarp.Mediator integration" to "martinothamar/Mediator"

Remaining references are in kanban/done/ tasks which document historical changes (kept as-is per task notes).

## Notes

- The migration from TimeWarp.Mediator to martinothamar/Mediator was completed in task 078-001
- Kanban done tasks contain historical references which should remain as-is (they document what happened)
- Some to-do tasks (008, 016) reference TimeWarp.Mediator and may also need updating
- Focus on user-facing documentation (readme, claude.md, changelog) rather than historical kanban records
